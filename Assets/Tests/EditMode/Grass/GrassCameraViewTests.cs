using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>GrassCameraView düzlemleri ve GrassDrawBounds sınırları (matris girdisiyle; kamera/pipeline gerekmez).</summary>
    public class GrassCameraViewTests
    {
        // Unity kamerası -Z'ye bakar: worldToCamera = Scale(1,1,-1) * inverse(camera TRS). Birim rotasyon = +Z'ye bakış.
        static Matrix4x4 ViewProjection(Vector3 position) => ViewProjection(position, Matrix4x4.Perspective(60f, 1f, 0.1f, 100f));

        static Matrix4x4 ViewProjection(Vector3 position, Matrix4x4 proj)
        {
            Matrix4x4 view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one).inverse;
            return proj * view;
        }

        static bool InsideAll(Vector4[] planes, Vector3 p)
        {
            foreach (Vector4 pl in planes)
                if (pl.x * p.x + pl.y * p.y + pl.z * p.z + pl.w < 0f) return false;
            return true;
        }

        [Test]
        public void Update_ReturnsSixPlanes_WithCameraPositionAndPadding()
        {
            var view = new GrassCameraView();
            GrassViewParams p = view.Update(ViewProjection(Vector3.zero), new Vector3(1f, 2f, 3f), 0.3f);

            Assert.AreEqual(GrassViewParams.PlaneCount, p.FrustumPlanes.Length);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), p.CameraPosition);
            Assert.AreEqual(0.3f, p.CullPadding);
            Assert.IsTrue(p.Validate(out string error), error);
        }

        [Test]
        public void Planes_PointInward_ContainPointInFront_RejectBehindAndBeyondFar()
        {
            var view = new GrassCameraView();
            Vector4[] planes = view.Update(ViewProjection(Vector3.zero), Vector3.zero, 0f).FrustumPlanes;

            Assert.IsTrue(InsideAll(planes, new Vector3(0f, 0f, 10f)), "önündeki nokta içeride olmalı");
            Assert.IsFalse(InsideAll(planes, new Vector3(0f, 0f, -10f)), "arkasındaki nokta dışarıda olmalı");
            Assert.IsFalse(InsideAll(planes, new Vector3(0f, 0f, 500f)), "far ötesi dışarıda olmalı");
            Assert.IsFalse(InsideAll(planes, new Vector3(500f, 0f, 10f)), "yan taraf dışarıda olmalı");
        }

        [Test]
        public void Frustum_Intersects_MatchesPlanes()
        {
            var view = new GrassCameraView();
            view.Update(ViewProjection(Vector3.zero), Vector3.zero, 0f);

            Assert.IsTrue(view.Frustum.Intersects(new Bounds(new Vector3(0f, 0f, 10f), Vector3.one)));
            Assert.IsFalse(view.Frustum.Intersects(new Bounds(new Vector3(0f, 0f, -10f), Vector3.one)));
        }

        [Test]
        public void Update_ReusesPlaneArray_AndTracksCameraMovement()
        {
            var view = new GrassCameraView();
            Vector4[] first = view.Update(ViewProjection(Vector3.zero), Vector3.zero, 0f).FrustumPlanes;
            Assert.IsFalse(InsideAll(first, new Vector3(0f, 0f, 150f)));

            Vector4[] second = view.Update(ViewProjection(new Vector3(0f, 0f, 100f)), new Vector3(0f, 0f, 100f), 0f).FrustumPlanes;

            Assert.AreSame(first, second, "dizi yeniden ayrılmamalı (GC yok)");
            Assert.IsTrue(InsideAll(second, new Vector3(0f, 0f, 150f)), "kamera ilerleyince aynı nokta görünür olmalı");
        }

        [Test]
        public void Orthographic_Planes_ContainBoxInFront_RejectSidesBehindAndBeyondFar()
        {
            // Yarı genişlik/yükseklik 10, near 0.1, far 100. Ortografikte yan düzlemler paralel: mesafe uzaklaşınca genişlemez.
            Matrix4x4 ortho = Matrix4x4.Ortho(-10f, 10f, -10f, 10f, 0.1f, 100f);
            var view = new GrassCameraView();
            Vector4[] planes = view.Update(ViewProjection(Vector3.zero, ortho), Vector3.zero, 0f).FrustumPlanes;

            Assert.IsTrue(InsideAll(planes, new Vector3(9f, 9f, 50f)), "kutu içindeki nokta içeride olmalı");
            Assert.IsTrue(InsideAll(planes, new Vector3(-9f, -9f, 99f)), "far'a yakın köşe içeride olmalı");
            Assert.IsFalse(InsideAll(planes, new Vector3(11f, 0f, 5f)), "yan dışı, yakında");
            Assert.IsFalse(InsideAll(planes, new Vector3(11f, 0f, 90f)), "perspektifin aksine uzakta da yan dışı (paralel düzlem)");
            Assert.IsFalse(InsideAll(planes, new Vector3(0f, 0f, -10f)), "arkası dışarıda");
            Assert.IsFalse(InsideAll(planes, new Vector3(0f, 0f, 150f)), "far ötesi dışarıda");
            Assert.IsTrue(view.Frustum.Intersects(new Bounds(new Vector3(0f, 0f, 20f), Vector3.one)));
            Assert.IsFalse(view.Frustum.Intersects(new Bounds(new Vector3(30f, 0f, 20f), Vector3.one)));
        }

        // Bulgu: far plane < drawDistance için ne GrassSettings ne GrassDrawSystem uyarı üretir (settings kamerayı bilmez);
        // chunk seçici frustum'u kullandığından far ötesi chunk'lar SESSİZCE elenir. Bu test davranışı sabitler.
        [Test]
        public void FarPlaneShorterThanDrawDistance_CullsChunksBeyondFar_Silently()
        {
            const float drawDistance = 120f;
            Matrix4x4 nearFar = Matrix4x4.Perspective(60f, 1f, 0.1f, 50f);
            var view = new GrassCameraView();
            view.Update(ViewProjection(Vector3.zero, nearFar), Vector3.zero, 0f);

            var withinFar = new Bounds(new Vector3(0f, 0f, 40f), Vector3.one);
            var beyondFarWithinDrawDistance = new Bounds(new Vector3(0f, 0f, 80f), Vector3.one);
            Assert.Less(80f, drawDistance);
            Assert.IsTrue(view.Frustum.Intersects(withinFar));
            Assert.IsFalse(view.Frustum.Intersects(beyondFarWithinDrawDistance),
                "drawDistance içinde ama far plane ötesindeki chunk çizilmez");
        }

        [Test]
        public void DrawBounds_CoversRingTerrainRangeBladeHeightAndMargin()
        {
            Bounds b = GrassDrawBounds.Compute(new Vector3(10f, 999f, -20f), 120f, 5f, 55f, 0.8f, 0.5f);

            Assert.AreEqual(10f, b.center.x, 1e-4f);
            Assert.AreEqual(-20f, b.center.z, 1e-4f);
            Assert.AreEqual(2f * (120f + 0.5f), b.size.x, 1e-3f);
            Assert.AreEqual(2f * (120f + 0.5f), b.size.z, 1e-3f);
            Assert.AreEqual(5f - 0.5f, b.min.y, 1e-4f, "alt sınır: terrainMinY - margin (kamera Y'sinden bağımsız)");
            Assert.AreEqual(55f + 0.8f + 0.5f, b.max.y, 1e-4f, "üst sınır: terrainMaxY + blade boyu + margin");
        }

        [Test]
        public void DrawBounds_ContainsEveryPointGeneratableAroundCamera()
        {
            var cam = new Vector3(0f, 30f, 0f);
            Bounds b = GrassDrawBounds.Compute(cam, 100f, 0f, 60f, 1f, 0.3f);

            Assert.IsTrue(b.Contains(new Vector3(99f, 0f, 0f)));
            Assert.IsTrue(b.Contains(new Vector3(0f, 60.9f, -99f)));
            Assert.IsFalse(b.Contains(new Vector3(101f, 30f, 0f)));
        }
    }
}
