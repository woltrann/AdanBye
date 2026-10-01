using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class GasFatigueRuleTests
    {
        [Test]
        public void BelowThreshold_IsOne() =>
            Assert.AreEqual(1f, GasFatigueRule.Multiplier(49f, 100f, 0.5f, 1.5f));

        [Test]
        public void AtOrAboveThreshold_IsPoisonedMultiplier()
        {
            Assert.AreEqual(1.5f, GasFatigueRule.Multiplier(50f, 100f, 0.5f, 1.5f));
            Assert.AreEqual(1.5f, GasFatigueRule.Multiplier(100f, 100f, 0.5f, 1.5f));
        }

        [Test]
        public void MultiplierBelowOne_ClampedToOne() =>
            Assert.AreEqual(1f, GasFatigueRule.Multiplier(100f, 100f, 0.5f, 0.5f));

        [Test]
        public void InvalidMax_TreatedAsNoPoison()
        {
            Assert.AreEqual(0f, GasFatigueRule.PoisonRatio(10f, 0f));
            Assert.AreEqual(1f, GasFatigueRule.Multiplier(10f, 0f, 0.5f, 2f));
        }

        [Test]
        public void Ratio_ClampedToOne() =>
            Assert.AreEqual(1f, GasFatigueRule.PoisonRatio(150f, 100f));
    }
}
