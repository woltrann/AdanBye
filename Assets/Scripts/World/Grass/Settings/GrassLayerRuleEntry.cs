using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Inspector'da düzenlenen tek terrain layer çim kuralı. Layer, TerrainLayer nesnesiyle değil alphamap indeksiyle
    /// tanımlanır (Core'daki <see cref="LayerDensityRule"/> ile aynı gerekçe: Terrain olmadan test edilebilirlik).
    /// </summary>
    [Serializable]
    public sealed class GrassLayerRuleEntry
    {
        [Tooltip("Terrain layer indeksi = alphamap kanalı (0..3, tek RGBA doku).")]
        public int layerIndex;

        [Tooltip("Opsiyonel: terrain'de bu indeksteki layer adı. Boş = kontrol etme. Layer'lar yeniden sıralanırsa uyarı verir.")]
        public string expectedLayerName;

        [Tooltip("Yoğunluk çarpanı (>= 0). 0 = bu layer'da çim yok.")]
        public float densityMultiplier = 1f;

        public Color rootTint = Color.white;
        public Color tipTint = Color.white;

        [Tooltip("Boy çarpanı (> 0).")]
        public float heightMultiplier = 1f;

        public LayerDensityRule ToRule(bool includeExpectedName)
        {
            // Boş metin "kontrol etme" demektir; Core null bekler (boş string'i beklenen isim sanıp yanlış uyarı vermesin).
            string expected = includeExpectedName && !string.IsNullOrEmpty(expectedLayerName) ? expectedLayerName : null;
            return new LayerDensityRule(layerIndex, expected, densityMultiplier, rootTint, tipTint, heightMultiplier);
        }

        /// <summary>
        /// Varsayılan (mevcut terrain layer asset adları: Grass_A_TerrainLayer, Grass_Dry_TerrainLayer, Snow_TerrainLayer, Muddy_TerrainLayer; TerrainData.terrainLayers[i].name asset adını verir): yoğunluk 1 / 0.6 / 0 / 0.15.
        /// Tint ve boy çarpanları ilk değer olarak makul yer tutuculardır (sanat yönü kararı değil).
        /// </summary>
        public static List<GrassLayerRuleEntry> CreateDefaults() => new List<GrassLayerRuleEntry>
        {
            new GrassLayerRuleEntry
            {
                layerIndex = 0, expectedLayerName = "Grass_A_TerrainLayer", densityMultiplier = 1f, heightMultiplier = 1f,
                rootTint = new Color(0.12f, 0.25f, 0.06f), tipTint = new Color(0.45f, 0.65f, 0.18f),
            },
            new GrassLayerRuleEntry
            {
                layerIndex = 1, expectedLayerName = "Grass_Dry_TerrainLayer", densityMultiplier = 0.6f, heightMultiplier = 0.8f,
                rootTint = new Color(0.35f, 0.30f, 0.12f), tipTint = new Color(0.75f, 0.65f, 0.30f),
            },
            new GrassLayerRuleEntry
            {
                layerIndex = 2, expectedLayerName = "Snow_TerrainLayer", densityMultiplier = 0f, heightMultiplier = 1f,
                rootTint = new Color(0.90f, 0.92f, 0.95f), tipTint = new Color(0.90f, 0.92f, 0.95f),
            },
            new GrassLayerRuleEntry
            {
                layerIndex = 3, expectedLayerName = "Muddy_TerrainLayer", densityMultiplier = 0.15f, heightMultiplier = 0.5f,
                rootTint = new Color(0.20f, 0.15f, 0.08f), tipTint = new Color(0.40f, 0.35f, 0.20f),
            },
        };
    }
}
