using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class AtmosphereMathTests
    {
        [Test]
        public void Outdoor_AddsBaseAndVolumes()
        {
            Assert.AreEqual(0.7f, AtmosphereMath.Density(false, 0.5f, 0.2f, 1f), 0.0001f);
        }

        [Test]
        public void Indoor_IgnoresBase_ButKeepsVolumes()
        {
            Assert.AreEqual(0f, AtmosphereMath.Density(true, 0.5f, 0f, 1f), 0.0001f);
            Assert.AreEqual(0.2f, AtmosphereMath.Density(true, 0.5f, 0.2f, 1f), 0.0001f);
        }

        [Test]
        public void Density_ClampedToZeroAndMax()
        {
            Assert.AreEqual(1f, AtmosphereMath.Density(false, 0.8f, 0.8f, 1f));
            Assert.AreEqual(0f, AtmosphereMath.Density(false, 0.2f, -0.9f, 1f));
        }
    }

    public class ToxinExposureModelTests
    {
        private const float Tol = 0.0001f;

        private static ToxinExposureModel Create() => new ToxinExposureModel(new ToxinExposureConfig());

        [Test]
        public void FilterFull_NoPoison_FilterDrainsWithDensity()
        {
            var f = new GasFilter();
            // Birim: dt OYUN SAATİ. 1 saat, yoğunluk 1 -> 100 birim/saat.
            float poison = Create().Tick(0.1f, 1f, f);
            Assert.AreEqual(0f, poison);
            Assert.AreEqual(100f - 10f, f.Value, Tol);

            var f2 = new GasFilter();
            Create().Tick(0.1f, 0.5f, f2);
            Assert.AreEqual(100f - 5f, f2.Value, Tol);
        }

        [Test]
        public void FilterEmpty_PoisonProportionalToDensity()
        {
            var f = new GasFilter();
            f.Drain(100f);
            var m = Create();
            // Zehir 80/oyun saati @ yoğunluk 1.
            Assert.AreEqual(80f, m.Tick(1f, 1f, f), Tol);
            Assert.AreEqual(80f, m.Tick(2f, 0.5f, f), Tol);
            Assert.AreEqual(0f, f.Value);
        }

        [Test]
        public void ZeroDensity_ChangesNothing()
        {
            var full = new GasFilter();
            Assert.AreEqual(0f, Create().Tick(5f, 0f, full));
            Assert.AreEqual(100f, full.Value);

            var empty = new GasFilter();
            empty.Drain(100f);
            Assert.AreEqual(0f, Create().Tick(5f, 0f, empty));
            Assert.AreEqual(0f, empty.Value);
        }

        [Test]
        public void FilterEmptiesMidTick_PoisonOnlyForRemainder()
        {
            var f = new GasFilter();
            f.Drain(99f); // 1 kaldı; density 1'de 100/saat hızla 0.01 saatte biter
            float poison = Create().Tick(0.02f, 1f, f);

            Assert.IsTrue(f.IsEmpty);
            Assert.AreEqual(0.01f * 80f, poison, Tol);
        }

        [Test]
        public void NonPositiveDtOrNaN_HasNoEffect()
        {
            var f = new GasFilter();
            var m = Create();
            Assert.AreEqual(0f, m.Tick(0f, 1f, f));
            Assert.AreEqual(0f, m.Tick(-1f, 1f, f));
            Assert.AreEqual(0f, m.Tick(float.NaN, 1f, f));
            Assert.AreEqual(100f, f.Value);
        }
    }
}
