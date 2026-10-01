using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class StaminaFillMathTests
    {
        [Test]
        public void Ratio_IsValueOverMax()
        {
            Assert.AreEqual(0.25f, StaminaFillMath.Ratio(25f, 100f), 0.0001f);
        }

        [Test]
        public void Ratio_ClampedToZeroAndOne()
        {
            Assert.AreEqual(1f, StaminaFillMath.Ratio(150f, 100f));
            Assert.AreEqual(0f, StaminaFillMath.Ratio(-5f, 100f));
        }

        [Test]
        public void Ratio_NonPositiveMax_ReturnsZero()
        {
            Assert.AreEqual(0f, StaminaFillMath.Ratio(10f, 0f));
            Assert.AreEqual(0f, StaminaFillMath.Ratio(10f, -1f));
            Assert.AreEqual(0f, StaminaFillMath.Ratio(10f, float.NaN));
        }
    }
}
