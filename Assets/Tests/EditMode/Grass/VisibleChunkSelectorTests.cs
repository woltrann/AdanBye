using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AdanBye.Grass.Tests
{
    public class VisibleChunkSelectorTests
    {
        const float DrawDistance = 120f;

        sealed class AcceptAll : IFrustum { public bool Intersects(Bounds bounds) => true; }
        sealed class RejectAll : IFrustum { public bool Intersects(Bounds bounds) => false; }
        sealed class MaxXAtLeast : IFrustum
        {
            readonly float _x;
            public MaxXAtLeast(float x) { _x = x; }
            public bool Intersects(Bounds bounds) => bounds.max.x >= _x;
        }

        // Gerçek terrain ölçüleri: 1000x1000, origin -500, 513 örnek, size.y=600, düz zemin.
        static ChunkBoundsTable BuildTable()
        {
            Assert.IsTrue(ChunkGrid.TryCreate(-500f, -500f, 1000f, 1000f, 16f, out ChunkGrid grid, out string e1), e1);
            var heights = new float[513, 513];
            Assert.IsTrue(ChunkBoundsTable.TryCreate(grid, heights, 0f, 600f, out ChunkBoundsTable table, out string e2), e2);
            return table;
        }

        static VisibleChunkSelector BuildSelector(ChunkBoundsTable table, int maxChunks = 4000, Vector3? padding = null)
        {
            Assert.IsTrue(VisibleChunkSelector.TryCreate(table, maxChunks, padding ?? new Vector3(0f, 2f, 0f),
                out VisibleChunkSelector selector, out string error), error);
            return selector;
        }

        static float RectDistSq(ChunkGrid grid, int cx, int cz, float camX, float camZ)
        {
            grid.GetChunkRect(cx, cz, out float minX, out float minZ, out float maxX, out float maxZ);
            float dx = Mathf.Max(minX - camX, camX - maxX, 0f);
            float dz = Mathf.Max(minZ - camZ, camZ - maxZ, 0f);
            return dx * dx + dz * dz;
        }

        [Test]
        public void Ring_MatchesBruteForce()
        {
            ChunkBoundsTable table = BuildTable();
            VisibleChunkSelector selector = BuildSelector(table);
            var cam = new Vector3(0f, 10f, 0f);

            int count = selector.Select(cam, DrawDistance, null);

            int expected = 0;
            for (int cz = 0; cz < table.Grid.CountZ; cz++)
                for (int cx = 0; cx < table.Grid.CountX; cx++)
                    if (RectDistSq(table.Grid, cx, cz, cam.x, cam.z) <= DrawDistance * DrawDistance) expected++;

            Assert.AreEqual(expected, count);
            Assert.IsFalse(selector.Overflowed);

            for (int i = 0; i < count; i++)
            {
                VisibleChunk c = selector[i];
                Assert.AreEqual(table.Grid.ToIndex(c.Cx, c.Cz), c.Index);
                Assert.AreEqual(RectDistSq(table.Grid, c.Cx, c.Cz, cam.x, cam.z), c.DistanceSq, 1e-3f);
            }
            Assert.AreEqual(count, selector.Chunks.Length);
        }

        [Test]
        public void CameraChunk_IsIncluded()
        {
            VisibleChunkSelector selector = BuildSelector(BuildTable());
            selector.Select(new Vector3(3f, 10f, 7f), DrawDistance, null);

            bool found = false;
            for (int i = 0; i < selector.Count; i++)
                if (selector[i].DistanceSq == 0f) found = true;
            Assert.IsTrue(found);
        }

        [Test]
        public void Frustum_RejectAll_ReturnsNothing_AndPartialFrustumFilters()
        {
            ChunkBoundsTable table = BuildTable();
            VisibleChunkSelector selector = BuildSelector(table);
            var cam = new Vector3(0f, 10f, 0f);

            Assert.AreEqual(0, selector.Select(cam, DrawDistance, new RejectAll()));

            int all = selector.Select(cam, DrawDistance, new AcceptAll());
            int half = selector.Select(cam, DrawDistance, new MaxXAtLeast(0f));
            Assert.Greater(half, 0);
            Assert.Less(half, all);
            for (int i = 0; i < half; i++)
            {
                // MaxXAtLeast(0): chunk'ın sağ kenarı 0'ın solunda kalan chunk seçilmemeli.
                table.Grid.GetChunkRect(selector[i].Cx, selector[i].Cz, out _, out _, out float maxX, out _);
                Assert.GreaterOrEqual(maxX, 0f);
            }
        }

        [Test]
        public void Overflow_KeepsNearestChunks_AndFlagsIt()
        {
            ChunkBoundsTable table = BuildTable();
            var cam = new Vector3(3f, 10f, 7f);

            VisibleChunkSelector big = BuildSelector(table);
            int total = big.Select(cam, DrawDistance, null);
            var all = new float[total];
            for (int i = 0; i < total; i++) all[i] = big[i].DistanceSq;
            Array.Sort(all);
            float tenthSmallest = all[9];

            VisibleChunkSelector small = BuildSelector(table, 10);
            int kept = small.Select(cam, DrawDistance, null);

            Assert.AreEqual(10, kept);
            Assert.IsTrue(small.Overflowed);
            bool hasCameraChunk = false;
            for (int i = 0; i < kept; i++)
            {
                Assert.LessOrEqual(small[i].DistanceSq, tenthSmallest);
                hasCameraChunk |= small[i].DistanceSq == 0f;
            }
            Assert.IsTrue(hasCameraChunk, "En yakın chunk (kameranın altındaki) korunmalı");

            // Sonraki çağrı bayrağı sıfırlamalı.
            small.Select(cam, 5f, null);
            Assert.IsFalse(small.Overflowed);
        }

        [Test]
        public void CameraOutsideTerrain_NearEdgeStillSelectsEdgeChunks_FarSelectsNothing()
        {
            VisibleChunkSelector selector = BuildSelector(BuildTable());

            int near = selector.Select(new Vector3(-520f, 10f, 0f), 50f, null); // kenara 20 m
            Assert.Greater(near, 0);
            for (int i = 0; i < near; i++) Assert.LessOrEqual(selector[i].Cx, 2);

            Assert.AreEqual(0, selector.Select(new Vector3(-2000f, 10f, 0f), 50f, null));
        }

        [Test]
        public void InvalidInputs_ReturnZeroWithoutThrowing()
        {
            VisibleChunkSelector selector = BuildSelector(BuildTable());
            Assert.AreEqual(0, selector.Select(new Vector3(float.NaN, 0f, 0f), 50f, null));
            Assert.AreEqual(0, selector.Select(new Vector3(0f, 0f, float.PositiveInfinity), 50f, null));
            Assert.AreEqual(0, selector.Select(Vector3.zero, 0f, null));
            Assert.AreEqual(0, selector.Select(Vector3.zero, -5f, null));
            Assert.AreEqual(0, selector.Select(Vector3.zero, float.NaN, null));
            Assert.AreEqual(0, selector.Select(Vector3.zero, float.PositiveInfinity, null));
            Assert.IsFalse(selector.Overflowed);
        }

        [Test]
        public void TryCreate_InvalidArguments_ReturnFalseWithError()
        {
            ChunkBoundsTable table = BuildTable();
            string error;
            Assert.IsFalse(VisibleChunkSelector.TryCreate(null, 10, Vector3.zero, out _, out error)); Assert.IsNotEmpty(error);
            Assert.IsFalse(VisibleChunkSelector.TryCreate(table, 0, Vector3.zero, out _, out error)); Assert.IsNotEmpty(error);
            Assert.IsFalse(VisibleChunkSelector.TryCreate(table, 10, new Vector3(0f, -1f, 0f), out _, out error)); Assert.IsNotEmpty(error);
            Assert.IsFalse(VisibleChunkSelector.TryCreate(table, 10, new Vector3(float.NaN, 0f, 0f), out _, out error)); Assert.IsNotEmpty(error);
        }

        [Test]
        public void HorizontalPadding_KeepsEdgeChunkThatUnpaddedFrustumRejects()
        {
            ChunkBoundsTable table = BuildTable();
            var cam = new Vector3(0f, 10f, 0f);
            // Origin -500 ve 16 m chunk ile chunk kenarları ..., -20, -4, 12 ... olur (0 bir kenar DEĞİL).
            // Frustum maxX >= 0 ister: sağ kenarı -4 olan chunk dolgusuz elenir, 4 m yatay dolguyla (maxX 0'a çıkar) tutulur.
            const float edgeMaxX = -4f;
            var frustum = new MaxXAtLeast(0f);

            int without = BuildSelector(table, 4000, new Vector3(0f, 2f, 0f)).Select(cam, DrawDistance, frustum);
            VisibleChunkSelector padded = BuildSelector(table, 4000, new Vector3(4f, 2f, 4f));
            int with = padded.Select(cam, DrawDistance, frustum);

            Assert.Greater(with, without);
            bool foundEdge = false;
            for (int i = 0; i < with; i++)
            {
                table.Grid.GetChunkRect(padded[i].Cx, padded[i].Cz, out _, out _, out float maxX, out _);
                if (Mathf.Approximately(maxX, edgeMaxX)) foundEdge = true;
            }
            Assert.IsTrue(foundEdge, "Yatay dolgu kenardaki chunk'ı tutmalı");
        }

        [Test]
        public void PlanesFrustum_CullsChunksBehindCamera()
        {
            ChunkBoundsTable table = BuildTable();
            VisibleChunkSelector selector = BuildSelector(table);
            var frustum = new PlanesFrustum();
            var cam = new Vector3(0f, 20f, 0f); // +Z'ye bakar

            Matrix4x4 proj = Matrix4x4.Perspective(60f, 1.5f, 0.3f, 300f);
            Matrix4x4 view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(cam, Quaternion.identity, Vector3.one).inverse;
            frustum.Update(proj * view);

            // Sıra önemli: seçici çıktısı her Select'te üzerine yazılır; frustum sonucu en son okunmalı.
            int ringOnly = selector.Select(cam, DrawDistance, null);
            int withFrustum = selector.Select(cam, DrawDistance, frustum);

            Assert.Greater(withFrustum, 0);
            Assert.Less(withFrustum, ringOnly);
            for (int i = 0; i < withFrustum; i++)
            {
                table.Grid.GetChunkRect(selector[i].Cx, selector[i].Cz, out _, out _, out _, out float maxZ);
                Assert.GreaterOrEqual(maxZ, 0.3f, "Near plane'in arkasındaki chunk seçilmemeli");
            }
        }

        [Test]
        public void Select_DoesNotAllocateGcMemory()
        {
            ChunkBoundsTable table = BuildTable();
            VisibleChunkSelector selector = BuildSelector(table);
            var planes = new PlanesFrustum();
            var accept = new AcceptAll();
            var cam = new Vector3(10f, 20f, -30f);
            Matrix4x4 viewProj = Matrix4x4.Perspective(60f, 1.5f, 0.3f, 300f) *
                                 Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(cam, Quaternion.identity, Vector3.one).inverse;

            // Isınma: JIT ve ilk-çağrı allocation'ları ölçüme girmesin.
            planes.Update(viewProj);
            selector.Select(cam, DrawDistance, planes);
            selector.Select(cam, DrawDistance, accept);

            Assert.That(() => { selector.Select(cam, DrawDistance, accept); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { planes.Update(viewProj); selector.Select(cam, DrawDistance, planes); }, Is.Not.AllocatingGCMemory());

            // Taşma yolu (yer değiştirme) de allocation yapmamalı.
            VisibleChunkSelector small = BuildSelector(table, 8);
            small.Select(cam, DrawDistance, accept);
            Assert.That(() => { small.Select(cam, DrawDistance, accept); }, Is.Not.AllocatingGCMemory());
        }
    }
}
