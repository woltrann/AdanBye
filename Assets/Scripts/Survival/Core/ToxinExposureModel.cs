using System;

namespace AdanBye.Survival
{
    [Serializable]
    public sealed class ToxinExposureConfig
    {
        // Yoğunluk 1'de filtrenin saniyede kaybettiği değer.
        public float FilterDrainPerSecondAtFullDensity = 1f / 7f;
        // Yoğunluk 1'de filtre boşken zehirin saniyede artışı.
        public float PoisonPerSecondAtFullDensity = 0.5f;
    }

    // Filtre doluyken zehir engellenir (filtre harcanır); boşken zehir birikir.
    public sealed class ToxinExposureModel
    {
        private readonly ToxinExposureConfig config;

        public ToxinExposureModel(ToxinExposureConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Dönüş: bu tick'teki zehir artışı. Filtre bu tick'te boşalırsa, boşaldığı andan
        // sonraki dt kısmı için zehir hesaplanır.
        public float Tick(float dt, float density, GasFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (!(dt > 0f) || float.IsInfinity(dt) || !(density > 0f)) return 0f;

            float poisonRate = density * config.PoisonPerSecondAtFullDensity;
            if (filter.IsEmpty) return poisonRate * dt;

            float drainRate = density * config.FilterDrainPerSecondAtFullDensity;
            // Boşaltma hızı 0 ise filtre hiç bitmez.
            if (drainRate <= 0f) return 0f;

            float value = filter.Value;
            float needed = drainRate * dt;
            if (value > needed)
            {
                filter.Drain(needed);
                return 0f;
            }

            float remainingDt = dt - value / drainRate;
            filter.Drain(value);
            return poisonRate * remainingDt;
        }
    }
}
