using System;
using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bake edilmiş exclusion mask asset'i: R8 texture + bake anındaki geometri + kaynak hash'i.
    /// Neden texture SO'nun sub-asset'i DEĞİL yanında ayrı asset: sub-asset SO'yu yeniden yazarken referansı
    /// kırılabiliyor ve texture ayrı Inspector'da görülemiyor; ayrı .asset'in GUID'i sabit kalır, içerik
    /// güncellenirken SO referansı değişmez. Yazma işi Editor baker'ında; runtime (6d/6e) yalnızca okur.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassExclusionMask", menuName = "Adan Bye/Grass/Exclusion Mask")]
    public sealed class GrassExclusionMask : ScriptableObject
    {
        [SerializeField] Texture2D texture;
        [SerializeField] Vector2 originXZ;
        [SerializeField] Vector2 sizeXZ;
        [SerializeField] int resolution;
        [SerializeField] string sourceHash = string.Empty;

        public Texture2D Texture => texture;
        public Vector2 OriginXZ => originXZ;
        public Vector2 SizeXZ => sizeXZ;
        public int Resolution => resolution;
        public string SourceHash => sourceHash;

        /// <summary>Bake sonucunu tek seferde yazar (kısmi güncelleme tutarsız duruma yol açmasın).</summary>
        public void Apply(Texture2D bakedTexture, Vector2 origin, Vector2 size, int res, string hash)
        {
            if (bakedTexture == null) throw new ArgumentNullException(nameof(bakedTexture));
            if (res < 2) throw new ArgumentOutOfRangeException(nameof(res));
            texture = bakedTexture;
            originXZ = origin;
            sizeXZ = size;
            resolution = res;
            sourceHash = hash ?? string.Empty;
        }

        /// <summary>Kaynaklar bake'ten sonra değiştiyse true. Texture yoksa da eski sayılır.</summary>
        public bool IsStale(string currentHash) =>
            texture == null || !string.Equals(sourceHash, currentHash, StringComparison.Ordinal);
    }
}
