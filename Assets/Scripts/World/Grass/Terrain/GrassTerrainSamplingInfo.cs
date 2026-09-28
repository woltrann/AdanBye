using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Compute shader'ın terrain'i dünya XZ'sinden texel koordinatına çevirmek için ihtiyaç duyduğu değişmez
    /// veriler. Neden ayrı bir değer tipi: hem doğrulama aracı hem ileride üretim compute'u (WP-3) aynı
    /// origin/size/çözünürlük sözleşmesini kullanacak; uniform isimleri GrassTerrainSampling.hlsl ile bu
    /// tek dosyada eşleştirilir, sahnede dağılmaz.
    /// </summary>
    public readonly struct GrassTerrainSamplingInfo
    {
        static readonly int OriginId = Shader.PropertyToID("_TerrainOrigin");
        static readonly int SizeId = Shader.PropertyToID("_TerrainSize");
        static readonly int HeightmapResId = Shader.PropertyToID("_HeightmapRes");
        static readonly int AlphamapResId = Shader.PropertyToID("_AlphamapRes");

        public Vector3 Origin { get; }
        public Vector3 Size { get; }
        public int HeightmapResolution { get; }
        public int AlphamapResolution { get; }

        GrassTerrainSamplingInfo(Vector3 origin, Vector3 size, int heightmapRes, int alphamapRes)
        {
            Origin = origin;
            Size = size;
            HeightmapResolution = heightmapRes;
            AlphamapResolution = alphamapRes;
        }

        /// <summary>
        /// Terrain örnekleme verisini üretir. Döndürülen false ise <paramref name="error"/> nedenini söyler.
        /// Neden döndürülen değer/error deseni: çağıran (Editor aracı veya runtime renderer) hatayı kendi
        /// kanalına (LogError / dialog / disable) yönlendirmeli; burada exception fırlatmak bu kararı çalar.
        /// </summary>
        public static bool TryCreate(Terrain terrain, out GrassTerrainSamplingInfo info, out string error)
        {
            info = default;
            if (terrain == null) { error = "Terrain null."; return false; }

            TerrainData data = terrain.terrainData;
            if (data == null) { error = "Terrain'in TerrainData'sı yok."; return false; }

            // Compute'taki UV matematiği yalnızca eksen hizalı, birim ölçekli terrain'i varsayar
            // (dünya XZ -> (pos - origin) / size). Aksi halde sessizce yanlış örneklerdi.
            Transform t = terrain.transform;
            if (Quaternion.Angle(t.rotation, Quaternion.identity) > 0.001f)
            {
                error = "Terrain döndürülmüş; örnekleme yalnızca rotasyonsuz terrain'i destekler.";
                return false;
            }
            if ((t.lossyScale - Vector3.one).sqrMagnitude > 1e-6f)
            {
                error = "Terrain transform ölçeği 1 değil; desteklenmiyor.";
                return false;
            }

            info = new GrassTerrainSamplingInfo(t.position, data.size, data.heightmapResolution, data.alphamapResolution);
            error = null;
            return true;
        }

        /// <summary>Uniform'ları compute shader'a yazar (kernel'den bağımsız, shader düzeyinde).</summary>
        public void ApplyTo(ComputeShader shader)
        {
            shader.SetVector(OriginId, Origin);
            shader.SetVector(SizeId, Size);
            shader.SetInts(HeightmapResId, HeightmapResolution, HeightmapResolution);
            shader.SetInts(AlphamapResId, AlphamapResolution, AlphamapResolution);
        }
    }
}
