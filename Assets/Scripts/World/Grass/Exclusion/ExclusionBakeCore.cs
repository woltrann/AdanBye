using System;
using System.Collections.Generic;

namespace AdanBye.Grass
{
    /// <summary>Bake çıktısı: dolu canvas + girdi hash'i.</summary>
    public readonly struct ExclusionBakeResult
    {
        public ExclusionMaskCanvas Canvas { get; }
        public string SourceHash { get; }

        public ExclusionBakeResult(ExclusionMaskCanvas canvas, string sourceHash)
        {
            Canvas = canvas;
            SourceHash = sourceHash;
        }
    }

    /// <summary>
    /// Kaynakları toplayıp canvas'ı dolduran saf koordinatör. Neden Editor'dan ayrı: asset/Terrain yazımı
    /// olmadan (fake provider + quad mesh ile) EditMode testte doğrulanabilsin.
    /// </summary>
    public static class ExclusionBakeCore
    {
        public static ExclusionBakeResult Bake(ExclusionBakeInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var canvas = new ExclusionMaskCanvas(input.OriginXZ, input.SizeXZ, input.Resolution);
            foreach (IGrassExclusionSource source in BuildSources(input))
                source.Rasterize(canvas);
            return new ExclusionBakeResult(canvas, ComputeHash(input));
        }

        /// <summary>Bake etmeden hash (stale kontrolü): ağaç/mesh okur, rasterize etmez.</summary>
        public static string ComputeHash(ExclusionBakeInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var h = new ExclusionSourceHasher();
            // Geometri ve parametreler de hash'te: çözünürlük/marj değişince mask değişir, stale olmalı.
            h.AddVector2(input.OriginXZ).AddVector2(input.SizeXZ).AddInt(input.Resolution)
             .AddFloat(input.TreeMargin).AddFloat(input.LakeMargin);

            if (input.Trees != null)
            {
                IReadOnlyList<TreeInstanceInfo> trees = input.Trees.GetInstances();
                h.AddTrees(trees);
                // Kullanılan prototiplerin etkin yarıçapı (override yoksa prefab bounds) sonucu belirler.
                var used = new SortedSet<int>();
                foreach (TreeInstanceInfo t in trees) used.Add(t.PrototypeIndex);
                foreach (int p in used) h.AddInt(p).AddFloat(ResolveRadius(input, p));
            }

            IReadOnlyList<MeshExclusionEntry> lakes = input.Lakes;
            h.AddInt(lakes != null ? lakes.Count : 0);
            if (lakes != null)
                for (int i = 0; i < lakes.Count; i++) h.AddMesh(lakes[i].Mesh, lakes[i].LocalToWorld);
            bool clip = ClipActive(input);
            h.AddInt(clip ? 1 : 0);
            if (clip) AddHeightDigest(h, input);
            return h.ToHex();
        }

        static bool ClipActive(ExclusionBakeInput input) =>
            input.ClipLakeToTerrainHeight && input.TerrainHeights != null;

        const int HeightDigestGrid = 65;

        // Su Y'si mesh matrisiyle zaten hash'te; terrain değişimi için canvas alanında seyrek ızgara yeter (ucuz, sculpt'ı yakalar).
        static void AddHeightDigest(ExclusionSourceHasher h, ExclusionBakeInput input)
        {
            for (int j = 0; j < HeightDigestGrid; j++)
            {
                float z = input.OriginXZ.y + input.SizeXZ.y * j / (HeightDigestGrid - 1);
                for (int i = 0; i < HeightDigestGrid; i++)
                    h.AddFloat(input.TerrainHeights.SampleHeightWorld(
                        input.OriginXZ.x + input.SizeXZ.x * i / (HeightDigestGrid - 1), z));
            }
        }

        static IEnumerable<IGrassExclusionSource> BuildSources(ExclusionBakeInput input)
        {
            if (input.Trees != null)
                yield return new TreeExclusionSource(input.Trees, input.TreeRadiusOverrides, input.TreeMargin);

            if (input.Lakes != null)
                foreach (MeshExclusionEntry lake in input.Lakes)
                    if (lake.Mesh != null)
                        yield return new MeshExclusionSource(lake.Mesh, lake.LocalToWorld, input.LakeMargin,
                            ClipActive(input) ? input.TerrainHeights : null);
        }

        static float ResolveRadius(ExclusionBakeInput input, int prototype) =>
            input.TreeRadiusOverrides != null && input.TreeRadiusOverrides.TryGetValue(prototype, out float r)
                ? r
                : input.Trees.GetPrefabRadius(prototype);
    }
}
