using UnityEngine;

namespace AdanBye.Grass
{
    /// <summary>
    /// Bir kameranın çim için görünüm verisi: chunk seçimi için <see cref="IFrustum"/> ve compute'a giden
    /// <see cref="GrassViewParams"/> (kamera konumu + 6 düzlem). Kamera başına bir örnek tutulur; düzlem dizileri
    /// ctor'da bir kez ayrılır, <c>Update</c> allocation yapmaz (beginCameraRendering her kamera için her karede çalışır).
    /// Neden Camera yerine matris girdisi de var: birim testte kamera/pipeline gerekmeden düzlemler doğrulanabilsin.
    /// </summary>
    public sealed class GrassCameraView
    {
        readonly PlanesFrustum _frustum = new PlanesFrustum();
        readonly Vector4[] _planes = new Vector4[GrassViewParams.PlaneCount];

        /// <summary>Son <c>Update</c>'in frustum'u (chunk seçicisine verilir).</summary>
        public IFrustum Frustum => _frustum;

        /// <summary>
        /// Görünümü günceller ve compute parametrelerini döner. Dönen değerin <c>FrustumPlanes</c> dizisi bu nesnenin
        /// tek dizisidir: bir sonraki <c>Update</c>'e kadar geçerlidir (kopyasız; her kare zaten dispatch'ten önce çağrılır).
        /// </summary>
        public GrassViewParams Update(Matrix4x4 viewProjection, Vector3 cameraPosition, float cullPadding)
        {
            _frustum.Update(viewProjection);
            _frustum.CopyPlanes(_planes);
            return new GrassViewParams(cameraPosition, _planes, cullPadding);
        }

        /// <summary>
        /// Kamera kısayolu. camera.projectionMatrix (API'den bağımsız) kullanılır: GeometryUtility düzlemleri
        /// GL'e özgü GPU projeksiyonunu değil bu matrisi bekler.
        /// </summary>
        public GrassViewParams Update(Camera camera, float cullPadding)
            => Update(camera.projectionMatrix * camera.worldToCameraMatrix, camera.transform.position, cullPadding);
    }

    /// <summary>
    /// RenderMeshIndirect'in <c>worldBounds</c> kutusu. Unity bu kutuyu kameraya karşı cull eder; çim dünya uzayında
    /// GPU'da üretildiğinden Unity gerçek konumları bilmez, kutu üretilebilecek her yeri KAPSAMALI (aksi halde kamera
    /// kutuyu görmezse tüm çim kaybolur). Kutu: kamera halkası (XZ) + terrain yükseklik aralığı + blade boyu + pay.
    /// </summary>
    public static class GrassDrawBounds
    {
        /// <param name="drawDistance">Chunk seçim halkası yarıçapı (m).</param>
        /// <param name="terrainMinY">Terrain'in en alçak dünya Y'si (ör. origin.y).</param>
        /// <param name="terrainMaxY">Terrain'in en yüksek dünya Y'si (ör. origin.y + size.y).</param>
        /// <param name="maxBladeHeight">En uzun blade boyu (m; heightRange.y).</param>
        /// <param name="margin">Yatay/dikey pay (m): cullPadding (rüzgar/ezme eğilmesi) + telafili maks blade genişliği.</param>
        public static Bounds Compute(Vector3 cameraPosition, float drawDistance, float terrainMinY, float terrainMaxY,
                                     float maxBladeHeight, float margin)
        {
            float half = drawDistance + margin;
            float yMin = terrainMinY - margin;
            float yMax = terrainMaxY + maxBladeHeight + margin;
            var center = new Vector3(cameraPosition.x, (yMin + yMax) * 0.5f, cameraPosition.z);
            var size = new Vector3(half * 2f, yMax - yMin, half * 2f);
            return new Bounds(center, size);
        }
    }
}
