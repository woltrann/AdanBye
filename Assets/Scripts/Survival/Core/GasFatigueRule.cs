using System;

namespace AdanBye.Survival
{
    // WP-9 karar mantığı: zehir oranı eşiği aşınca yorgunluk (tavan kaybı) çarpanı artar.
    public static class GasFatigueRule
    {
        // max <= 0 veya NaN güvenli: oran 0 sayılır.
        public static float PoisonRatio(float current, float max)
        {
            if (!(max > 0f) || !(current > 0f)) return 0f;
            return Math.Min(1f, current / max);
        }

        // Gaz yorgunluğu asla yavaşlatmaz: çarpan 1'in altına inmez.
        public static float Multiplier(float current, float max, float threshold, float poisonedMultiplier)
        {
            return PoisonRatio(current, max) >= threshold ? Math.Max(1f, poisonedMultiplier) : 1f;
        }
    }
}
