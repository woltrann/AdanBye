using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bir dispatch'in doğrulanmış üretim parametreleri. Ham sayıları doğrudan compute'a vermek yerine burada
    /// doğrulanır: negatif aralık, NaN yoğunluk ya da hücre sayısı taşması compute'ta sessizce çöp üretirdi.
    /// LOD bütçesi burada DEĞİL: bütçe = LOD buffer kapasitesi (GrassGpuResources).
    /// </summary>
    public readonly struct GrassGenerateSettings
    {
        // Chunk başına eksen hücre sayısı üst sınırı: 256x256 = 65k thread/chunk; bozuk yoğunluk ayarı GPU'yu boğmasın.
        public const int MaxCellsPerAxis = 256;

        public float ChunkSize { get; }
        public int CellsPerChunkAxis { get; }
        public float CellSize { get; }
        public uint Seed { get; }
        public Vector4 LayerDensity { get; }
        /// <summary>Radyan: x = tam yoğunluk sınırı, y = sıfır yoğunluk sınırı.</summary>
        public Vector2 SlopeAnglesRad { get; }
        public Vector2 HeightRange { get; }
        public Vector2 WidthRange { get; }

        /// <summary>Hücre boyutu tam sayı hücreye yuvarlandığı için gerçek üst yoğunluk istenenden büyük/eşit olur (adet/m2).</summary>
        public float ActualMaxDensityPerM2 => 1f / (CellSize * CellSize);

        GrassGenerateSettings(float chunkSize, int cells, uint seed, Vector4 layerDensity,
                              Vector2 slopeRad, Vector2 heightRange, Vector2 widthRange)
        {
            ChunkSize = chunkSize;
            CellsPerChunkAxis = cells;
            CellSize = chunkSize / cells;
            Seed = seed;
            LayerDensity = layerDensity;
            SlopeAnglesRad = slopeRad;
            HeightRange = heightRange;
            WidthRange = widthRange;
        }

        /// <summary>
        /// Hücre boyutu maxDensityPerM2'den türer: chunk'ı eksende ceil(chunkSize * sqrt(density)) hücreye böleriz;
        /// böylece hücre kenarı chunk kenarını TAM böler (hücre ızgarası chunk sınırlarına oturur, chunk'lar arası
        /// dikiş/çift aday olmaz). Örn. 16 m chunk, 16/m2 -> 64 hücre, 0.25 m.
        /// </summary>
        public static bool TryCreate(float chunkSize, float maxDensityPerM2, uint seed,
                                     Vector4 layerDensity, float slopeMinDeg, float slopeMaxDeg,
                                     Vector2 heightRange, Vector2 widthRange,
                                     out GrassGenerateSettings settings, out string error)
        {
            settings = default;
            if (!IsFinitePositive(chunkSize)) { error = "chunkSize sonlu ve > 0 olmalı."; return false; }
            if (!IsFinitePositive(maxDensityPerM2)) { error = "maxDensityPerM2 sonlu ve > 0 olmalı."; return false; }

            double cellsD = System.Math.Ceiling(chunkSize * System.Math.Sqrt(maxDensityPerM2));
            if (cellsD < 1.0) cellsD = 1.0;
            if (cellsD > MaxCellsPerAxis)
            {
                error = $"Yoğunluk chunk başına {cellsD:0} hücre/eksen gerektiriyor; üst sınır {MaxCellsPerAxis}. Yoğunluğu ya da chunk boyutunu düşür.";
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                float d = layerDensity[i];
                if (float.IsNaN(d) || float.IsInfinity(d) || d < 0f) { error = $"layerDensity[{i}] sonlu ve >= 0 olmalı."; return false; }
            }
            if (!IsFinite(slopeMinDeg) || !IsFinite(slopeMaxDeg) || slopeMinDeg < 0f || slopeMaxDeg > 90f || slopeMaxDeg <= slopeMinDeg)
            {
                error = "Eğim açıları 0 <= min < max <= 90 (derece) olmalı.";
                return false;
            }
            if (!IsValidRange(heightRange)) { error = "heightRange: 0 < min <= max ve sonlu olmalı."; return false; }
            if (!IsValidRange(widthRange)) { error = "widthRange: 0 < min <= max ve sonlu olmalı."; return false; }

            settings = new GrassGenerateSettings(chunkSize, (int)cellsD, seed, layerDensity,
                                                 new Vector2(slopeMinDeg * Mathf.Deg2Rad, slopeMaxDeg * Mathf.Deg2Rad),
                                                 heightRange, widthRange);
            error = null;
            return true;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static bool IsFinitePositive(float v) => IsFinite(v) && v > 0f;
        static bool IsValidRange(Vector2 r) => IsFinitePositive(r.x) && IsFinite(r.y) && r.y >= r.x;
    }

    /// <summary>
    /// Bir dispatch'in kamera girdisi: LOD mesafesi için konum, blade başına frustum culling için 6 düzlem.
    /// Düzlemler PlanesFrustum.CopyPlanes ile doldurulur (allocation'sız; dizi çağıranda yeniden kullanılır).
    /// </summary>
    public readonly struct GrassViewParams
    {
        public const int PlaneCount = 6;

        public Vector3 CameraPosition { get; }
        /// <summary>6 düzlem (xyz = iç yönlü normal, w = uzaklık) ya da null (blade frustum testi yok).</summary>
        public Vector4[] FrustumPlanes { get; }
        /// <summary>Blade küresine eklenen pay (m); rüzgar/ezme eğilmesi için.</summary>
        public float CullPadding { get; }

        public GrassViewParams(Vector3 cameraPosition, Vector4[] frustumPlanes, float cullPadding)
        {
            CameraPosition = cameraPosition;
            FrustumPlanes = frustumPlanes;
            CullPadding = cullPadding;
        }

        public bool Validate(out string error)
        {
            if (float.IsNaN(CameraPosition.x + CameraPosition.y + CameraPosition.z) ||
                float.IsInfinity(CameraPosition.x + CameraPosition.y + CameraPosition.z))
            {
                error = "Kamera konumu sonlu olmalı.";
                return false;
            }
            if (FrustumPlanes != null && FrustumPlanes.Length != PlaneCount)
            {
                error = $"Frustum düzlemi sayısı {PlaneCount} olmalı (değer {FrustumPlanes.Length}).";
                return false;
            }
            if (float.IsNaN(CullPadding) || float.IsInfinity(CullPadding) || CullPadding < 0f)
            {
                error = "CullPadding sonlu ve >= 0 olmalı.";
                return false;
            }
            error = null;
            return true;
        }
    }

    /// <summary>
    /// GrassGenerate.compute'u sürer: kernel kimlikleri, parametre/kaynak bağlama, Reset -> Generate -> FinalizeArgs.
    /// ComputeShader DIŞARIDAN verilir (DIP): asset yükleme sorumluluğu çağıranda, bu sınıf test/tekrar kullanımda
    /// başka bir shader örneğiyle çalışabilir. Chunk seçimi ve çizim bu sınıfın işi değildir.
    /// </summary>
    public sealed class GrassComputeDispatcher
    {
        const int GroupSize = 8; // compute'taki numthreads(8,8,1) ile aynı olmalı

        static readonly int ChunksId = Shader.PropertyToID("_Chunks");
        static readonly int[] InstancesIds =
        {
            Shader.PropertyToID("_Instances0"), Shader.PropertyToID("_Instances1"), Shader.PropertyToID("_Instances2"),
        };
        static readonly int StateId = Shader.PropertyToID("_State");
        static readonly int ArgsId = Shader.PropertyToID("_Args");
        static readonly int HeightmapId = Shader.PropertyToID("_Heightmap");
        static readonly int Alphamap0Id = Shader.PropertyToID("_Alphamap0");
        static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        static readonly int CellsPerAxisId = Shader.PropertyToID("_CellsPerAxis");
        static readonly int SeedId = Shader.PropertyToID("_Seed");
        static readonly int ChunkCountId = Shader.PropertyToID("_ChunkCount");
        static readonly int LayerDensityId = Shader.PropertyToID("_LayerDensity");
        static readonly int SlopeAnglesId = Shader.PropertyToID("_SlopeAngles");
        static readonly int HeightRangeId = Shader.PropertyToID("_HeightRange");
        static readonly int WidthRangeId = Shader.PropertyToID("_WidthRange");
        static readonly int LodCountId = Shader.PropertyToID("_LodCount");
        static readonly int LodMaxDistSqId = Shader.PropertyToID("_LodMaxDistSq");
        static readonly int LodKeepId = Shader.PropertyToID("_LodKeep");
        static readonly int LodWidthCompId = Shader.PropertyToID("_LodWidthComp");
        static readonly int LodBandId = Shader.PropertyToID("_LodBand");
        static readonly int LodBudgetId = Shader.PropertyToID("_LodBudget");
        static readonly int CameraPosId = Shader.PropertyToID("_CameraPos");
        static readonly int FrustumEnabledId = Shader.PropertyToID("_FrustumEnabled");
        static readonly int FrustumPlanesId = Shader.PropertyToID("_FrustumPlanes");
        static readonly int CullPaddingId = Shader.PropertyToID("_CullPadding");

        readonly ComputeShader _shader;
        readonly int _kernelReset;
        readonly int _kernelGenerate;
        readonly int _kernelFinalize;
        readonly int[] _budgetScratch = new int[4]; // per-frame allocation olmasın diye tek sefer

        GrassComputeDispatcher(ComputeShader shader, int reset, int generate, int finalize)
        {
            _shader = shader;
            _kernelReset = reset;
            _kernelGenerate = generate;
            _kernelFinalize = finalize;
        }

        public static bool TryCreate(ComputeShader shader, out GrassComputeDispatcher dispatcher, out string error)
        {
            dispatcher = null;
            if (shader == null) { error = "ComputeShader null."; return false; }

            // FindKernel bulunamazsa exception atar; HasKernel ile önce sor, açık hata mesajı ver.
            if (!shader.HasKernel("CSReset") || !shader.HasKernel("CSGenerate") || !shader.HasKernel("CSFinalizeArgs"))
            {
                error = $"'{shader.name}' beklenen kernel'leri (CSReset/CSGenerate/CSFinalizeArgs) içermiyor.";
                return false;
            }

            dispatcher = new GrassComputeDispatcher(shader, shader.FindKernel("CSReset"),
                                                    shader.FindKernel("CSGenerate"), shader.FindKernel("CSFinalizeArgs"));
            error = null;
            return true;
        }

        /// <summary>
        /// Chunk buffer'ındaki ilk <paramref name="chunkCount"/> chunk için çim üretir, LOD'lara böler, culling yapar.
        /// Girdi geçersizse HİÇBİR dispatch yapılmaz ve false döner (kaynak bağlanmamış compute çalıştırmak GPU
        /// hatası/çöp üretirdi). Bu durumda önceki karenin state/args'ı olduğu gibi kalır; çağıran hata sonucunda
        /// çizimi atlamalıdır. chunkCount 0 ise Reset + FinalizeArgs yine çalışır: instanceCount 0 olur, eski kareden
        /// kalan sayı çizilmez.
        /// </summary>
        public bool Dispatch(GrassGpuResources resources, in GrassTerrainSamplingInfo terrain, Texture heightmap,
                             Texture alphamap, in GrassGenerateSettings settings, LodDistanceTable lods,
                             in GrassViewParams view, int chunkCount, out string error)
        {
            if (resources == null || resources.IsDisposed) { error = "GrassGpuResources yok/dispose edilmiş."; return false; }
            if (heightmap == null) { error = "Heightmap dokusu null."; return false; }
            if (alphamap == null) { error = "Alphamap dokusu null."; return false; }
            if (lods == null) { error = "LodDistanceTable null."; return false; }
            if (lods.Count != resources.LodCount)
            {
                error = $"LOD tablosu {lods.Count} LOD içeriyor, kaynaklar {resources.LodCount} LOD için oluşturulmuş.";
                return false;
            }
            if (!view.Validate(out error)) return false;
            if (chunkCount < 0 || chunkCount > resources.ChunkCapacity)
            {
                error = $"chunkCount {chunkCount} kapasite dışı (0..{resources.ChunkCapacity}).";
                return false;
            }

            terrain.ApplyTo(_shader);
            _shader.SetFloat(CellSizeId, settings.CellSize);
            _shader.SetInt(CellsPerAxisId, settings.CellsPerChunkAxis);
            _shader.SetInt(SeedId, unchecked((int)settings.Seed));
            _shader.SetInt(ChunkCountId, chunkCount);
            _shader.SetVector(LayerDensityId, settings.LayerDensity);
            _shader.SetVector(SlopeAnglesId, settings.SlopeAnglesRad);
            _shader.SetVector(HeightRangeId, settings.HeightRange);
            _shader.SetVector(WidthRangeId, settings.WidthRange);
            BindLods(resources, lods);
            BindView(view);

            _shader.SetBuffer(_kernelReset, StateId, resources.State);
            _shader.Dispatch(_kernelReset, 1, 1, 1);

            if (chunkCount > 0)
            {
                _shader.SetBuffer(_kernelGenerate, ChunksId, resources.Chunks);
                _shader.SetBuffer(_kernelGenerate, StateId, resources.State);
                // Üç UAV de bağlı olmalı; kullanılmayan slotlar kaynakların yer tutucu buffer'ındadır (aynı buffer'ı
                // birden çok slota bağlamak D3D11'de yinelenen binding'i düşürür). lod < _LodCount => onlara yazılmaz.
                for (int i = 0; i < GrassGpuResources.MaxLodCount; i++)
                    _shader.SetBuffer(_kernelGenerate, InstancesIds[i], resources.Instances(i));
                _shader.SetTexture(_kernelGenerate, HeightmapId, heightmap);
                _shader.SetTexture(_kernelGenerate, Alphamap0Id, alphamap);

                int groups = (settings.CellsPerChunkAxis + GroupSize - 1) / GroupSize;
                _shader.Dispatch(_kernelGenerate, groups, groups, chunkCount);
            }

            _shader.SetBuffer(_kernelFinalize, StateId, resources.State);
            _shader.SetBuffer(_kernelFinalize, ArgsId, resources.Args);
            _shader.Dispatch(_kernelFinalize, 1, 1, 1);

            error = null;
            return true;
        }

        void BindLods(GrassGpuResources resources, LodDistanceTable lods)
        {
            _shader.SetInt(LodCountId, lods.Count);
            _shader.SetVector(LodMaxDistSqId, Pack(lods.MaxDistanceSq));
            _shader.SetVector(LodKeepId, Pack(lods.KeepRatio));
            _shader.SetVector(LodWidthCompId, Pack(lods.WidthCompensation));
            _shader.SetVector(LodBandId, Pack(lods.TransitionBand));

            // Bütçe = LOD buffer kapasitesi: compute buffer'ın ötesine asla yazmaz.
            for (int i = 0; i < 4; i++) _budgetScratch[i] = i < resources.LodCount ? resources.InstanceCapacity(i) : 0;
            _shader.SetInts(LodBudgetId, _budgetScratch);
        }

        void BindView(in GrassViewParams view)
        {
            _shader.SetVector(CameraPosId, view.CameraPosition);
            _shader.SetFloat(CullPaddingId, view.CullPadding);
            bool frustum = view.FrustumPlanes != null;
            _shader.SetInt(FrustumEnabledId, frustum ? 1 : 0);
            if (frustum) _shader.SetVectorArray(FrustumPlanesId, view.FrustumPlanes);
        }

        // xyz = LOD 0..2; eksik LOD'lar son değeri tekrarlar (compute zaten _LodCount'a kadar okur).
        static Vector4 Pack(float[] values)
        {
            var v = new Vector4();
            for (int i = 0; i < 4; i++) v[i] = values[Mathf.Min(i, values.Length - 1)];
            return v;
        }
    }
}
