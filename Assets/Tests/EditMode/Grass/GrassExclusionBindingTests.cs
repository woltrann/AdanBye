using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    /// <summary>
    /// WP-6d: mask bağlaması (terrain uyuşmazlık uyarısı, null = maskesiz) ve compute örneklemesinin C# eşlemesiyle
    /// (ExclusionMaskCanvas: texel = (x-origin)/size*(res-1), yarım texel kayması yok) uyumu.
    /// Tam dispatch testi (Terrain gerektirir; sahneyi kirletir) yok: "tam maskeli chunk = 0 instance" GPU testinde
    /// mask=1 örneklemesi + compute'taki `density *= 1-mask` ile (rank >= 0 her zaman) kanıtlanır.
    /// </summary>
    public class GrassExclusionBindingTests
    {
        const string ComputePath = "Assets/Shaders/Grass/GrassGenerate.compute";
        static readonly Vector3 TerrainOrigin = new Vector3(-500f, 3f, -500f);
        static readonly Vector3 TerrainSize = new Vector3(1000f, 200f, 1000f);

        static GrassExclusionMask MakeMask(Texture2D tex, Vector2 origin, Vector2 size, int res)
        {
            var mask = ScriptableObject.CreateInstance<GrassExclusionMask>();
            if (tex != null) mask.Apply(tex, origin, size, res, "h");
            return mask;
        }

        static Texture2D MakeTexture(int res, byte[] data)
        {
            var tex = new Texture2D(res, res, TextureFormat.R8, false, true) { filterMode = FilterMode.Point };
            tex.SetPixelData(data, 0);
            tex.Apply(false, false);
            return tex;
        }

        [Test]
        public void FromMask_Null_IsNoneWithoutWarning()
        {
            var report = new ValidationReport();
            GrassExclusionBinding b = GrassExclusionBinding.FromMask(null, TerrainOrigin, TerrainSize, report);
            Assert.IsFalse(b.IsActive);
            Assert.AreEqual(0, report.Warnings.Count);
        }

        [Test]
        public void FromMask_MatchingTerrain_ActiveWithoutWarning()
        {
            var tex = MakeTexture(4, new byte[16]);
            var mask = MakeMask(tex, new Vector2(-500f, -500f), new Vector2(1000f, 1000f), 4);
            try
            {
                var report = new ValidationReport();
                GrassExclusionBinding b = GrassExclusionBinding.FromMask(mask, TerrainOrigin, TerrainSize, report);
                Assert.IsTrue(b.IsActive);
                Assert.AreEqual(new Vector4(-500f, -500f, 1000f, 1000f), b.Rect);
                Assert.AreEqual(4, b.Resolution);
                Assert.AreEqual(0, report.Warnings.Count);
            }
            finally { Object.DestroyImmediate(mask); Object.DestroyImmediate(tex); }
        }

        [Test]
        public void FromMask_TerrainMismatch_WarnsButStillBinds()
        {
            var tex = MakeTexture(4, new byte[16]);
            var mask = MakeMask(tex, new Vector2(-400f, -500f), new Vector2(1000f, 1000f), 4);
            try
            {
                var report = new ValidationReport();
                GrassExclusionBinding b = GrassExclusionBinding.FromMask(mask, TerrainOrigin, TerrainSize, report);
                Assert.IsTrue(b.IsActive);
                Assert.AreEqual(1, report.Warnings.Count);
            }
            finally { Object.DestroyImmediate(mask); Object.DestroyImmediate(tex); }
        }

        [Test]
        public void FromMask_UnbakedOrWrongTextureSize_IsNoneWithWarning()
        {
            var unbaked = MakeMask(null, Vector2.zero, Vector2.one, 0);
            var tex = MakeTexture(4, new byte[16]);
            var wrongRes = MakeMask(tex, new Vector2(-500f, -500f), new Vector2(1000f, 1000f), 8);
            try
            {
                var r1 = new ValidationReport();
                Assert.IsFalse(GrassExclusionBinding.FromMask(unbaked, TerrainOrigin, TerrainSize, r1).IsActive);
                Assert.AreEqual(1, r1.Warnings.Count);

                var r2 = new ValidationReport();
                Assert.IsFalse(GrassExclusionBinding.FromMask(wrongRes, TerrainOrigin, TerrainSize, r2).IsActive);
                Assert.AreEqual(1, r2.Warnings.Count);
            }
            finally { Object.DestroyImmediate(unbaked); Object.DestroyImmediate(wrongRes); Object.DestroyImmediate(tex); }
        }

        [Test]
        public void Dispatcher_Dispose_IsIdempotent()
        {
            if (!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute shader desteklenmiyor.");
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            Assert.IsTrue(GrassComputeDispatcher.TryCreate(cs, out GrassComputeDispatcher d, out string error), error);
            d.SetExclusion(GrassExclusionBinding.None);
            d.Dispose();
            Assert.DoesNotThrow(d.Dispose);
        }

        // Rastgele mask; texel merkezlerinde GPU == data/255 (yarım texel kayması olsaydı komşu texel okunurdu),
        // texel aralarında 4 texel'in bilinear'ı, terrain dışında kenara sıkışma.
        [Test]
        public void GpuSample_MatchesCanvasMapping()
        {
            if (!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute shader desteklenmiyor.");
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            Assert.IsNotNull(cs, ComputePath);

            const int res = 17;
            var canvas = new ExclusionMaskCanvas(new Vector2(-16f, -8f), new Vector2(32f, 64f), res);
            var rng = new System.Random(7);
            for (int i = 0; i < canvas.Data.Length; i++) canvas.Data[i] = (byte)rng.Next(0, 256);

            var points = new System.Collections.Generic.List<Vector2>();
            var expected = new System.Collections.Generic.List<float>();
            for (int tz = 0; tz < res; tz += 3)
                for (int tx = 0; tx < res; tx += 2)
                {
                    points.Add(new Vector2(canvas.TexelToWorldX(tx), canvas.TexelToWorldZ(tz)));
                    expected.Add(canvas.GetTexel(tx, tz) / 255f);
                }
            // Dört texel'in tam ortası (f = 0.5, 0.5).
            for (int tz = 0; tz < res - 1; tz += 4)
                for (int tx = 0; tx < res - 1; tx += 4)
                {
                    points.Add(new Vector2((canvas.TexelToWorldX(tx) + canvas.TexelToWorldX(tx + 1)) * 0.5f,
                                           (canvas.TexelToWorldZ(tz) + canvas.TexelToWorldZ(tz + 1)) * 0.5f));
                    expected.Add((canvas.GetTexel(tx, tz) + canvas.GetTexel(tx + 1, tz) +
                                  canvas.GetTexel(tx, tz + 1) + canvas.GetTexel(tx + 1, tz + 1)) / (4f * 255f));
                }
            // Sınır dışı: sıkıştırılır (köşe texel'i).
            points.Add(new Vector2(-1000f, -1000f)); expected.Add(canvas.GetTexel(0, 0) / 255f);
            points.Add(new Vector2(1000f, 1000f)); expected.Add(canvas.GetTexel(res - 1, res - 1) / 255f);

            Texture2D tex = MakeTexture(res, canvas.Data);
            var inBuf = new ComputeBuffer(points.Count, sizeof(float) * 2);
            var outBuf = new ComputeBuffer(points.Count, sizeof(float));
            try
            {
                inBuf.SetData(points);
                int k = cs.FindKernel("CSExclusionParity");
                cs.SetTexture(k, "_ExclusionMask", tex);
                cs.SetVector("_ExclusionRect", new Vector4(canvas.Origin.x, canvas.Origin.y, canvas.Size.x, canvas.Size.y));
                cs.SetInt("_ExclusionRes", res);
                cs.SetBuffer(k, "_ExclusionIn", inBuf);
                cs.SetBuffer(k, "_ExclusionOut", outBuf);
                cs.SetInt("_ExclusionCount", points.Count);
                cs.Dispatch(k, (points.Count + 63) / 64, 1, 1);
                var gpu = new float[points.Count];
                outBuf.GetData(gpu);
                for (int i = 0; i < gpu.Length; i++)
                    Assert.AreEqual(expected[i], gpu[i], 1.5f / 255f, $"nokta {i} {points[i]}");
            }
            finally
            {
                inBuf.Release();
                outBuf.Release();
                Object.DestroyImmediate(tex);
            }
        }

        // Tam maskeli (255) her yerde 1 => compute `density *= 1 - mask` yoğunluğu 0 yapar => hiçbir rank geçmez => 0 instance.
        [Test]
        public void GpuSample_FullyMasked_IsOneEverywhere()
        {
            if (!SystemInfo.supportsComputeShaders) Assert.Ignore("Compute shader desteklenmiyor.");
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

            const int res = 8;
            var data = new byte[res * res];
            for (int i = 0; i < data.Length; i++) data[i] = 255;
            Texture2D tex = MakeTexture(res, data);
            var points = new[] { new Vector2(0.1f, 0.1f), new Vector2(31.9f, 5f), new Vector2(15.5f, 15.5f) };
            var inBuf = new ComputeBuffer(points.Length, sizeof(float) * 2);
            var outBuf = new ComputeBuffer(points.Length, sizeof(float));
            try
            {
                inBuf.SetData(points);
                int k = cs.FindKernel("CSExclusionParity");
                cs.SetTexture(k, "_ExclusionMask", tex);
                cs.SetVector("_ExclusionRect", new Vector4(0f, 0f, 32f, 32f));
                cs.SetInt("_ExclusionRes", res);
                cs.SetBuffer(k, "_ExclusionIn", inBuf);
                cs.SetBuffer(k, "_ExclusionOut", outBuf);
                cs.SetInt("_ExclusionCount", points.Length);
                cs.Dispatch(k, 1, 1, 1);
                var gpu = new float[points.Length];
                outBuf.GetData(gpu);
                foreach (float v in gpu) Assert.AreEqual(1f, v, 1e-6f);
            }
            finally
            {
                inBuf.Release();
                outBuf.Release();
                Object.DestroyImmediate(tex);
            }
        }
    }
}
