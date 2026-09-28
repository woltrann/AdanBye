using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>
    /// HLSL Grass_Pack (GrassInstanceData.hlsl) ile C# GrassInstance.Pack'in aynı bit düzenini ürettiğini GPU'da
    /// ölçer (CSPackParity kernel'i). GPU'suz ortamda (batchmode -nographics) Ignore edilir.
    /// </summary>
    public class GrassGenerateParityTests
    {
        const string ComputePath = "Assets/Shaders/Grass/GrassGenerate.compute";

        // HLSL PackParityCase ile aynı düzen (52 bayt).
        [StructLayout(LayoutKind.Sequential)]
        struct ParityCase
        {
            public Vector3 position;
            public float yaw, height, width, lodFade;
            public Vector4 color;
            public uint normalOct, hash;
        }

        [Test]
        public void Struct_MatchesHlslParityCaseSize()
        {
            Assert.AreEqual(52, Marshal.SizeOf(typeof(ParityCase)));
        }

        [Test]
        public void HlslPack_MatchesCsharpPack()
        {
            if (!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute shader desteklenmiyor.");
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            Assert.IsNotNull(cs, ComputePath);

            // Elle seçilmiş sınır durumları: negatif/büyük yaw, taşan boyut, negatif boyut, aralık dışı fade.
            var cases = new System.Collections.Generic.List<ParityCase>
            {
                Make(new Vector3(1, 2, 3), 0f, 1f, 0.5f, 1f, 0x11, 0x22, 0x33, 0x44),
                Make(Vector3.zero, -1.5f, 0.0625f, 2f, 0f, 255, 0, 128, 1),
                Make(Vector3.one, 7f * 6.28318530718f + 0.25f, 1e9f, -3f, 2f, 0, 0, 0, 0),
                Make(Vector3.one, 100f, 0f, 65504f, -1f, 9, 8, 7, 6),
            };
            var rng = new Unity.Mathematics.Random(2024u);
            for (int i = 0; i < 4000; i++)
            {
                cases.Add(Make(new Vector3(rng.NextFloat(-500, 500), rng.NextFloat(0, 600), rng.NextFloat(-500, 500)),
                               rng.NextFloat(-10f, 20f), rng.NextFloat(0.01f, 3f), rng.NextFloat(0.01f, 0.5f), rng.NextFloat(0f, 1f),
                               (byte)rng.NextUInt(0, 256), (byte)rng.NextUInt(0, 256), (byte)rng.NextUInt(0, 256), (byte)rng.NextUInt(0, 256)));
            }

            var input = cases.ToArray();
            var inBuf = new ComputeBuffer(input.Length, Marshal.SizeOf(typeof(ParityCase)));
            var outBuf = new ComputeBuffer(input.Length, GrassInstance.Stride);
            try
            {
                inBuf.SetData(input);
                int k = cs.FindKernel("CSPackParity");
                cs.SetBuffer(k, "_ParityIn", inBuf);
                cs.SetBuffer(k, "_ParityOut", outBuf);
                cs.SetInt("_ParityCount", input.Length);
                cs.Dispatch(k, (input.Length + 63) / 64, 1, 1);
                var gpu = new GrassInstance[input.Length];
                outBuf.GetData(gpu);

                int exact = 0;
                for (int i = 0; i < input.Length; i++)
                {
                    ParityCase c = input[i];
                    GrassInstance cpu = GrassInstance.Pack(new GrassInstanceValues
                    {
                        Position = c.position, Yaw = c.yaw, Height = c.height, Width = c.width, LodFade = c.lodFade,
                        // color 0..1 (GPU girdisi); C# tarafı 8 bit ister -> HLSL ile aynı yuvarlama.
                        Color = new Color32((byte)Mathf.RoundToInt(c.color.x * 255f), (byte)Mathf.RoundToInt(c.color.y * 255f),
                                            (byte)Mathf.RoundToInt(c.color.z * 255f), (byte)Mathf.RoundToInt(c.color.w * 255f)),
                        NormalOct = c.normalOct, Hash = c.hash,
                    });
                    GrassInstance g = gpu[i];

                    Assert.AreEqual(cpu.position, g.position, $"pos #{i}");
                    Assert.AreEqual(cpu.colorRGBA8, g.colorRGBA8, $"color #{i}");
                    Assert.AreEqual(cpu.normalOct, g.normalOct, $"normalOct #{i}");
                    Assert.AreEqual(cpu.hash, g.hash, $"hash #{i}");

                    // Alt sözcükler: GPU'nun f32tof16 yuvarlaması/FMA'sı son bitte farklı olabilir; 1 birim (1 half ulp /
                    // 1 unorm16 adımı) tolerans, ama katı eşleşen oranı raporlanır.
                    int dyaw = System.Math.Abs((int)(cpu.yawHeight & 0xFFFF) - (int)(g.yawHeight & 0xFFFF));
                    int dh = System.Math.Abs((int)(cpu.yawHeight >> 16) - (int)(g.yawHeight >> 16));
                    int dw = System.Math.Abs((int)(cpu.widthFade & 0xFFFF) - (int)(g.widthFade & 0xFFFF));
                    int df = System.Math.Abs((int)(cpu.widthFade >> 16) - (int)(g.widthFade >> 16));
                    Assert.LessOrEqual(dyaw, 1, $"yaw16 #{i}");
                    Assert.LessOrEqual(dh, 1, $"height half #{i}");
                    Assert.LessOrEqual(dw, 1, $"width half #{i}");
                    Assert.LessOrEqual(df, 1, $"fade16 #{i}");
                    if (cpu.yawHeight == g.yawHeight && cpu.widthFade == g.widthFade) exact++;
                }
                Debug.Log($"[GrassGenerateParity] {exact}/{input.Length} instance tam bit-bit eşleşti (kalanı <=1 birim fark).");
            }
            finally
            {
                inBuf.Release();
                outBuf.Release();
            }
        }

        static ParityCase Make(Vector3 p, float yaw, float h, float w, float fade, byte r, byte g, byte b, byte a) =>
            new ParityCase
            {
                position = p, yaw = yaw, height = h, width = w, lodFade = fade,
                color = new Vector4(r, g, b, a) / 255f * 1f, normalOct = 0xA5A5A5A5u ^ (uint)(r | (g << 8)), hash = (uint)(r * 2654435761u) ^ (uint)(g << 16 | b),
            };
    }
}
