using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class GameTimeConversionTests
    {
        private const float Tol = 0.0001f;
        // dayDuration = 3840 sn -> 24 / 3840 oyun saati / gerçek saniye (1 oyun saati = 160 sn).
        private const float Rate3840 = 24f / 3840f;

        [Test]
        public void RealSecondsToGameHours_160SecondsIsOneHour()
        {
            Assert.AreEqual(1f, GameTimeConversion.RealSecondsToGameHours(160f, Rate3840), Tol);
        }

        [Test]
        public void GameHoursToRealSeconds_FiveHoursIs800Seconds_At3840()
        {
            Assert.AreEqual(800f, GameTimeConversion.GameHoursToRealSeconds(5f, Rate3840), 0.01f);
        }

        [Test]
        public void Tutorial480_FiveHoursIs100Seconds()
        {
            Assert.AreEqual(100f, GameTimeConversion.GameHoursToRealSeconds(5f, 24f / 480f), 0.01f);
        }

        [Test]
        public void InvalidInputs_ReturnZero()
        {
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(0f, Rate3840));
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(-1f, Rate3840));
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(float.NaN, Rate3840));
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(1f, 0f));
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(1f, -1f));
            Assert.AreEqual(0f, GameTimeConversion.RealSecondsToGameHours(1f, float.NaN));
            Assert.AreEqual(0f, GameTimeConversion.GameHoursToRealSeconds(1f, 0f));
            Assert.AreEqual(0f, GameTimeConversion.GameHoursToRealSeconds(-1f, Rate3840));
            Assert.AreEqual(0f, GameTimeConversion.GameHoursToRealSeconds(float.NaN, Rate3840));
            Assert.AreEqual(0f, GameTimeConversion.GameHoursToRealSeconds(float.PositiveInfinity, Rate3840));
        }
    }

    // Filtre/zehir artık OYUN SAATİNE bağlı; kullanıcı kararı: taban yoğunlukta (0.2) 5 oyun saati.
    public class ToxinGameTimeTests
    {
        private const float Tol = 0.001f;
        private const float BaseDensity = 0.2f;
        private const float Rate3840 = 24f / 3840f;

        private static ToxinExposureModel Create() => new ToxinExposureModel(new ToxinExposureConfig());

        [Test]
        public void BaseDensity_FilterEmptiesAtExactlyFiveGameHours()
        {
            var almost = new GasFilter();
            Create().Tick(4.99f, BaseDensity, almost);
            Assert.IsFalse(almost.IsEmpty);

            var f = new GasFilter();
            float poison = Create().Tick(5f, BaseDensity, f);
            Assert.IsTrue(f.IsEmpty);
            Assert.AreEqual(0f, poison, Tol);
        }

        [Test]
        public void FullDensity_FilterEmptiesInOneGameHour()
        {
            var almost = new GasFilter();
            Create().Tick(0.99f, 1f, almost);
            Assert.IsFalse(almost.IsEmpty);

            var f = new GasFilter();
            Create().Tick(1f, 1f, f);
            Assert.IsTrue(f.IsEmpty);
        }

        // Kare kare simülasyon: gerçek saniye -> oyun saati dönüşümüyle. Hız 2x ise süre yarıya iner.
        private static float RealSecondsUntilEmpty(float gameHoursPerRealSecond)
        {
            var model = Create();
            var f = new GasFilter();
            const float frame = 0.1f;
            float t = 0f;
            while (!f.IsEmpty && t < 5000f)
            {
                model.Tick(GameTimeConversion.RealSecondsToGameHours(frame, gameHoursPerRealSecond), BaseDensity, f);
                t += frame;
            }
            return t;
        }

        [Test]
        public void At3840_FilterEmptiesIn800RealSeconds()
        {
            Assert.AreEqual(800f, RealSecondsUntilEmpty(Rate3840), 0.5f);
        }

        [Test]
        public void DoubleClockSpeed_FilterEmptiesInHalfTheRealTime()
        {
            float normal = RealSecondsUntilEmpty(Rate3840);
            float doubled = RealSecondsUntilEmpty(Rate3840 * 2f);
            Assert.AreEqual(normal / 2f, doubled, 0.5f);
        }

        [Test]
        public void Poison_FullDensityEmptyFilter_80PerGameHour()
        {
            var f = new GasFilter();
            f.Drain(100f);
            Assert.AreEqual(80f, Create().Tick(1f, 1f, f), Tol);
            // Eski his: 0.5/sn x 160 sn = 80 (dayDuration=3840).
            Assert.AreEqual(0.5f * 160f, Create().Tick(1f, 1f, f), Tol);
        }

        [Test]
        public void TimeSkip_SevenHoursOutdoors_DrainsFilterThenPoisonsRemainder()
        {
            // Atlama dozu: yoğunluk sabit 0.2, 7 saat -> 5 saatte filtre biter, kalan 2 saat zehir: 2 x 0.2 x 80 = 32.
            var f = new GasFilter();
            float poison = Create().Tick(7f, BaseDensity, f);
            Assert.IsTrue(f.IsEmpty);
            Assert.AreEqual(32f, poison, Tol);
        }

        [Test]
        public void TimeSkip_TwoHoursOutdoors_DrainsFortyUnits()
        {
            var f = new GasFilter();
            Create().Tick(2f, BaseDensity, f);
            Assert.AreEqual(60f, f.Value, Tol);
        }

        [Test]
        public void InvalidDt_HasNoEffect()
        {
            var f = new GasFilter();
            var m = Create();
            Assert.AreEqual(0f, m.Tick(0f, BaseDensity, f));
            Assert.AreEqual(0f, m.Tick(-1f, BaseDensity, f));
            Assert.AreEqual(0f, m.Tick(float.NaN, BaseDensity, f));
            Assert.AreEqual(0f, m.Tick(float.PositiveInfinity, BaseDensity, f));
            Assert.AreEqual(100f, f.Value);
        }

        [Test]
        public void ZeroClockSpeed_ConvertedDtIsZero_NothingHappens()
        {
            var f = new GasFilter();
            float hours = GameTimeConversion.RealSecondsToGameHours(1f, 0f);
            Assert.AreEqual(0f, Create().Tick(hours, BaseDensity, f));
            Assert.AreEqual(100f, f.Value);
        }
    }

    public class TimeSkipMathTests
    {
        [Test]
        public void PickHours_InterpolatesBetweenMinAndMax()
        {
            Assert.AreEqual(1f, TimeSkipMath.PickHours(1f, 2f, 0f), 0.0001f);
            Assert.AreEqual(1.5f, TimeSkipMath.PickHours(1f, 2f, 0.5f), 0.0001f);
            Assert.AreEqual(2f, TimeSkipMath.PickHours(1f, 2f, 1f), 0.0001f);
        }

        [Test]
        public void PickHours_SanitizesBadInput()
        {
            Assert.AreEqual(2f, TimeSkipMath.PickHours(1f, 2f, 5f), 0.0001f);   // t sınırlanır
            Assert.AreEqual(1f, TimeSkipMath.PickHours(1f, 2f, -5f), 0.0001f);
            Assert.AreEqual(1.5f, TimeSkipMath.PickHours(2f, 1f, 0.5f), 0.0001f); // ters min/max
            Assert.AreEqual(0f, TimeSkipMath.PickHours(-3f, -1f, 0.5f), 0.0001f);  // negatif -> 0
            Assert.AreEqual(0f, TimeSkipMath.PickHours(float.NaN, 2f, 0.5f));
            Assert.AreEqual(0f, TimeSkipMath.PickHours(1f, 2f, float.NaN));
        }

        [Test]
        public void TwoHourSkip_At3840_GivesCorrectVitalsTicks()
        {
            // 2 oyun saati = 320 gerçek sn eşdeğeri; açlık 7 sn'de bir, susuzluk 5 sn'de bir -> 45 ve 64 tick.
            float realSeconds = GameTimeConversion.GameHoursToRealSeconds(2f, 24f / 3840f);
            Assert.AreEqual(45, new IntervalTicker(7f).Advance(realSeconds));
            Assert.AreEqual(64, new IntervalTicker(5f).Advance(realSeconds));
        }

        [Test]
        public void Skip_DeviceDrainClampsAtZero()
        {
            // Telefon 10 sn/birim: 320 sn -> 32 birim; 100'den 68'e iner. Çok uzun atlama 0'ın altına inmez.
            var phone = new ChargeableDevice(1f / 10f, 1f / 10f);
            phone.Tick(320f, true, false);
            Assert.AreEqual(68f, phone.Value, 0.01f);
            phone.Tick(100000f, true, false);
            Assert.AreEqual(0f, phone.Value);
        }
    }
}
