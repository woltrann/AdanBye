using AdanBye.Survival;
using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class ChargeableDeviceTests
    {
        [Test]
        public void StartsFull()
        {
            Assert.AreEqual(100f, new ChargeableDevice(0.1f, 1f).Value);
        }

        [Test]
        public void Consuming_DrainsAndClampsAtZero()
        {
            var d = new ChargeableDevice(1f, 1f);
            d.Tick(10f, consuming: true, charging: false);
            Assert.AreEqual(90f, d.Value, 0.001f);
            d.Tick(1000f, true, false);
            Assert.AreEqual(0f, d.Value);
            Assert.IsTrue(d.IsEmpty);
        }

        [Test]
        public void Charging_ClampsAtMax()
        {
            var d = new ChargeableDevice(1f, 5f, initial: 50f);
            d.Tick(1f, false, true);
            Assert.AreEqual(55f, d.Value, 0.001f);
            d.Tick(1000f, false, true);
            Assert.AreEqual(100f, d.Value);
        }

        [Test]
        public void ChargingAndConsuming_ChargeWins()
        {
            var d = new ChargeableDevice(1f, 5f, initial: 50f);
            d.Tick(2f, consuming: true, charging: true);
            Assert.AreEqual(60f, d.Value, 0.001f);
        }

        [Test]
        public void NeitherFlag_NoChange()
        {
            var d = new ChargeableDevice(1f, 5f, initial: 50f);
            d.Tick(5f, false, false);
            Assert.AreEqual(50f, d.Value);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void NonPositiveDt_HasNoEffect(float dt)
        {
            var d = new ChargeableDevice(1f, 5f, initial: 50f);
            d.Tick(dt, true, false);
            d.Tick(dt, false, true);
            Assert.AreEqual(50f, d.Value);
        }

        [Test]
        public void Fill_RestoresMax()
        {
            var d = new ChargeableDevice(1f, 1f, initial: 3f);
            d.Fill();
            Assert.AreEqual(100f, d.Value);
        }

        [Test]
        public void Restore_AddsAndClamps()
        {
            var d = new ChargeableDevice(1f, 1f, initial: 10f);
            d.Restore(20f);
            Assert.AreEqual(30f, d.Value, 0.001f);
            d.Restore(500f);
            Assert.AreEqual(100f, d.Value);
            d.Restore(-500f);
            Assert.AreEqual(0f, d.Value);
        }
    }
}
