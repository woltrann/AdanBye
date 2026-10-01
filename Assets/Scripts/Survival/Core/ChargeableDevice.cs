using System;

namespace AdanBye.Survival
{
    // Telefon/saat/fener gibi şarjlı cihazların saf durum modeli.
    // UnityEngine'e bağımlı değil: zamanı (dt) ve niyeti (tüketim/şarj) dışarıdan alır,
    // böylece EditMode testinde sahne/coroutine olmadan doğrulanabilir.
    public sealed class ChargeableDevice
    {
        private readonly float drainPerSecond;
        private readonly float chargePerSecond;

        public float Max { get; }
        public float Value { get; private set; }
        public bool IsEmpty => Value <= 0f;

        public ChargeableDevice(float drainPerSecond, float chargePerSecond, float max = 100f, float initial = -1f)
        {
            if (max <= 0f) throw new ArgumentOutOfRangeException(nameof(max));
            if (drainPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(drainPerSecond));
            if (chargePerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(chargePerSecond));

            this.drainPerSecond = drainPerSecond;
            this.chargePerSecond = chargePerSecond;
            Max = max;
            // Negatif initial = "belirtilmedi" -> dolu başla (eski UXobjects varsayılanı 100).
            Value = initial < 0f ? max : Math.Min(initial, max);
        }

        // Şarj önceliklidir: eski UXobjects'te isRecharge true ise artıyor, değilse azalıyordu.
        public void Tick(float dt, bool consuming, bool charging)
        {
            if (dt <= 0f) return;

            if (charging) Set(Value + chargePerSecond * dt);
            else if (consuming) Set(Value - drainPerSecond * dt);
        }

        public void Fill() => Value = Max;

        public void Restore(float amount) => Set(Value + amount);

        private void Set(float v) => Value = Math.Max(0f, Math.Min(Max, v));
    }
}
