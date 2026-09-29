using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Compute'a gidecek exclusion mask bağlaması: texture + dünya XZ dikdörtgeni + çözünürlük. Neden ayrı struct:
    /// SO'dan okuma/doğrulama (Unity nesnesi) ile compute'a yazma (dispatcher) ayrılsın; doğrulama SO'suz test edilebilsin.
    /// Texture null => mask yok; dispatcher 1x1 siyah fallback bağlar, sonuç maskesiz hâlle birebir aynıdır.
    /// </summary>
    public readonly struct GrassExclusionBinding
    {
        /// <summary>Mask origin/size'ının terrain'inkinden bu kadar (m) sapması uyarı üretir.</summary>
        public const float MismatchToleranceMeters = 0.01f;

        public Texture2D Texture { get; }
        /// <summary>xy = origin XZ, zw = size XZ (dünya, m).</summary>
        public Vector4 Rect { get; }
        public int Resolution { get; }
        public bool IsActive => Texture != null && Resolution > 1;

        public GrassExclusionBinding(Texture2D texture, Vector2 originXZ, Vector2 sizeXZ, int resolution)
        {
            Texture = texture;
            Rect = new Vector4(originXZ.x, originXZ.y, sizeXZ.x, sizeXZ.y);
            Resolution = resolution;
        }

        public static GrassExclusionBinding None => default;

        /// <summary>
        /// Mask'tan bağlama üretir. Mask yoksa None (uyarısız: opsiyonel özellik). Texture'sız / geçersiz geometrili
        /// mask None + uyarı; terrain ile uyuşmazlık uyarı ama YİNE DE bağlanır (kullanıcı görsün, çim kaybolmasın).
        /// </summary>
        public static GrassExclusionBinding FromMask(GrassExclusionMask mask, Vector3 terrainOrigin, Vector3 terrainSize,
                                                     ValidationReport report)
        {
            if (mask == null) return None;
            if (mask.Texture == null)
            {
                report?.AddWarning($"Exclusion mask '{mask.name}' bake edilmemiş (texture yok); exclusion uygulanmıyor.");
                return None;
            }
            Vector2 size = mask.SizeXZ;
            bool sane = mask.Resolution >= 2 && size.x > 0f && size.y > 0f &&
                        !float.IsNaN(size.x + size.y + mask.OriginXZ.x + mask.OriginXZ.y);
            if (!sane)
            {
                report?.AddWarning($"Exclusion mask '{mask.name}' geometrisi geçersiz (çözünürlük/boyut); exclusion uygulanmıyor.");
                return None;
            }
            if (mask.Texture.width != mask.Resolution || mask.Texture.height != mask.Resolution)
            {
                report?.AddWarning($"Exclusion mask '{mask.name}' texture boyutu ({mask.Texture.width}x{mask.Texture.height}) kayıtlı çözünürlükle ({mask.Resolution}) uyuşmuyor; yeniden bake edin. Exclusion uygulanmıyor.");
                return None;
            }

            if (!MatchesTerrain(mask.OriginXZ, size, terrainOrigin, terrainSize))
                report?.AddWarning($"Exclusion mask '{mask.name}' terrain ile uyuşmuyor (mask origin {mask.OriginXZ} size {size}; terrain origin ({terrainOrigin.x}, {terrainOrigin.z}) size ({terrainSize.x}, {terrainSize.z})). Mask'ı yeniden bake edin.");

            return new GrassExclusionBinding(mask.Texture, mask.OriginXZ, size, mask.Resolution);
        }

        public static bool MatchesTerrain(Vector2 maskOrigin, Vector2 maskSize, Vector3 terrainOrigin, Vector3 terrainSize) =>
            Mathf.Abs(maskOrigin.x - terrainOrigin.x) <= MismatchToleranceMeters &&
            Mathf.Abs(maskOrigin.y - terrainOrigin.z) <= MismatchToleranceMeters &&
            Mathf.Abs(maskSize.x - terrainSize.x) <= MismatchToleranceMeters &&
            Mathf.Abs(maskSize.y - terrainSize.z) <= MismatchToleranceMeters;
    }
}
