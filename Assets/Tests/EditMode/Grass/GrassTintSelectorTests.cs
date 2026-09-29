using NUnit.Framework;
using UnityEngine;

namespace AdanBye.Grass.Tests
{
    public class GrassTintSelectorTests
    {
        const int Samples = 10000;
        const uint Seed = 12345u;

        static readonly Vector4[] Tints =
        {
            new Vector4(0.2f, 0.6f, 0.1f, 0.4f),
            new Vector4(0.7f, 0.6f, 0.3f, 0.5f),
            new Vector4(0.9f, 0.9f, 0.9f, 1f),
            new Vector4(0.4f, 0.3f, 0.2f, 0.45f),
        };

        static uint HashAt(int i) => GrassTintSelector.HashCell(i % 251, i / 251, Seed);

        static int[] CountLayers(Vector4 splat, Vector4 density)
        {
            var counts = new int[4];
            for (int i = 0; i < Samples; i++) counts[GrassTintSelector.SelectLayer(HashAt(i), splat, density)]++;
            return counts;
        }

        [Test]
        public void SingleLayer_AlwaysChosen()
        {
            for (int layer = 0; layer < 4; layer++)
            {
                var splat = Vector4.zero;
                splat[layer] = 1f;
                int[] counts = CountLayers(splat, Vector4.one);
                Assert.AreEqual(Samples, counts[layer], $"layer {layer}");
            }
        }

        [Test]
        public void EqualSplatAndDensity_SplitsAboutHalf()
        {
            int[] counts = CountLayers(new Vector4(0.5f, 0.5f, 0f, 0f), Vector4.one);
            float pct = 100f * counts[0] / Samples;
            Assert.AreEqual(50f, pct, 3f, $"counts {counts[0]}/{counts[1]}");
            Assert.AreEqual(Samples, counts[0] + counts[1]);
        }

        [Test]
        public void DensityWeightsTheChoice()
        {
            // Splat eşit, yoğunluk 3:1 => layer 0 ~%75.
            int[] counts = CountLayers(new Vector4(0.5f, 0.5f, 0f, 0f), new Vector4(1f, 1f / 3f, 1f, 1f));
            Assert.AreEqual(75f, 100f * counts[0] / Samples, 3f);
        }

        [Test]
        public void ZeroDensityLayer_NeverChosen()
        {
            // Snow benzeri: splat'te ağırlığı büyük ama yoğunluğu 0.
            int[] counts = CountLayers(new Vector4(0.2f, 0.2f, 0.6f, 0f), new Vector4(1f, 1f, 0f, 1f));
            Assert.AreEqual(0, counts[2]);
            Assert.AreEqual(Samples, counts[0] + counts[1]);
        }

        [Test]
        public void ZeroWeightLayerAtEnd_NeverChosen()
        {
            // Son layer'ın ağırlığı 0: pick*total yuvarlama uç durumunda bile seçilmemeli.
            int[] counts = CountLayers(new Vector4(0.5f, 0.3f, 0.2f, 0f), Vector4.one);
            Assert.AreEqual(0, counts[3]);
        }

        [Test]
        public void ZeroTotalWeight_FallsBackToLayerZero()
        {
            Assert.AreEqual(0, GrassTintSelector.SelectLayer(HashAt(7), Vector4.zero, Vector4.one));
            Assert.AreEqual(0, GrassTintSelector.SelectLayer(HashAt(7), Vector4.one, Vector4.zero));
        }

        [Test]
        public void ComputeColor_IsDeterministic()
        {
            var splat = new Vector4(0.4f, 0.3f, 0.2f, 0.1f);
            for (int i = 0; i < 200; i++)
            {
                Vector4 a = GrassTintSelector.ComputeColor(HashAt(i), splat, Vector4.one, Tints, 0.08f);
                Vector4 b = GrassTintSelector.ComputeColor(HashAt(i), splat, Vector4.one, Tints, 0.08f);
                Assert.AreEqual(a, b);
            }
        }

        [Test]
        public void ComputeColor_JitterStaysWithinBoundsAndShadeUntouched()
        {
            const float jitter = 0.08f;
            var splat = new Vector4(1f, 0f, 0f, 0f);
            bool sawBrighter = false, sawDarker = false;
            for (int i = 0; i < Samples; i++)
            {
                Vector4 c = GrassTintSelector.ComputeColor(HashAt(i), splat, Vector4.one, Tints, jitter);
                for (int k = 0; k < 3; k++)
                {
                    Assert.GreaterOrEqual(c[k], Tints[0][k] * (1f - jitter) - 1e-5f);
                    Assert.LessOrEqual(c[k], Tints[0][k] * (1f + jitter) + 1e-5f);
                }
                Assert.AreEqual(Tints[0].w, c.w, 1e-6f, "shade jitter'a girmemeli");
                sawBrighter |= c.y > Tints[0].y;
                sawDarker |= c.y < Tints[0].y;
            }
            Assert.IsTrue(sawBrighter && sawDarker, "jitter iki yöne de dağılmalı");
        }

        [Test]
        public void ComputeColor_ClampsToUnitRange()
        {
            var splat = new Vector4(0f, 0f, 1f, 0f); // uç (0.9) * 1.5 > 1
            for (int i = 0; i < 500; i++)
            {
                Vector4 c = GrassTintSelector.ComputeColor(HashAt(i), splat, Vector4.one, Tints, 0.5f);
                for (int k = 0; k < 4; k++)
                {
                    Assert.GreaterOrEqual(c[k], 0f);
                    Assert.LessOrEqual(c[k], 1f);
                }
            }
        }

        [Test]
        public void ZeroJitter_ReturnsLayerTintExactly()
        {
            Vector4 c = GrassTintSelector.ComputeColor(HashAt(3), new Vector4(0f, 1f, 0f, 0f), Vector4.one, Tints, 0f);
            Assert.AreEqual(Tints[1], c);
        }

        [Test]
        public void Stream01_IsInHalfOpenUnitRange()
        {
            for (int i = 0; i < 2000; i++)
            {
                float v = GrassTintSelector.Stream01(HashAt(i), GrassTintSelector.StreamSelect);
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test]
        public void HeightMultiplier_UsesSelectedLayerOnly()
        {
            var heights = new Vector4(1f, 0.8f, 1.5f, 0.5f);
            for (int layer = 0; layer < 4; layer++)
            {
                var splat = Vector4.zero;
                splat[layer] = 1f;
                for (int i = 0; i < 200; i++)
                {
                    uint h = HashAt(i);
                    int chosen = GrassTintSelector.SelectLayer(h, splat, Vector4.one);
                    Assert.AreEqual(layer, chosen);
                    Assert.AreEqual(0.6f * heights[layer], GrassTintSelector.ApplyHeightMultiplier(0.6f, heights, chosen), 1e-6f);
                }
            }
        }

        [Test]
        public void HeightMultiplier_HalfHalvesAndOneKeeps()
        {
            var heights = new Vector4(1f, 0.5f, 1f, 1f);
            Assert.AreEqual(0.4f, GrassTintSelector.ApplyHeightMultiplier(0.8f, heights, 1), 1e-6f);
            Assert.AreEqual(0.8f, GrassTintSelector.ApplyHeightMultiplier(0.8f, heights, 0), 1e-6f);
        }

        [Test]
        public void MixedSplat_HeightAndColorShareTheSameLayer()
        {
            // Renk ve boy aynı SelectLayer sonucundan türer: tint ile çarpanın layer'ı hiçbir örnekte ayrışmamalı.
            var splat = new Vector4(0.5f, 0.5f, 0f, 0f);
            var heights = new Vector4(1f, 0.5f, 1f, 1f);
            for (int i = 0; i < 500; i++)
            {
                uint h = HashAt(i);
                int layer = GrassTintSelector.SelectLayer(h, splat, Vector4.one);
                Vector4 c = GrassTintSelector.ComputeColor(h, splat, Vector4.one, Tints, 0f);
                Assert.AreEqual(Tints[layer], c);
                Assert.AreEqual(heights[layer], GrassTintSelector.ApplyHeightMultiplier(1f, heights, layer));
            }
        }
    }
}
