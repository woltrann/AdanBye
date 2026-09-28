using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bir terrain layer'ı için çim kuralı (saf veri; TerrainLayer nesnesi yok — layer, alphamap indeksiyle
    /// tanımlanır, bu sayede sınıf Terrain olmadan test edilir).
    /// </summary>
    public readonly struct LayerDensityRule
    {
        /// <summary>Terrain'deki layer indeksi (alphamap kanalı = indeks; tek RGBA doku varsayımı).</summary>
        public readonly int LayerIndex;

        /// <summary>
        /// Beklenen layer adı (opsiyonel, null = kontrol etme). Terrain'de bu indeksteki isim farklıysa uyarı:
        /// layer'lar yeniden sıralanınca kural sessizce yanlış layer'a bağlanmasın.
        /// </summary>
        public readonly string ExpectedLayerName;

        public readonly float DensityMultiplier;
        public readonly Color RootTint;
        public readonly Color TipTint;
        public readonly float HeightMultiplier;

        public LayerDensityRule(int layerIndex, string expectedLayerName, float densityMultiplier,
                                Color rootTint, Color tipTint, float heightMultiplier)
        {
            LayerIndex = layerIndex;
            ExpectedLayerName = expectedLayerName;
            DensityMultiplier = densityMultiplier;
            RootTint = rootTint;
            TipTint = tipTint;
            HeightMultiplier = heightMultiplier;
        }
    }

    /// <summary>
    /// Layer kurallarını, tek RGBA alphamap dokusunun 4 kanalına karşılık gelen compute girdilerine çevirir:
    /// float4 yoğunluk çarpanı (kanal başına), float4 yükseklik çarpanı ve 4'er kök/uç tint'i. Compute, splat
    /// ağırlığı (rgba) ile bu çarpanları dot/ağırlıklı toplarla birleştirir. Kuralı olmayan kanalın yoğunluğu 0
    /// (çim yok). Neden 4 layer sınırı: mevcut terrain tek alphamap dokusu (512, 4 layer) kullanıyor; 4'ten
    /// fazlası ikinci doku ve ikinci örnekleme gerektirir — sessizce yanlış çalışmak yerine açık hata verilir.
    /// </summary>
    public sealed class LayerDensityMapper
    {
        /// <summary>Tek RGBA alphamap dokusunun kanal sayısı.</summary>
        public const int MaxLayers = 4;

        /// <summary>x=R, y=G, z=B, w=A kanalının yoğunluk çarpanı.</summary>
        public Vector4 DensityMultipliers { get; }

        public Vector4 HeightMultipliers { get; }

        /// <summary>4 elemanlı; SetVectorArray'e doğrudan gider. DEĞİŞTİRME (paylaşılan).</summary>
        public Vector4[] RootTints { get; }
        public Vector4[] TipTints { get; }

        LayerDensityMapper(Vector4 density, Vector4 height, Vector4[] roots, Vector4[] tips)
        {
            DensityMultipliers = density;
            HeightMultipliers = height;
            RootTints = roots;
            TipTints = tips;
        }

        /// <summary>Splat ağırlığından (alphamap rgba) çim yoğunluk çarpanı — compute'un CPU aynası.</summary>
        public float EvaluateDensity(Vector4 splat) => Vector4.Dot(splat, DensityMultipliers);

        /// <param name="terrainLayerNames">Terrain layer'larının alphamap sırasıyla isimleri.</param>
        /// <param name="rules">Çim kuralları; sıra önemsiz, layer başına en fazla bir kural.</param>
        public static bool TryCreate(IReadOnlyList<string> terrainLayerNames, IReadOnlyList<LayerDensityRule> rules,
                                     out LayerDensityMapper mapper, out ValidationReport report)
        {
            mapper = null;
            report = new ValidationReport();

            if (terrainLayerNames == null || terrainLayerNames.Count == 0)
            {
                report.AddError("Terrain layer listesi boş: alphamap kanalı eşlenemez.");
                return false;
            }
            if (rules == null || rules.Count == 0)
            {
                report.AddError("Çim layer kuralı listesi boş: hiçbir layer'da çim üretilmez.");
                return false;
            }

            int layerCount = terrainLayerNames.Count;
            if (layerCount > MaxLayers)
            {
                report.AddWarning($"Terrain {layerCount} layer içeriyor; yalnızca ilk {MaxLayers} layer (tek alphamap dokusu) " +
                                  "desteklenir, sonrakilerde çim üretilmez.");
            }

            var density = new float[MaxLayers];
            var height = new float[MaxLayers] { 1f, 1f, 1f, 1f };
            var roots = new Vector4[MaxLayers];
            var tips = new Vector4[MaxLayers];
            var hasRule = new bool[MaxLayers];
            // Kuralsız kanal: nötr tint (yoğunluk zaten 0, tint hiç görünmez).
            for (int i = 0; i < MaxLayers; i++) { roots[i] = Vector4.one; tips[i] = Vector4.one; }

            for (int r = 0; r < rules.Count; r++)
            {
                LayerDensityRule rule = rules[r];
                int index = rule.LayerIndex;

                if (index < 0 || index >= layerCount)
                {
                    report.AddError($"Kural {r}: layerIndex {index} terrain'de yok (layer sayısı {layerCount}).");
                    continue;
                }
                if (index >= MaxLayers)
                {
                    report.AddError($"Kural {r}: layerIndex {index} ikinci alphamap dokusuna düşer; tek RGBA doku " +
                                    $"yalnızca ilk {MaxLayers} layer'ı destekler.");
                    continue;
                }
                if (hasRule[index])
                {
                    report.AddError($"Kural {r}: layer {index} için birden fazla kural var.");
                    continue;
                }

                string actualName = terrainLayerNames[index];
                if (rule.ExpectedLayerName != null && rule.ExpectedLayerName != actualName)
                {
                    report.AddWarning($"Kural {r}: layer {index} beklenen '{rule.ExpectedLayerName}' değil, terrain'de '{actualName}'. " +
                                      "Layer'lar yeniden sıralanmış olabilir.");
                }

                bool ok = true;
                if (!IsFinite(rule.DensityMultiplier) || rule.DensityMultiplier < 0f)
                {
                    report.AddError($"Kural {r} (layer {index}): densityMultiplier sonlu ve >= 0 olmalı (değer {rule.DensityMultiplier}).");
                    ok = false;
                }
                if (!IsFinite(rule.HeightMultiplier) || rule.HeightMultiplier <= 0f)
                {
                    report.AddError($"Kural {r} (layer {index}): heightMultiplier sonlu ve > 0 olmalı (değer {rule.HeightMultiplier}).");
                    ok = false;
                }
                if (!IsFinite(rule.RootTint) || !IsFinite(rule.TipTint))
                {
                    report.AddError($"Kural {r} (layer {index}): tint değerleri sonlu olmalı (NaN/Inf).");
                    ok = false;
                }
                if (!ok) continue;

                hasRule[index] = true;
                density[index] = rule.DensityMultiplier;
                height[index] = rule.HeightMultiplier;
                roots[index] = rule.RootTint;
                tips[index] = rule.TipTint;
            }

            // Kuralı olmayan (ama alphamap'te bulunan) layer sessizce çimsiz kalır; bunu fark ettir.
            int mappedLayers = layerCount < MaxLayers ? layerCount : MaxLayers;
            for (int i = 0; i < mappedLayers; i++)
            {
                if (!hasRule[i])
                    report.AddWarning($"Terrain layer {i} ('{terrainLayerNames[i]}') için çim kuralı yok; orada çim üretilmez.");
            }

            if (!report.IsValid) return false;

            mapper = new LayerDensityMapper(
                new Vector4(density[0], density[1], density[2], density[3]),
                new Vector4(height[0], height[1], height[2], height[3]),
                roots, tips);
            return true;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static bool IsFinite(Color c) => IsFinite(c.r) && IsFinite(c.g) && IsFinite(c.b) && IsFinite(c.a);
    }
}
