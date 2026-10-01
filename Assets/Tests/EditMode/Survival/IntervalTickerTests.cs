using System;
using AdanBye.Survival;
using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class IntervalTickerTests
    {
        [Test]
        public void Advance_BelowInterval_NoTick()
        {
            var t = new IntervalTicker(5f);
            Assert.AreEqual(0, t.Advance(4.9f));
        }

        [Test]
        public void Advance_ReachesInterval_OneTick()
        {
            var t = new IntervalTicker(5f);
            t.Advance(3f);
            Assert.AreEqual(1, t.Advance(2f));
        }

        [Test]
        public void Advance_LongFrame_ReturnsAllMissedTicks()
        {
            var t = new IntervalTicker(7f);
            Assert.AreEqual(3, t.Advance(22f));
        }

        [Test]
        public void Advance_KeepsRemainderAcrossCalls()
        {
            var t = new IntervalTicker(10f);
            Assert.AreEqual(1, t.Advance(12f));
            Assert.AreEqual(0, t.Advance(7f));
            Assert.AreEqual(1, t.Advance(1f)); // 2 + 7 + 1 = 10
        }

        [Test]
        public void Advance_NonPositiveDt_Ignored()
        {
            var t = new IntervalTicker(1f);
            Assert.AreEqual(0, t.Advance(0f));
            Assert.AreEqual(0, t.Advance(-3f));
        }

        [Test]
        public void Reset_ClearsAccumulatedTime()
        {
            var t = new IntervalTicker(10f);
            t.Advance(9f);
            t.Reset();
            Assert.AreEqual(0, t.Advance(2f));
        }

        [Test]
        public void Constructor_NonPositiveInterval_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalTicker(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalTicker(-1f));
        }
    }
}
