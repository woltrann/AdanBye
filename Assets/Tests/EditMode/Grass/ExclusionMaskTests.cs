using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class ExclusionMaskTests
    {
        // Gerçek terrain düzeni, testte hızlı olsun diye varsayılan 101 texel -> texel boyu 10 m.
        static ExclusionMaskCanvas Create(int res = 101) =>
            new ExclusionMaskCanvas(new Vector2(-500f, -500f), new Vector2(1000f, 1000f), res);

        static int Count(ExclusionMaskCanvas c)
        {
            int n = 0;
            foreach (byte b in c.Data) if (b > 0) n++;
            return n;
        }

        class FakeTrees : ITreeInstanceProvider
        {
            public readonly List<TreeInstanceInfo> Items = new List<TreeInstanceInfo>();
            public float PrefabRadius = 1f;
            public IReadOnlyList<TreeInstanceInfo> GetInstances() => Items;
            public float GetPrefabRadius(int i) => PrefabRadius;
        }

        [Test]
        public void Mapping_Endpoints()
        {
            var c = Create(2048);
            Assert.AreEqual(0f, c.WorldToTexelX(-500f), 1e-4f);
            Assert.AreEqual(2047f, c.WorldToTexelX(500f), 1e-3f);
            Assert.AreEqual(-500f, c.TexelToWorldZ(0), 1e-4f);
            Assert.AreEqual(500f, c.TexelToWorldZ(2047), 1e-3f);
        }

        [Test]
        public void StampDisk_InsideFull_OutsideZero()
        {
            var c = Create();
            c.StampDisk(Vector2.zero, 30f, 0f);
            Assert.AreEqual(255, c.SampleWorld(0f, 0f));
            Assert.AreEqual(255, c.SampleWorld(20f, 0f));
            Assert.AreEqual(0, c.SampleWorld(50f, 0f));
        }

        [Test]
        public void StampDisk_Falloff_IsMonotonic()
        {
            var c = Create(1001); // 1 m texel
            c.StampDisk(Vector2.zero, 20f, 30f);
            byte prev = 255;
            for (int x = 0; x <= 60; x++)
            {
                byte v = c.SampleWorld(x, 0f);
                Assert.LessOrEqual(v, prev, "x=" + x);
                prev = v;
            }
            Assert.AreEqual(255, c.SampleWorld(20f, 0f));
            Assert.Greater(c.SampleWorld(35f, 0f), 0);
            Assert.AreEqual(0, c.SampleWorld(55f, 0f));
        }

        [Test]
        public void StampDisk_Overlap_KeepsMax()
        {
            var c = Create(1001);
            c.StampDisk(Vector2.zero, 10f, 0f);
            c.StampDisk(Vector2.zero, 30f, 20f); // dış falloff bölgesi iç 255'i silmemeli
            Assert.AreEqual(255, c.SampleWorld(0f, 0f));
            c.StampDisk(new Vector2(100f, 0f), 5f, 0f);
            c.StampDisk(new Vector2(100f, 0f), 2f, 10f);
            Assert.AreEqual(255, c.SampleWorld(100f, 0f));
        }

        [Test]
        public void StampDisk_OutsideTerrain_ClipsWithoutException()
        {
            var c = Create();
            Assert.DoesNotThrow(() => c.StampDisk(new Vector2(-500f, -500f), 100f, 10f));
            Assert.DoesNotThrow(() => c.StampDisk(new Vector2(5000f, 5000f), 100f, 10f));
            Assert.DoesNotThrow(() => c.StampDisk(new Vector2(0f, 0f), 1e12f, 0f));
            Assert.AreEqual(255, c.SampleWorld(-500f, -500f));
        }

        [Test]
        public void FillTriangle_InsideAndOutside_BothWindings()
        {
            var a = new Vector2(-100f, -100f);
            var b = new Vector2(100f, -100f);
            var t = new Vector2(0f, 100f);
            foreach (bool flip in new[] { false, true })
            {
                var c = Create();
                if (flip) c.FillTriangleXZ(a, t, b); else c.FillTriangleXZ(a, b, t);
                Assert.AreEqual(255, c.SampleWorld(0f, 0f));
                Assert.AreEqual(0, c.SampleWorld(0f, 200f));
                Assert.AreEqual(0, c.SampleWorld(-90f, 90f));
            }
        }

        [Test]
        public void Dilate_GrowsByOneTexel()
        {
            var c = Create(); // texel 10 m
            c.StampDisk(Vector2.zero, 1f, 0f);
            Assert.AreEqual(1, Count(c));
            c.Dilate(10f);
            Assert.AreEqual(255, c.SampleWorld(10f, 0f));
            Assert.AreEqual(255, c.SampleWorld(0f, -10f));
            Assert.AreEqual(0, c.SampleWorld(20f, 0f));
            Assert.AreEqual(0, c.SampleWorld(10f, 10f)); // çapraz mesafe 14.1 > 10
        }

        [Test]
        public void EmptySources_ProduceZeroMask()
        {
            var c = Create();
            new TreeExclusionSource(new FakeTrees()).Rasterize(c);
            Assert.AreEqual(0, Count(c));
        }

        [Test]
        public void TreeSource_UsesOverrideScaleAndMargin()
        {
            var c = Create(1001);
            var trees = new FakeTrees { PrefabRadius = 100f };
            trees.Items.Add(new TreeInstanceInfo(new Vector2(0f, 0f), 0, 2f));   // override 3*2 + 1.5 = 7.5
            trees.Items.Add(new TreeInstanceInfo(new Vector2(200f, 0f), 1, 1f)); // prefab 100 + 1.5
            var overrides = new Dictionary<int, float> { { 0, 3f } };
            new TreeExclusionSource(trees, overrides, 1.5f).Rasterize(c);
            Assert.AreEqual(255, c.SampleWorld(7f, 0f));
            Assert.AreEqual(0, c.SampleWorld(9f, 0f));
            Assert.AreEqual(255, c.SampleWorld(300f, 0f));
        }

        [Test]
        public void TreeSource_NormalizedToWorldConversion()
        {
            // TerrainTreeProvider formülü: dünya = normalize * size + origin.
            var world = new Vector2(0.75f * 1000f - 500f, 0.25f * 1000f - 500f);
            Assert.AreEqual(new Vector2(250f, -250f), world);
            var c = Create(1001);
            var trees = new FakeTrees();
            trees.Items.Add(new TreeInstanceInfo(world, 0, 1f));
            new TreeExclusionSource(trees, null, 0f).Rasterize(c);
            Assert.AreEqual(255, c.SampleWorld(250f, -250f));
            Assert.AreEqual(0, c.SampleWorld(0f, 0f));
        }

        [Test]
        public void MeshSource_Quad_FillsFootprintPlusMargin()
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            var c = Create(1001);
            Matrix4x4 m = Matrix4x4.TRS(new Vector3(100f, 5f, 50f), Quaternion.identity, new Vector3(20f, 1f, 20f));
            new MeshExclusionSource(mesh, m, 5f).Rasterize(c);
            Assert.AreEqual(255, c.SampleWorld(100f, 50f));
            Assert.AreEqual(255, c.SampleWorld(122f, 50f)); // kenar 120 + margin 5
            Assert.AreEqual(0, c.SampleWorld(130f, 50f));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MeshSource_SceneLake_BuiltInPlane_Scale18()
        {
            // TerrainTest12 "Lake": pos (-51.7, 15.3, 191.6), scale (18,1,18); child "water" = built-in Plane (10x10).
            // "Plane.fbx" kaynağı 1x1 mesh verir; sahnedeki water ise Plane primitive'inin 10x10 mesh'ini kullanır.
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            try
            {
                Mesh plane = primitive.GetComponent<MeshFilter>().sharedMesh;
                // Testin varsayımı: 10x10 birim, merkezi orijinde. Tutmazsa aşağıdaki köşe assert'leri yanıltıcı olur.
                Assert.AreEqual(10f, plane.bounds.size.x, 1e-3f, $"Plane bounds: {plane.bounds}");
                Assert.AreEqual(10f, plane.bounds.size.z, 1e-3f, $"Plane bounds: {plane.bounds}");
                Assert.AreEqual(0f, plane.bounds.center.x, 1e-3f, $"Plane bounds: {plane.bounds}");
                Assert.AreEqual(0f, plane.bounds.center.z, 1e-3f, $"Plane bounds: {plane.bounds}");
                var c = Create(1001);
                Matrix4x4 m = Matrix4x4.TRS(new Vector3(-51.7f, 15.3f, 191.6f), Quaternion.identity, new Vector3(18f, 1f, 18f));
                new MeshExclusionSource(plane, m, 0f).Rasterize(c);
                Assert.AreEqual(255, c.SampleWorld(-51.7f, 191.6f));
                Assert.AreEqual(255, c.SampleWorld(-141f, 102f)); // köşe içi: x -141.7, z 101.6
                Assert.AreEqual(255, c.SampleWorld(38f, 281f));   // x 38.3, z 281.6
                Assert.AreEqual(0, c.SampleWorld(-145f, 191.6f));
                Assert.AreEqual(0, c.SampleWorld(-51.7f, 285f));
            }
            finally
            {
                Object.DestroyImmediate(primitive);
            }
        }
    }
}
