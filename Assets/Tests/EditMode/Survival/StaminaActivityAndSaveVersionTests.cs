using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class StaminaActivityResolverTests
    {
        [Test]
        public void Stationary_IsIdle_EvenIfRunHeld()
        {
            Assert.AreEqual(StaminaActivity.Idle, StaminaActivityResolver.Resolve(0f, true));
            Assert.AreEqual(StaminaActivity.Idle, StaminaActivityResolver.Resolve(0.05f, false));
        }

        [Test]
        public void Moving_WithoutRun_IsWalk()
        {
            Assert.AreEqual(StaminaActivity.Walk, StaminaActivityResolver.Resolve(3f, false));
        }

        [Test]
        public void Moving_WithRun_IsRun()
        {
            Assert.AreEqual(StaminaActivity.Run, StaminaActivityResolver.Resolve(8f, true));
        }

        [Test]
        public void NaNSpeed_IsIdle()
        {
            Assert.AreEqual(StaminaActivity.Idle, StaminaActivityResolver.Resolve(float.NaN, true));
        }
    }

    public class SaveVersionPolicyTests
    {
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        public void HasSurvivalData_OnlyFromVersion2(int version, bool expected)
        {
            Assert.AreEqual(expected, SaveVersionPolicy.HasSurvivalData(version));
        }

        [Test]
        public void Current_HasSurvivalData()
        {
            Assert.IsTrue(SaveVersionPolicy.HasSurvivalData(SaveVersionPolicy.Current));
        }
    }
}
