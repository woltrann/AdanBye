using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class ExclusionBakeTests
    {
        class FakeTrees : ITreeInstanceProvider
        {
            public List<TreeInstanceInfo> Trees = new List<TreeInstanceInfo>();
            public float PrefabRadius = 1f;
            public IReadOnlyList<TreeInstanceInfo> GetInstances() => Trees;
            public float GetPrefabRadius(int prototypeIndex) => PrefabRadius;
        }

        // x < SplitX ise Low, değilse High yükseklik (yarı su altı / yarı kara).
        class FakeHeights : ITerrainHeightProvider
        {
            public float SplitX = 0f, Low = -1f, High = 5f;
            public float SampleHeightWorld(float x, float z) => x < SplitX ? Low : High;
        }

        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        // XZ düzleminde [-half, half] karesi (2 üçgen).
        Mesh Quad(float half)
        {
            var m = new Mesh();
            m.vertices = new[]
            {
                new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half), new Vector3(-half, 0f, half),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _created.Add(m);
            return m;
        }

        ExclusionBakeInput Input(FakeTrees trees, params MeshExclusionEntry[] lakes) => new ExclusionBakeInput
        {
            OriginXZ = new Vector2(-500f, -500f),
            SizeXZ = new Vector2(1000f, 1000f),
            Resolution = 101, // texel boyu 10 m
            Trees = trees,
            TreeMargin = 0f,
            Lakes = lakes,
            LakeMargin = 0f,
        };

        // --- Hasher ---

        [Test]
        public void Hasher_SameInput_SameHash()
        {
            Mesh q = Quad(5f);
            string a = new ExclusionSourceHasher().AddMesh(q, Matrix4x4.identity).AddInt(3).ToHex();
            string b = new ExclusionSourceHasher().AddMesh(q, Matrix4x4.identity).AddInt(3).ToHex();
            Assert.AreEqual(a, b);
        }

        [Test]
        public void Hasher_MeshTransformOrVertexChange_ChangesHash()
        {
            string baseHash = new ExclusionSourceHasher().AddMesh(Quad(5f), Matrix4x4.identity).ToHex();
            string moved = new ExclusionSourceHasher().AddMesh(Quad(5f), Matrix4x4.Translate(Vector3.right)).ToHex();
            string bigger = new ExclusionSourceHasher().AddMesh(Quad(6f), Matrix4x4.identity).ToHex();
            Assert.AreNotEqual(baseHash, moved);
            Assert.AreNotEqual(baseHash, bigger);
        }

        [Test]
        public void Hasher_TreeMoveOrCountChange_ChangesHash()
        {
            var one = new List<TreeInstanceInfo> { new TreeInstanceInfo(new Vector2(1f, 2f), 0, 1f) };
            var moved = new List<TreeInstanceInfo> { new TreeInstanceInfo(new Vector2(1f, 2.5f), 0, 1f) };
            var two = new List<TreeInstanceInfo>(one) { new TreeInstanceInfo(new Vector2(3f, 3f), 0, 1f) };
            string h = new ExclusionSourceHasher().AddTrees(one).ToHex();
            Assert.AreEqual(h, new ExclusionSourceHasher().AddTrees(one).ToHex());
            Assert.AreNotEqual(h, new ExclusionSourceHasher().AddTrees(moved).ToHex());
            Assert.AreNotEqual(h, new ExclusionSourceHasher().AddTrees(two).ToHex());
        }

        [Test]
        public void Hasher_NegativeZeroEqualsZero()
        {
            Assert.AreEqual(new ExclusionSourceHasher().AddFloat(0f).ToHex(),
                new ExclusionSourceHasher().AddFloat(-0f).ToHex());
        }

        // --- Çekirdek ---

        [Test]
        public void Core_TreeAndLake_FillExpectedTexels()
        {
            var trees = new FakeTrees { PrefabRadius = 15f };
            trees.Trees.Add(new TreeInstanceInfo(new Vector2(200f, 200f), 0, 1f));
            var lake = new MeshExclusionEntry(Quad(50f), Matrix4x4.identity);

            ExclusionBakeResult r = ExclusionBakeCore.Bake(Input(trees, lake));

            Assert.AreEqual(255, r.Canvas.SampleWorld(0f, 0f), "göl ortası");
            Assert.AreEqual(255, r.Canvas.SampleWorld(200f, 200f), "ağaç merkezi");
            Assert.AreEqual(0, r.Canvas.SampleWorld(-300f, 300f), "boş alan");
            Assert.AreEqual(0, r.Canvas.SampleWorld(0f, 100f), "göl dışı");
        }

        [Test]
        public void Core_RadiusOverride_ReplacesPrefabRadius()
        {
            var trees = new FakeTrees { PrefabRadius = 1f };
            trees.Trees.Add(new TreeInstanceInfo(new Vector2(0f, 0f), 0, 1f));
            ExclusionBakeInput input = Input(trees);
            input.TreeRadiusOverrides = new Dictionary<int, float> { { 0, 40f } };

            ExclusionBakeResult r = ExclusionBakeCore.Bake(input);

            Assert.AreEqual(255, r.Canvas.SampleWorld(30f, 0f)); // prefab yarıçapı (1 m) olsaydı 0 olurdu
        }

        [Test]
        public void Core_HashChangesWithSources_StableOtherwise()
        {
            var trees = new FakeTrees();
            trees.Trees.Add(new TreeInstanceInfo(new Vector2(10f, 10f), 0, 1f));
            var lake = new MeshExclusionEntry(Quad(50f), Matrix4x4.identity);

            string h1 = ExclusionBakeCore.Bake(Input(trees, lake)).SourceHash;
            Assert.AreEqual(h1, ExclusionBakeCore.ComputeHash(Input(trees, lake)));

            trees.Trees.Add(new TreeInstanceInfo(new Vector2(20f, 20f), 0, 1f));
            Assert.AreNotEqual(h1, ExclusionBakeCore.ComputeHash(Input(trees, lake)));
        }

        [Test]
        public void Core_NoSources_EmptyMask()
        {
            ExclusionBakeResult r = ExclusionBakeCore.Bake(Input(null));
            foreach (byte b in r.Canvas.Data) Assert.AreEqual(0, b);
        }

        // --- SO ---

        [Test]
        public void MaskAsset_Apply_SetsFieldsAndStaleLogic()
        {
            var mask = ScriptableObject.CreateInstance<GrassExclusionMask>();
            _created.Add(mask);
            var tex = new Texture2D(2, 2, TextureFormat.R8, false, true);
            _created.Add(tex);

            Assert.IsTrue(mask.IsStale("x"), "texture yokken eski");
            mask.Apply(tex, new Vector2(1f, 2f), new Vector2(3f, 4f), 2, "abc");
            Assert.AreEqual(2, mask.Resolution);
            Assert.AreEqual(new Vector2(3f, 4f), mask.SizeXZ);
            Assert.IsFalse(mask.IsStale("abc"));
            Assert.IsTrue(mask.IsStale("abd"));
        }

        // --- Yükseklik filtresi ---

        [Test]
        public void LakeClip_MasksOnlyWhereTerrainBelowWater()
        {
            var input = Input(null, new MeshExclusionEntry(Quad(200f), Matrix4x4.identity)); // su Y = 0
            input.ClipLakeToTerrainHeight = true;
            input.TerrainHeights = new FakeHeights();
            ExclusionMaskCanvas c = ExclusionBakeCore.Bake(input).Canvas;

            Assert.AreEqual(255, c.SampleWorld(-100f, 0f), "terrain su altında -> maskeli");
            Assert.AreEqual(0, c.SampleWorld(100f, 0f), "terrain su üstünde -> maskesiz");
        }

        [Test]
        public void LakeClip_Disabled_KeepsFullFootprint()
        {
            var input = Input(null, new MeshExclusionEntry(Quad(200f), Matrix4x4.identity));
            input.ClipLakeToTerrainHeight = false;
            input.TerrainHeights = new FakeHeights();
            ExclusionMaskCanvas c = ExclusionBakeCore.Bake(input).Canvas;

            Assert.AreEqual(255, c.SampleWorld(-100f, 0f));
            Assert.AreEqual(255, c.SampleWorld(100f, 0f));
        }

        [Test]
        public void LakeClip_Hash_ChangesWithTerrainHeightAndToggle()
        {
            var lake = new MeshExclusionEntry(Quad(200f), Matrix4x4.identity);
            var heights = new FakeHeights();
            var input = Input(null, lake);
            input.TerrainHeights = heights;

            input.ClipLakeToTerrainHeight = false;
            string off = ExclusionBakeCore.ComputeHash(input);
            input.ClipLakeToTerrainHeight = true;
            string on = ExclusionBakeCore.ComputeHash(input);
            heights.High = 6f;
            string changed = ExclusionBakeCore.ComputeHash(input);

            Assert.AreNotEqual(off, on);
            Assert.AreNotEqual(on, changed);
        }
    }
}
