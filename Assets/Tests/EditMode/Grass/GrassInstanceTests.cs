using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using AdanBye.Grass.Spike;

namespace AdanBye.Grass.Tests
{
    public class GrassInstanceTests
    {
        const float TwoPi = 6.28318530718f;

        [Test]
        public void Struct_Is32Bytes_WithHlslFieldOffsets()
        {
            Assert.AreEqual(32, Marshal.SizeOf(typeof(GrassInstance)));
            Assert.AreEqual(32, GrassInstance.Stride);

            // GrassInstanceData.hlsl: float3 position; uint yawHeight, widthFade, colorRGBA8, normalOct, hash;
            Assert.AreEqual(0, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.position)));
            Assert.AreEqual(12, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.yawHeight)));
            Assert.AreEqual(16, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.widthFade)));
            Assert.AreEqual(20, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.colorRGBA8)));
            Assert.AreEqual(24, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.normalOct)));
            Assert.AreEqual(28, (int)Marshal.OffsetOf(typeof(GrassInstance), nameof(GrassInstance.hash)));
        }

        // Spike'taki kanıtlanmış düzenle kayma olmasın diye koruma. Spike WP-8'de silinirse bu test de silinir
        // (yukarıdaki sabit ofsetler kalıcı sözleşmedir).
        [Test]
        public void Layout_MatchesWorkingSpikeStruct()
        {
            Assert.AreEqual(Marshal.SizeOf(typeof(GrassSpikeInstance)), Marshal.SizeOf(typeof(GrassInstance)));
            foreach (string field in new[] { "position", "yawHeight", "widthFade", "colorRGBA8", "normalOct", "hash" })
            {
                Assert.AreEqual(
                    (int)Marshal.OffsetOf(typeof(GrassSpikeInstance), field),
                    (int)Marshal.OffsetOf(typeof(GrassInstance), field),
                    field);
            }
        }

        // SABİT TEST VEKTÖRLERİ (GPU'da çalıştırılmaz; bit düzeninin HLSL Grass_Unpack ile eşleştiğini elle doğrulanmış
        // sözcüklerle kilitler). half bit desenleri: 1.0=0x3C00, 0.5=0x3800, 2.0=0x4000, 0.0625=0x2C00.
        //   Vektör A: yaw16=0x4000 (2pi*16384/65535), height=1.0, width=0.5, lodFade=1.0 (0xFFFF),
        //             rgba=(0x11,0x22,0x33,0x44), normalOct=0xDEADBEEF, hash=0xCAFEBABE
        //     yawHeight = 0x3C00 << 16 | 0x4000 = 0x3C004000
        //     widthFade = 0xFFFF << 16 | 0x3800 = 0xFFFF3800
        //     colorRGBA8 = 0x44332211
        //   Vektör B: yaw=0, height=0.0625, width=2.0, lodFade=0, rgba=(255,0,128,1)
        //     yawHeight = 0x2C000000, widthFade = 0x00004000, colorRGBA8 = 0x018000FF
        [Test]
        public void Pack_MatchesHandComputedHlslWords_VectorA()
        {
            GrassInstance p = GrassInstance.Pack(new GrassInstanceValues
            {
                Position = new Vector3(1f, 2f, 3f),
                Yaw = TwoPi * 16384f / 65535f,
                Height = 1f,
                Width = 0.5f,
                LodFade = 1f,
                Color = new Color32(0x11, 0x22, 0x33, 0x44),
                NormalOct = 0xDEADBEEF,
                Hash = 0xCAFEBABE,
            });

            Assert.AreEqual(new Vector3(1f, 2f, 3f), p.position);
            Assert.AreEqual(0x3C004000u, p.yawHeight);
            Assert.AreEqual(0xFFFF3800u, p.widthFade);
            Assert.AreEqual(0x44332211u, p.colorRGBA8);
            Assert.AreEqual(0xDEADBEEFu, p.normalOct);
            Assert.AreEqual(0xCAFEBABEu, p.hash);
        }

        [Test]
        public void Pack_MatchesHandComputedHlslWords_VectorB()
        {
            GrassInstance p = GrassInstance.Pack(new GrassInstanceValues
            {
                Position = Vector3.zero,
                Yaw = 0f,
                Height = 0.0625f,
                Width = 2f,
                LodFade = 0f,
                Color = new Color32(255, 0, 128, 1),
            });

            Assert.AreEqual(0x2C000000u, p.yawHeight);
            Assert.AreEqual(0x00004000u, p.widthFade);
            Assert.AreEqual(0x018000FFu, p.colorRGBA8);
        }

        [Test]
        public void Unpack_OfHandComputedWords_FollowsHlslFormulas()
        {
            // HLSL formüllerinin bağımsız transkripsiyonu (Pack'e bağlı değil): Vektör A sözcükleri.
            var raw = new GrassInstance
            {
                position = new Vector3(1f, 2f, 3f),
                yawHeight = 0x3C004000u,
                widthFade = 0xFFFF3800u,
                colorRGBA8 = 0x44332211u,
                normalOct = 7u,
                hash = 9u,
            };
            GrassInstanceValues v = raw.Unpack();

            Assert.AreEqual(16384u * (6.28318530718f / 65535f), v.Yaw, 1e-5f);
            Assert.AreEqual(math.f16tof32(0x3C00u), v.Height);
            Assert.AreEqual(1f, v.Height);
            Assert.AreEqual(0.5f, v.Width);
            Assert.AreEqual(1f, v.LodFade, 1e-6f);
            Assert.AreEqual(new Color32(0x11, 0x22, 0x33, 0x44), v.Color);
            Assert.AreEqual(7u, v.NormalOct);
            Assert.AreEqual(9u, v.Hash);
        }

        [Test]
        public void RoundTrip_StaysWithinQuantizationTolerance()
        {
            var input = new GrassInstanceValues
            {
                Position = new Vector3(10.5f, -2f, 300f),
                Yaw = 1.234f,
                Height = 0.75f,
                Width = 0.05f,
                LodFade = 0.5f,
                Color = new Color32(10, 200, 30, 255),
                NormalOct = 7u,
                Hash = 123456u,
            };

            GrassInstanceValues o = GrassInstance.Pack(input).Unpack();

            Assert.AreEqual(input.Position, o.Position);
            Assert.AreEqual(input.Yaw, o.Yaw, TwoPi / 65535f);           // unorm16 adımı
            Assert.AreEqual(input.Height, o.Height, input.Height * 1e-3f); // half: ~11 bit mantis
            Assert.AreEqual(input.Width, o.Width, input.Width * 1e-3f);
            Assert.AreEqual(input.LodFade, o.LodFade, 1f / 65535f);
            Assert.AreEqual(input.Color, o.Color);
            Assert.AreEqual(input.NormalOct, o.NormalOct);
            Assert.AreEqual(input.Hash, o.Hash);
        }

        [Test]
        public void Yaw_IsWrappedIntoZeroToTwoPi()
        {
            var v = new GrassInstanceValues();

            v.Yaw = -Mathf.PI * 0.5f;
            Assert.AreEqual(Mathf.PI * 1.5f, GrassInstance.Pack(v).Unpack().Yaw, 1e-3f);

            v.Yaw = 3f * TwoPi + 0.5f;
            Assert.AreEqual(0.5f, GrassInstance.Pack(v).Unpack().Yaw, 1e-3f);
        }

        [Test]
        public void InvalidScalars_AreSanitized()
        {
            GrassInstanceValues o = GrassInstance.Pack(new GrassInstanceValues
            {
                Yaw = float.NaN,
                Height = -1f,
                Width = float.NaN,
                LodFade = 2f,
            }).Unpack();
            Assert.AreEqual(0f, o.Yaw);
            Assert.AreEqual(0f, o.Height);
            Assert.AreEqual(0f, o.Width);
            Assert.AreEqual(1f, o.LodFade, 1e-6f);

            o = GrassInstance.Pack(new GrassInstanceValues { Height = 1e9f, LodFade = float.NaN }).Unpack();
            Assert.AreEqual(65504f, o.Height); // half'in en büyük sonlu değeri; sonsuza taşmaz
            Assert.AreEqual(0f, o.LodFade);
        }
    }
}
