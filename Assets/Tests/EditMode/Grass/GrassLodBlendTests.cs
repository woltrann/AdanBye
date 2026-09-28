using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>
    /// LOD seyreltme + geçiş bandı mantığının CPU aynası (LodDistanceTable.Blend). Compute'taki Grass_LodBlend bununla
    /// aynı formülü uygular; GPU sonuçları Editor doğrulamasında ayrıca ölçülür.
    /// </summary>
    public class GrassLodBlendTests
    {
        static LodDistanceTable DefaultTable()
        {
            Assert.IsTrue(LodDistanceTable.TryCreate(GrassLodDefaults.Create(), out LodDistanceTable table, out ValidationReport report), report.ToString());
            return table;
        }

        [Test]
        public void Defaults_AreValid_AndWidthCompensationIsCapped()
        {
            LodDistanceTable t = DefaultTable();
            Assert.AreEqual(3, t.Count);
            Assert.AreEqual(1f, t.WidthCompensation[0], 1e-6f);
            Assert.AreEqual(1.7f, t.WidthCompensation[1], 1e-6f); // 1/0.35 = 2.86 -> cap 1.7
            Assert.AreEqual(3f, t.WidthCompensation[2], 1e-6f);   // 1/0.1 = 10 -> cap 3
            Assert.AreEqual(120f, t.DrawDistance, 1e-6f);
        }

        [Test]
        public void Blend_BeyondDrawDistance_IsCulled()
        {
            LodBlendResult r = DefaultTable().Blend(120.5f, 0f);
            Assert.AreEqual(-1, r.Lod);
            Assert.IsFalse(r.IsVisible);
        }

        [Test]
        public void Blend_ThinningIsNested_SurvivorAtFarLodExistsAtEveryNearerLod()
        {
            // Aynı rank oranı için her mesafede: uzak LOD'da tutulan blade, daha yakın mesafede (daha küçük LOD) de tutulur.
            LodDistanceTable t = DefaultTable();
            for (float r = 0f; r < 1f; r += 0.01f)
            {
                bool wasKeptFar = false;
                for (float d = 119.9f; d >= 0.1f; d -= 0.1f)
                {
                    LodBlendResult b = t.Blend(d, r);
                    if (b.Lod < 0) continue;
                    if (b.Kept && b.HeightScale > 0f) wasKeptFar = true;
                    // Bir kez (uzakta) görünen blade yaklaşırken bir daha kaybolmamalı.
                    if (wasKeptFar) Assert.IsTrue(b.Kept, $"rank {r:0.00} d {d:0.0}: uzakta görünen blade yakında kayboldu.");
                }
            }
        }

        [Test]
        public void Blend_KeepRatioMatchesLodFraction()
        {
            LodDistanceTable t = DefaultTable();
            AssertKeptFraction(t, 10f, 1.0f);
            AssertKeptFraction(t, 40f, 0.35f);
            AssertKeptFraction(t, 90f, 0.10f);
        }

        static void AssertKeptFraction(LodDistanceTable t, float distance, float expected)
        {
            int kept = 0;
            const int n = 10000;
            for (int i = 0; i < n; i++) if (t.Blend(distance, (i + 0.5f) / n).Kept) kept++;
            Assert.AreEqual(expected, kept / (float)n, 0.001f, $"d={distance}");
        }

        [Test]
        public void Blend_SurvivorWidth_IsContinuousAcrossLodBorders()
        {
            // Sınırı geçince var kalan blade'in genişliği/boyu sıçramamalı (alt küme + telafi bandı).
            LodDistanceTable t = DefaultTable();
            const float eps = 0.001f;
            AssertContinuous(t, 25f, 0.05f, eps); // LOD0->1 ve LOD1->2'de de hayatta kalan
            AssertContinuous(t, 60f, 0.05f, eps);
            AssertContinuous(t, 25f, 0.2f, eps);  // LOD1'e kadar hayatta kalır (rank 0.2 < 0.35), LOD2'de yok
        }

        static void AssertContinuous(LodDistanceTable t, float border, float rank, float eps)
        {
            LodBlendResult a = t.Blend(border - eps, rank);
            LodBlendResult b = t.Blend(border + eps, rank);
            Assert.IsTrue(a.Kept && b.Kept, $"border {border} rank {rank}: sınırın iki yanında da tutulmalı");
            Assert.AreEqual(a.WidthScale, b.WidthScale, 0.01f, $"width border {border} rank {rank}");
            Assert.AreEqual(a.HeightScale, b.HeightScale, 0.01f, $"height border {border} rank {rank}");
        }

        [Test]
        public void Blend_NonSurvivor_ShrinksToZeroBeforeBorder()
        {
            LodDistanceTable t = DefaultTable();
            // rank 0.5: LOD1'de tutulmaz (keep 0.35) => LOD0 sonunda boyu 0'a inmeli.
            Assert.AreEqual(1f, t.Blend(10f, 0.5f).HeightScale, 1e-6f);
            Assert.Less(t.Blend(24.9f, 0.5f).HeightScale, 0.05f);
            Assert.IsFalse(t.Blend(25.1f, 0.5f).Kept);
        }

        [Test]
        public void Blend_LastLod_FadesEverythingOutAtDrawDistance()
        {
            LodDistanceTable t = DefaultTable();
            Assert.AreEqual(1f, t.Blend(100f, 0.05f).HeightScale, 1e-6f);
            Assert.Less(t.Blend(119.9f, 0.05f).HeightScale, 0.05f);
        }

        [Test]
        public void Blend_ZeroBand_DoesNotFade()
        {
            var specs = new[] { new LodLevelSpec(50f, 1f, 1f, 0f) };
            Assert.IsTrue(LodDistanceTable.TryCreate(specs, out LodDistanceTable t, out _));
            Assert.AreEqual(1f, t.Blend(49.9f, 0.5f).HeightScale, 1e-6f);
        }

        [Test]
        public void PlanesFrustum_CopyPlanes_MatchesInsideOutsideSemantics()
        {
            var f = new PlanesFrustum();
            Matrix4x4 view = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one).inverse; // kamera orijinde, +Z'ye bakar (Unity view -Z ekseni için ölçek)
            view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * view;
            Matrix4x4 proj = Matrix4x4.Perspective(60f, 1f, 0.1f, 100f);
            f.Update(proj * view);
            var planes = new Vector4[GrassViewParams.PlaneCount];
            f.CopyPlanes(planes);

            Assert.IsTrue(Inside(planes, new Vector3(0, 0, 10)), "ön nokta içeride olmalı");
            Assert.IsFalse(Inside(planes, new Vector3(0, 0, -10)), "arka nokta dışarıda olmalı");
            Assert.IsFalse(Inside(planes, new Vector3(0, 0, 200)), "far ötesi dışarıda olmalı");
            Assert.Throws<System.ArgumentException>(() => f.CopyPlanes(new Vector4[3]));
        }

        static bool Inside(Vector4[] planes, Vector3 p)
        {
            foreach (Vector4 pl in planes)
                if (pl.x * p.x + pl.y * p.y + pl.z * p.z + pl.w < 0f) return false;
            return true;
        }

        [Test]
        public void ViewParams_Validate_RejectsBadInput()
        {
            Assert.IsTrue(new GrassViewParams(Vector3.zero, null, 0.1f).Validate(out _));
            Assert.IsFalse(new GrassViewParams(new Vector3(float.NaN, 0, 0), null, 0.1f).Validate(out _));
            Assert.IsFalse(new GrassViewParams(Vector3.zero, new Vector4[5], 0.1f).Validate(out _));
            Assert.IsFalse(new GrassViewParams(Vector3.zero, null, -1f).Validate(out _));
        }

        [Test]
        public void GpuResources_TryCreate_RejectsInvalidCapacities()
        {
            Assert.IsFalse(GrassGpuResources.TryCreate(null, 8, out _, out string e1), e1);
            Assert.IsFalse(GrassGpuResources.TryCreate(new int[0], 8, out _, out string e2), e2);
            Assert.IsFalse(GrassGpuResources.TryCreate(new[] { 10, 10, 10, 10 }, 8, out _, out string e3), e3);
            Assert.IsFalse(GrassGpuResources.TryCreate(new[] { 0 }, 8, out _, out string e4), e4);
            Assert.IsFalse(GrassGpuResources.TryCreate(new[] { GrassGpuResources.MaxInstanceCapacity + 1 }, 8, out _, out string e5), e5);
            Assert.IsFalse(GrassGpuResources.TryCreate(new[] { 10 }, 0, out _, out string e6), e6);
        }
    }
}
