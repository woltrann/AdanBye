using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Blade rengi seçiminin saf C# aynası (GrassGenerate.compute Grass_SelectTint ile AYNI algoritma; biri değişirse
    /// ikisi birlikte değişir). Layer, splat * yoğunluk ağırlıklarıyla olasılıksal seçilir; böylece layer sınırında
    /// bıçaklar karışır ve yoğunluğu 0 olan layer asla seçilmez. Seçim compute'taki `rank`ten DEĞİL kendi hash akışından
    /// (7) gelir: rank LOD seyreltmesinde kullanılır, uzak LOD'da yalnızca düşük rank'lar kalır => rank'tan seçim bias'lı olurdu.
    /// Parlaklık jitter'ı akış 8'den gelir.
    /// </summary>
    public static class GrassTintSelector
    {
        /// <summary>Hash.hlsl akış numaraları; 1..6 yerleşim/rank/yaw/boy/genişlik tarafından dolu.</summary>
        public const uint StreamSelect = 7u;
        public const uint StreamJitter = 8u;

        /// <summary>Toplam ağırlık bunun altındaysa (splat*yoğunluk ~ 0) layer 0 seçilir.</summary>
        const float MinTotalWeight = 1e-6f;

        /// <summary>
        /// Blade'in layer'ı. <paramref name="hash"/> = Hash_Cell çıktısı (compute'taki `h`).
        /// </summary>
        public static int SelectLayer(uint hash, Vector4 splat, Vector4 density)
        {
            float w0 = splat.x * density.x, w1 = splat.y * density.y, w2 = splat.z * density.z, w3 = splat.w * density.w;
            float c0 = w0;
            float c1 = c0 + w1;
            float c2 = c1 + w2;
            float total = c2 + w3;
            if (!(total > MinTotalWeight)) return 0; // NaN de buraya düşer

            float pick = Stream01(hash, StreamSelect) * total;
            // En son ağırlıklı layer'dan başla: pick*total yuvarlaması total'e eşitlenirse ağırlığı 0 layer seçilmesin.
            int layer = w3 > 0f ? 3 : (w2 > 0f ? 2 : (w1 > 0f ? 1 : 0));
            if (pick < c2) layer = 2;
            if (pick < c1) layer = 1;
            if (pick < c0) layer = 0;
            return layer;
        }

        /// <summary>
        /// Instance rengi: rgb = seçilen layer'ın uç rengi * parlaklık jitter'ı (0..1'e kırpılır), a = kök koyulaştırma
        /// oranı (jitter'a girmez). <paramref name="layerTints"/> LayerDensityMapper.LayerTints ile aynı düzen (4 eleman).
        /// </summary>
        public static Vector4 ComputeColor(uint hash, Vector4 splat, Vector4 density, Vector4[] layerTints, float colorJitter)
        {
            Vector4 tint = layerTints[SelectLayer(hash, splat, density)];
            float jitter = 1f + (Stream01(hash, StreamJitter) * 2f - 1f) * colorJitter;
            return new Vector4(Saturate(tint.x * jitter), Saturate(tint.y * jitter), Saturate(tint.z * jitter), Saturate(tint.w));
        }

        static float Saturate(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        // --- Hash.hlsl'in (PCG) birebir C# aynası: yalnızca tamsayı işlemi, GPU'yla bit-bit aynı sonuç. ---

        static uint Pcg(uint v)
        {
            unchecked
            {
                uint state = v * 747796405u + 2891336233u;
                uint word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
                return (word >> 22) ^ word;
            }
        }

        /// <summary>Hash_Stream01 aynası: [0,1).</summary>
        public static float Stream01(uint hash, uint stream)
        {
            uint h = Pcg(unchecked(hash + stream * 0x9E3779B9u));
            return (h >> 8) * (1f / 16777216f);
        }

        /// <summary>Hash_Cell aynası (testlerde farklı hücrelerden hash üretmek için).</summary>
        public static uint HashCell(int x, int y, uint seed)
            => Pcg(unchecked((uint)x) ^ Pcg(unchecked((uint)y) ^ Pcg(seed)));
    }
}
