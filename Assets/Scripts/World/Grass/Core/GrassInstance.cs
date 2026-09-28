using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// GrassInstance'ın paketlenmemiş (okunabilir) hali. Compute'un yazdığı/shader'ın okuduğu sıkıştırılmış
    /// yapının CPU tarafı görünümü; testler, tanılama ve CPU referans üretimi bunu kullanır.
    /// </summary>
    public struct GrassInstanceValues
    {
        public Vector3 Position;
        /// <summary>Radyan; Pack [0, 2pi) aralığına sarar.</summary>
        public float Yaw;
        /// <summary>Metre (half hassasiyeti).</summary>
        public float Height;
        /// <summary>Metre (half hassasiyeti).</summary>
        public float Width;
        /// <summary>0..1 (unorm16).</summary>
        public float LodFade;
        public Color32 Color;
        /// <summary>Terrain normali (oktahedral); şimdilik rezerve, ham uint taşınır.</summary>
        public uint NormalOct;
        public uint Hash;
    }

    /// <summary>
    /// GPU'ya giden kalıcı çim instance verisi: 32 bayt, Sequential.
    /// BİT DÜZENİ Assets/Shaders/Grass/GrassInstanceData.hlsl içindeki GrassInstance/Grass_Unpack ile BİREBİR
    /// aynı olmak zorunda: alan sırası, boyutu veya paketleme değişirse iki dosya (ve GrassInstanceTests'teki
    /// sabit vektörler) BİRLİKTE değişir. Bilerek Spike sınıfından bağımsız yazıldı; spike silinince bu kalır.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GrassInstance
    {
        public const int Stride = 32;

        const float TwoPi = 6.28318530718f;
        const uint Unorm16Max = 65535u;

        // half'in taşıyabildiği en büyük sonlu değer; aşan değer f32tof16'da sonsuza gider.
        const float HalfMax = 65504f;

        public Vector3 position;
        /// <summary>düşük 16 bit = yaw (unorm16, 0..2pi), yüksek 16 bit = yükseklik (half, m).</summary>
        public uint yawHeight;
        /// <summary>düşük 16 bit = genişlik (half, m), yüksek 16 bit = lodFade (unorm16).</summary>
        public uint widthFade;
        /// <summary>R | G&lt;&lt;8 | B&lt;&lt;16 | A&lt;&lt;24.</summary>
        public uint colorRGBA8;
        public uint normalOct;
        public uint hash;

        public static GrassInstance Pack(in GrassInstanceValues v)
        {
            // Yaw'ı sarmak: negatif/büyük açı unorm aralığına taşmasın; NaN 0'a düşsün (bozuk veri GPU'ya
            // rastgele bit örüntüsü olarak gitmesin).
            float yaw = v.Yaw;
            yaw = float.IsNaN(yaw) || float.IsInfinity(yaw) ? 0f : yaw - math.floor(yaw / TwoPi) * TwoPi;
            uint yaw16 = (uint)(yaw / TwoPi * Unorm16Max + 0.5f);
            if (yaw16 > Unorm16Max) yaw16 = Unorm16Max;

            uint height16 = math.f32tof16(SanitizeMetres(v.Height));
            uint width16 = math.f32tof16(SanitizeMetres(v.Width));

            float fade = !(v.LodFade > 0f) ? 0f : (v.LodFade > 1f ? 1f : v.LodFade); // NaN -> 0
            uint fade16 = (uint)(fade * Unorm16Max + 0.5f);

            Color32 c = v.Color;
            return new GrassInstance
            {
                position = v.Position,
                yawHeight = yaw16 | (height16 << 16),
                widthFade = width16 | (fade16 << 16),
                colorRGBA8 = (uint)c.r | ((uint)c.g << 8) | ((uint)c.b << 16) | ((uint)c.a << 24),
                normalOct = v.NormalOct,
                hash = v.Hash,
            };
        }

        public GrassInstanceValues Unpack()
        {
            // HLSL Grass_Unpack'in birebir C# karşılığı (aynı maske/kaydırma/ölçek).
            return new GrassInstanceValues
            {
                Position = position,
                Yaw = (yawHeight & 0xFFFFu) * (TwoPi / Unorm16Max),
                Height = math.f16tof32(yawHeight >> 16),
                Width = math.f16tof32(widthFade & 0xFFFFu),
                LodFade = (widthFade >> 16) * (1f / Unorm16Max),
                Color = new Color32(
                    (byte)(colorRGBA8 & 0xFFu),
                    (byte)((colorRGBA8 >> 8) & 0xFFu),
                    (byte)((colorRGBA8 >> 16) & 0xFFu),
                    (byte)((colorRGBA8 >> 24) & 0xFFu)),
                NormalOct = normalOct,
                Hash = hash,
            };
        }

        // Negatif/NaN boyut anlamsız (ters yüzlü veya çöp bıçak); half taşmasını da önler.
        static float SanitizeMetres(float value) => !(value > 0f) ? 0f : (value > HalfMax ? HalfMax : value);
    }
}
