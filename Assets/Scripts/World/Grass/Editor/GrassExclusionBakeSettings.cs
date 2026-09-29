using UnityEngine;

namespace AdanBye.Grass.Editor
{
    /// <summary>
    /// Bake ayarları (Editor-only asset). Sahne referansı SO'da tutulamadığı için göl, adı verilen
    /// GameObject'lerin (ve çocuklarının) MeshFilter'larından bulunur; terrain aktif terrain'dir.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassExclusionBakeSettings", menuName = "Adan Bye/Grass/Exclusion Bake Settings")]
    public sealed class GrassExclusionBakeSettings : ScriptableObject
    {
        [Tooltip("Bake sonucunun yazılacağı mask asset'i.")]
        public GrassExclusionMask targetMask;

        [Min(2)] public int resolution = 2048;

        [Header("Ağaç")]
        public float treeMargin = 1.5f;
        [Tooltip("Prototip indeksine göre ölçek-1 yarıçap (metre). 0 veya negatif = prefab bounds kullan.")]
        public float[] treeRadiusPerPrototype = new float[0];

        [Header("Göl")]
        public float lakeMargin = 1.0f;
        [Tooltip("Açıksa göl yalnız terrain yüksekliği su yüzeyinin altında kalan yerlerde maskelenir (yer altındaki plane kısmı çimi silmez).")]
        public bool clipLakeToTerrainHeight = true;
        [Tooltip("Aktif sahnede bu adlı GameObject'ler ve çocuklarındaki MeshFilter'lar göl sayılır (büyük/küçük harf duyarsız).")]
        public string[] lakeObjectNames = { "Lake" };
    }
}
