using System;

namespace AdanBye.Survival
{
    // "Her N saniyede bir" işleri (açlık/susuzluk/droid) için dt tabanlı sayaç.
    // Coroutine + WaitForSeconds yerine kullanılır: allocation yok, duraklatma/test kolay,
    // ve kare süresi aralıktan uzunsa kaçan tick'ler kaybolmaz (artık süre biriktirilir).
    // Not: ilk tick Start anında değil, bir tam aralık sonra gelir (eski coroutine hemen düşürüyordu).
    public sealed class IntervalTicker
    {
        private float accumulator;

        public float Interval { get; }

        public IntervalTicker(float interval)
        {
            if (interval <= 0f) throw new ArgumentOutOfRangeException(nameof(interval));
            Interval = interval;
        }

        // Geçen dt'de kaç tam aralık dolduğunu döndürür.
        public int Advance(float dt)
        {
            if (dt <= 0f) return 0;

            accumulator += dt;
            if (accumulator < Interval) return 0;

            int ticks = (int)(accumulator / Interval);
            accumulator -= ticks * Interval;
            return ticks;
        }

        public void Reset() => accumulator = 0f;
    }
}
