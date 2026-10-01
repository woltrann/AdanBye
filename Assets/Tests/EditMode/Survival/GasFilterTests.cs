using AdanBye.Survival;
using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class GasFilterTests
    {
        [Test]
        public void StartsFullAndNotEmpty()
        {
            var f = new GasFilter();
            Assert.AreEqual(100f, f.Value);
            Assert.IsFalse(f.IsEmpty);
        }

        [Test]
        public void Drain_ReducesAndClampsToEmpty()
        {
            var f = new GasFilter();
            f.Drain(30f);
            Assert.AreEqual(70f, f.Value, 0.001f);
            f.Drain(1000f);
            Assert.AreEqual(0f, f.Value);
            Assert.IsTrue(f.IsEmpty);
        }

        [Test]
        public void Drain_NonPositiveAmount_Ignored()
        {
            var f = new GasFilter();
            f.Drain(-5f);
            f.Drain(0f);
            Assert.AreEqual(100f, f.Value);
        }

        [Test]
        public void RefillAndRestore()
        {
            var f = new GasFilter();
            f.Drain(100f);
            f.Restore(25f);
            Assert.AreEqual(25f, f.Value, 0.001f);
            f.Restore(500f);
            Assert.AreEqual(100f, f.Value);
            f.Drain(60f);
            f.Refill();
            Assert.AreEqual(100f, f.Value);
        }
    }
}
