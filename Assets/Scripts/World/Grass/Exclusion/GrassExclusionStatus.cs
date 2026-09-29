using UnityEngine;

namespace AdanBye.Grass
{
    public enum ExclusionMaskState { None, NotBaked, GeometryMismatch, Ok }

    /// <summary>
    /// Inspector için mask durum sınıflandırması. Neden ayrı saf sınıf: Editor GUI'sinden bağımsız test edilsin;
    /// terrain karşılaştırması GrassExclusionBinding.MatchesTerrain ile aynı kuralı kullanır (tekrar yazılmaz).
    /// </summary>
    public static class GrassExclusionStatus
    {
        public static ExclusionMaskState Classify(GrassExclusionMask mask, Vector3 terrainOrigin, Vector3 terrainSize) =>
            Classify(mask != null, mask != null ? mask.Texture != null : false,
                     mask != null ? mask.OriginXZ : default, mask != null ? mask.SizeXZ : default,
                     terrainOrigin, terrainSize);

        public static ExclusionMaskState Classify(bool hasMask, bool hasTexture, Vector2 maskOrigin, Vector2 maskSize,
                                                  Vector3 terrainOrigin, Vector3 terrainSize)
        {
            if (!hasMask) return ExclusionMaskState.None;
            if (!hasTexture) return ExclusionMaskState.NotBaked;
            return GrassExclusionBinding.MatchesTerrain(maskOrigin, maskSize, terrainOrigin, terrainSize)
                ? ExclusionMaskState.Ok
                : ExclusionMaskState.GeometryMismatch;
        }
    }
}
