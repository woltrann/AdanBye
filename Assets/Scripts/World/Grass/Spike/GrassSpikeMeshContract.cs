using System.Collections.Generic;
using UnityEngine;

namespace AdanBye.Grass.Spike
{
    /// <summary>
    /// Kullanıcının verdiği blade mesh'inin sözleşmeye uyup uymadığını denetler (saf mantık, sahneye/Mesh asset'ine yazmaz).
    /// Sözleşme: pivot kökte (0,0,0), +Y yukarı, ~1 birim yükseklik ve ~1 birim taban genişliği; instance
    /// ölçeği (width, height, width) ile uygulandığı için mesh "birim boyutta" olmalıdır.
    /// </summary>
    public static class GrassSpikeMeshContract
    {
        /// <summary>Pivot kökte değilse bile blade'i tamamen bozmayan tolerans (metre, birim mesh uzayında).</summary>
        public const float MinYTolerance = -0.05f;
        public const float MinHeight = 0.25f;
        public const float MaxHeight = 4f;

        /// <summary>Mesh hiç çizilemezse false + hata metni. Bounds/vertexCount okumak isReadable GEREKTİRMEZ.</summary>
        public static bool IsUsable(int vertexCount, out string error)
        {
            error = vertexCount > 0 ? null : "Mesh'te vertex yok (vertexCount=0).";
            return vertexCount > 0;
        }

        /// <summary>Çizilebilir ama sözleşmeyi ihlal eden durumlar için uyarı metinleri ekler.</summary>
        public static void CollectWarnings(int subMeshCount, Bounds bounds, List<string> warnings)
        {
            if (subMeshCount > 1)
                warnings.Add($"subMeshCount={subMeshCount}; yalnızca submesh 0 çizilir.");

            if (bounds.min.y < MinYTolerance)
                warnings.Add($"pivot kökte değil: bounds.min.y={bounds.min.y:F3} (beklenen ~0; taban yerin altına gömülür).");

            float height = bounds.size.y;
            if (height < MinHeight || height > MaxHeight)
                warnings.Add($"ölçek beklenenden farklı: bounds.size.y={height:F3} (beklenen ~1; {MinHeight}..{MaxHeight} dışı). " +
                             "Instance ölçeği (width,height,width) ile uygulandığından blade çok küçük/büyük görünür.");
        }
    }
}
