using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// 6 düzlemden oluşan <see cref="IFrustum"/>. Düzlem dizisi bir kez ayrılır ve her frame yeniden doldurulur:
    /// GeometryUtility.CalculateFrustumPlanes(Matrix4x4, Plane[]) aşırı yüklemesi allocation yapmaz, dolayısıyla
    /// kamera başına per-frame kullanım GC üretmez.
    /// </summary>
    public sealed class PlanesFrustum : IFrustum
    {
        readonly Plane[] _planes = new Plane[6];

        /// <summary>Kamera projeksiyon*görünüm matrisinden düzlemleri günceller (allocation yok).</summary>
        public void Update(Matrix4x4 viewProjection)
        {
            GeometryUtility.CalculateFrustumPlanes(viewProjection, _planes);
        }

        public bool Intersects(Bounds bounds) => GeometryUtility.TestPlanesAABB(_planes, bounds);

        /// <summary>
        /// Düzlemleri compute'a gidecek biçimde (xyz = iç yönlü normal, w = uzaklık; n.p + w &gt;= 0 içeride) verilen
        /// 6 elemanlı diziye yazar. Dizi çağıranda tutulur => allocation yok.
        /// </summary>
        public void CopyPlanes(Vector4[] destination)
        {
            if (destination == null || destination.Length < _planes.Length)
                throw new System.ArgumentException("destination en az 6 eleman içermeli.", nameof(destination));
            for (int i = 0; i < _planes.Length; i++)
                destination[i] = new Vector4(_planes[i].normal.x, _planes[i].normal.y, _planes[i].normal.z, _planes[i].distance);
        }
    }
}
