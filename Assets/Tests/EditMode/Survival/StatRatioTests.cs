using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    // StatBarView'in tüm kaynakları (açlık/susuzluk/stamina) oranı StaminaFillMath.Ratio ile alır; sınır davranışı burada kilitlenir.
    public class StatRatioTests
    {
        [TestCase(50f, 100f, 0.5f)]   // açlık/susuzluk tipik durum
        [TestCase(0f, 100f, 0f)]
        [TestCase(100f, 100f, 1f)]
        [TestCase(150f, 100f, 1f)]    // max düşerken current bir kare geride kalabilir
        [TestCase(-5f, 100f, 0f)]
        [TestCase(10f, 0f, 0f)]       // Max<=0: bölme hatası yok
        [TestCase(10f, -1f, 0f)]
        [TestCase(float.NaN, 100f, 0f)]
        [TestCase(10f, float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 100f, 1f)]
        [TestCase(float.NegativeInfinity, 100f, 0f)]
        public void Ratio_HandlesBounds(float value, float max, float expected)
        {
            Assert.AreEqual(expected, StaminaFillMath.Ratio(value, max), 0.0001f);
        }
    }
}
