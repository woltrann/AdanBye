using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Çim sisteminin Inspector'dan düzenlenen ayarları. Yalnızca veri + saf dönüştürme taşır; doğrulama mantığı
    /// Core'da (LodDistanceTable, LayerDensityMapper, GrassGenerateSettings) kalır ve burada YENİDEN yazılmaz:
    /// <see cref="TryBuildRuntime(out GrassRuntimeConfig, out ValidationReport)"/> ham alanları o doğrulayıcılara
    /// besler ve sonucu tek <see cref="GrassRuntimeConfig"/>'e toplar. Çizim/GPU bu sınıfı değil config'i kullanır.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassSettings", menuName = "Adan Bye/Grass/Grass Settings")]
    public sealed class GrassSettings : ScriptableObject
    {
        // Varsayılanlar tek yerde: hem alan başlatıcıları hem ResetToDefaults kullanır (biri değişirse diğeri sapmasın).
        const float DefaultDrawDistance = 120f;
        const float DefaultChunkSize = 16f;
        const float DefaultMaxDensityPerM2 = 16f;
        const int DefaultSeed = 12345;
        const float DefaultSlopeMinDeg = 30f;
        const float DefaultSlopeMaxDeg = 55f;
        const float DefaultCullPadding = 0.3f;
        const float DefaultColorJitter = 0.08f;
        static readonly Vector2 DefaultHeightRange = new Vector2(0.4f, 0.8f);
        static readonly Vector2 DefaultWidthRange = new Vector2(0.06f, 0.10f);

        [Header("Mesafe")]
        [Tooltip("Chunk seçiminin kamera uzaklık sınırı (m). Son LOD'un maxDistance'ından küçük olamaz; büyükse fazlası boşa chunk taranır.")]
        [SerializeField] float drawDistance = DefaultDrawDistance;

        [Tooltip("Chunk kenarı (m). Hücre ızgarası chunk sınırına oturur.")]
        [SerializeField] float chunkSize = DefaultChunkSize;

        [Tooltip("Üst yoğunluk sınırı (adet/m2). Hücre boyutu buradan türer; gerçek yoğunluk yuvarlama yüzünden biraz yüksek olabilir.")]
        [SerializeField] float maxDensityPerM2 = DefaultMaxDensityPerM2;

        [Tooltip("Yerleşim tohumu: aynı seed aynı çim dağılımını verir.")]
        [SerializeField] int seed = DefaultSeed;

        [Header("Eğim (derece)")]
        [Tooltip("Bu eğime kadar tam yoğunluk.")]
        [SerializeField] float slopeMinDeg = DefaultSlopeMinDeg;
        [Tooltip("Bu eğimden sonra çim yok.")]
        [SerializeField] float slopeMaxDeg = DefaultSlopeMaxDeg;

        [Header("Blade boyutu (x = min, y = max)")]
        [SerializeField] Vector2 heightRange = DefaultHeightRange;
        [SerializeField] Vector2 widthRange = DefaultWidthRange;

        [Header("Renk")]
        [Tooltip("Blade başına parlaklık jitter'ı: uç rengi 1 +- bu oranda çarpılır (0..0.5). 0 = layer rengi birebir.")]
        [SerializeField] float colorJitter = DefaultColorJitter;

        [Header("Culling")]
        [Tooltip("Blade frustum culling küresine eklenen pay (m); rüzgar/ezme eğilmesini kesmemek için.")]
        [SerializeField] float cullPadding = DefaultCullPadding;

        [Header("LOD (yakından uzağa; en fazla 3)")]
        [SerializeField] List<GrassLodEntry> lods = new List<GrassLodEntry>(GrassLodEntry.CreateDefaults());

        [Header("Terrain layer kuralları (layer başına en fazla bir)")]
        [SerializeField] List<GrassLayerRuleEntry> layerRules = GrassLayerRuleEntry.CreateDefaults();

        /// <summary>Tüm alanları plan varsayılanlarına döndürür (Inspector düğmesi ve Reset() kullanır).</summary>
        public void ResetToDefaults()
        {
            drawDistance = DefaultDrawDistance;
            chunkSize = DefaultChunkSize;
            maxDensityPerM2 = DefaultMaxDensityPerM2;
            seed = DefaultSeed;
            slopeMinDeg = DefaultSlopeMinDeg;
            slopeMaxDeg = DefaultSlopeMaxDeg;
            heightRange = DefaultHeightRange;
            widthRange = DefaultWidthRange;
            cullPadding = DefaultCullPadding;
            colorJitter = DefaultColorJitter;
            lods = new List<GrassLodEntry>(GrassLodEntry.CreateDefaults());
            layerRules = GrassLayerRuleEntry.CreateDefaults();
        }

        // Unity, asset ilk oluşturulurken ve bileşen menüsünden "Reset" seçilince çağırır.
        void Reset() => ResetToDefaults();

        /// <summary>
        /// Inspector'da (veya geri al/yinele ile) bir alan değişince tetiklenir. Neden: çalışan renderer'ın snapshot'ı
        /// (GrassRuntimeConfig) SO'dan kopyadır; SO değişince renderer bu olayla sistemini yeniden kurar.
        /// Yalnızca Editor'da tetiklenir (OnValidate Editor mesajıdır); oyunda ayar değişimi ayrı bir Rebuild ister.
        /// </summary>
        public event System.Action Changed;

        void OnValidate() => Changed?.Invoke();

        /// <summary>
        /// Terrain'e bakmadan dönüştürür: layer kuralları yalnızca 4 alphamap kanalına karşı doğrulanır ve
        /// <c>expectedLayerName</c> denetlenmez (terrain adları henüz bilinmiyor). Editor Inspector'ı bunu kullanır.
        /// </summary>
        public bool TryBuildRuntime(out GrassRuntimeConfig config, out ValidationReport report)
            => TryBuildRuntime(null, out config, out report);

        /// <summary>
        /// Ayarları doğrulayıp <see cref="GrassRuntimeConfig"/>'e çevirir. Neden istisna değil rapor: aynı anda birden fazla
        /// alan yanlış olabilir; TÜM bağımsız doğrulayıcılar başarısızlıktan bağımsız çalışır ve hataları tek raporda
        /// toplar (Core'daki desenle aynı). <paramref name="config"/> yalnızca <c>report.IsValid</c> iken doludur.
        /// </summary>
        /// <param name="terrainLayerNames">
        /// Terrain layer adları (alphamap sırasıyla). Verilirse layerIndex terrain'e ve ExpectedLayerName gerçek adlara
        /// karşı denetlenir (çizim katmanı böyle çağırır); null ise yalnızca 4 kanala karşı doğrulanır.
        /// </param>
        public bool TryBuildRuntime(IReadOnlyList<string> terrainLayerNames, out GrassRuntimeConfig config, out ValidationReport report)
        {
            config = null;
            report = new ValidationReport();

            // 1) LOD: ham girdiyi Core'un LodLevelSpec'ine çevir, kapasite/mesh'i ayır, doğrulamayı Core'a bırak.
            LodDistanceTable lodTable = null;
            int[] budgets = null;
            Mesh[] meshes = null;
            if (TryCollectLods(report, out List<LodLevelSpec> specs, out budgets, out meshes))
            {
                LodDistanceTable.TryCreate(specs, out lodTable, out ValidationReport lodReport);
                report.Merge(lodReport);
            }

            ValidateDrawDistance(report);
            if (!IsFinite(cullPadding) || cullPadding < 0f)
                report.AddError($"cullPadding sonlu ve >= 0 olmalı (değer {cullPadding}).");

            // 2) Layer kuralları.
            LayerDensityMapper mapper = null;
            if (TryCollectLayerRules(report, terrainLayerNames != null, out List<LayerDensityRule> rules))
            {
                IReadOnlyList<string> names = terrainLayerNames ?? UnknownTerrainLayerNames();
                LayerDensityMapper.TryCreate(names, rules, out mapper, out ValidationReport layerReport);
                report.Merge(layerReport);
            }

            // 3) Üretim parametreleri. Layer başarısızsa sıfır yoğunlukla yine denenir: diğer alanların (chunk, eğim,
            // boy...) hataları layer hatasının arkasında gizli kalmasın.
            Vector4 layerDensity = mapper != null ? mapper.DensityMultipliers : Vector4.zero;
            Vector4[] layerTints = mapper != null ? mapper.LayerTints : NeutralLayerTints();
            bool generateOk = GrassGenerateSettings.TryCreate(chunkSize, maxDensityPerM2, unchecked((uint)seed), layerDensity,
                                                              layerTints, colorJitter, slopeMinDeg, slopeMaxDeg, heightRange, widthRange,
                                                              out GrassGenerateSettings generate, out string generateError);
            if (!generateOk) report.AddError(generateError);

            if (!report.IsValid) return false;

            config = new GrassRuntimeConfig(generate, lodTable, mapper, drawDistance, cullPadding, budgets, meshes);
            return true;
        }

        bool TryCollectLods(ValidationReport report, out List<LodLevelSpec> specs, out int[] budgets, out Mesh[] meshes)
        {
            specs = new List<LodLevelSpec>();
            budgets = null;
            meshes = null;

            int count = lods != null ? lods.Count : 0;
            var budgetList = new int[count];
            var meshList = new Mesh[count];
            bool complete = true;
            for (int i = 0; i < count; i++)
            {
                GrassLodEntry entry = lods[i];
                if (entry == null)
                {
                    // Null girdi ile LOD indeksleri kayardı; sahte hatalar üretmemek için LOD doğrulamasını atla.
                    report.AddError($"LOD{i}: girdi null.");
                    complete = false;
                    continue;
                }

                if (entry.instanceBudget < 1 || entry.instanceBudget > GrassGpuResources.MaxInstanceCapacity)
                    report.AddError($"LOD{i}: instanceBudget 1..{GrassGpuResources.MaxInstanceCapacity} aralığında olmalı (değer {entry.instanceBudget}).");

                specs.Add(entry.ToSpec());
                budgetList[i] = entry.instanceBudget;
                meshList[i] = entry.mesh;
            }

            // Boş liste de Core'a gider: "LOD listesi boş" mesajı tek yerde (Core) kalsın.
            if (!complete) return false;
            budgets = budgetList;
            meshes = meshList;
            return true;
        }

        bool TryCollectLayerRules(ValidationReport report, bool checkExpectedNames, out List<LayerDensityRule> rules)
        {
            rules = new List<LayerDensityRule>();
            int count = layerRules != null ? layerRules.Count : 0;
            bool complete = true;
            for (int i = 0; i < count; i++)
            {
                if (layerRules[i] == null)
                {
                    report.AddError($"Kural {i}: girdi null.");
                    complete = false;
                    continue;
                }
                rules.Add(layerRules[i].ToRule(checkExpectedNames));
            }
            return complete;
        }

        // Layer başarısızken (yoğunluk zaten 0) üretim doğrulamasının tint dizisinden takılmaması için nötr yer tutucu.
        static Vector4[] NeutralLayerTints()
        {
            var tints = new Vector4[LayerDensityMapper.MaxLayers];
            for (int i = 0; i < tints.Length; i++) tints[i] = Vector4.one;
            return tints;
        }

        // Terrain bilinmiyorken Core'un layer eşlemesi için 4 kanallık yer tutucu adlar; expected name zaten
        // denetlenmediği için (ToRule includeExpectedName=false) adların değeri önemsiz.
        static IReadOnlyList<string> UnknownTerrainLayerNames()
        {
            var names = new string[LayerDensityMapper.MaxLayers];
            for (int i = 0; i < names.Length; i++) names[i] = "Layer" + i;
            return names;
        }

        // Çizim mesafesi < son LOD sonu: son LOD'un dış kısmı için chunk hiç seçilmez (LOD tablosu yalan söyler) -> hata.
        // Çizim mesafesi > son LOD sonu: chunk seçilir ama compute hepsini eler (boşa iş) -> yalnızca uyarı.
        void ValidateDrawDistance(ValidationReport report)
        {
            if (!IsFinite(drawDistance) || drawDistance <= 0f)
            {
                report.AddError($"drawDistance sonlu ve > 0 olmalı (değer {drawDistance}).");
                return;
            }

            if (lods == null || lods.Count == 0 || lods[lods.Count - 1] == null) return;
            float lastLodEnd = lods[lods.Count - 1].maxDistance;
            if (!IsFinite(lastLodEnd)) return; // LOD hatası zaten Core tarafından raporlanır.

            if (drawDistance < lastLodEnd)
                report.AddError($"drawDistance ({drawDistance}) son LOD'un maxDistance'ından ({lastLodEnd}) küçük olamaz.");
            else if (drawDistance > lastLodEnd)
                report.AddWarning($"drawDistance ({drawDistance}) son LOD'un maxDistance'ından ({lastLodEnd}) büyük; " +
                                  "aradaki mesafede çim çizilmez ama chunk'lar boşuna taranır.");
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
