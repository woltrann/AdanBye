using System;

namespace AdanBye.Survival
{
    // Birimler OYUN SAATİ tabanlı: saat hızı (dayDuration) değişince boşalma/zehir gerçek sürede kendiliğinden ölçeklenir.
    // NOT: Eski alan adları (…PerSecondAtFullDensity) bilerek kullanılmadı; PlayerMain.prefab'da eski değer
    // (0.1428…) serileştirilmiş ve yeni anlamla yeniden kullanılsaydı yanlış yüklenirdi.
    [Serializable]
    public sealed class ToxinExposureConfig
    {
        // Yoğunluk 1'de filtrenin oyun saati başına kaybettiği değer. Hız yoğunlukla orantılı:
        // süre(oyun saati) = 100 / (bu değer x yoğunluk). Dış mekan taban yoğunluğu 0.2 ile 100 -> 5 oyun saati.
        public float FilterDrainPerGameHourAtFullDensity = 100f;
        // Yoğunluk 1'de filtre boşken zehirin oyun saati başına artışı. 80 = eski his
        // (0.5/sn x 160 sn/oyun-saati; dayDuration=3840'ta 1 oyun saati = 160 gerçek sn). Tasarımcı ayarlar.
        public float PoisonPerGameHourAtFullDensity = 80f;
    }

    // Filtre doluyken zehir engellenir (filtre harcanır); boşken zehir birikir.
    public sealed class ToxinExposureModel
    {
        private readonly ToxinExposureConfig config;

        public ToxinExposureModel(ToxinExposureConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // dtGameHours OYUN SAATİ cinsindendir (gerçek saniye değil; dönüşüm çağıranda: GameTimeConversion).
        // Dönüş: bu tick'teki zehir artışı. Filtre bu tick'te boşalırsa, boşaldığı andan
        // sonraki süre kısmı için zehir hesaplanır.
        public float Tick(float dtGameHours, float density, GasFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (!(dtGameHours > 0f) || float.IsInfinity(dtGameHours) || !(density > 0f)) return 0f;

            float poisonRate = density * config.PoisonPerGameHourAtFullDensity;
            if (filter.IsEmpty) return poisonRate * dtGameHours;

            float drainRate = density * config.FilterDrainPerGameHourAtFullDensity;
            // Boşaltma hızı 0 ise filtre hiç bitmez.
            if (drainRate <= 0f) return 0f;

            float value = filter.Value;
            float needed = drainRate * dtGameHours;
            if (value > needed)
            {
                filter.Drain(needed);
                return 0f;
            }

            float remainingHours = dtGameHours - value / drainRate;
            filter.Drain(value);
            return poisonRate * remainingHours;
        }
    }
}
