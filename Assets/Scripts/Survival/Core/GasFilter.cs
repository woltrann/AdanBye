using System;

namespace AdanBye.Survival
{
    // Gaz filtresi: zamana değil, çağıranın verdiği miktara göre azalır
    // (dışarıda olma süresini ölçmek çağıranın sorumluluğu).
    public sealed class GasFilter
    {
        public const float MaxValue = 100f;

        public float Value { get; private set; } = MaxValue;
        public bool IsEmpty => Value <= 0f;

        public void Drain(float amount)
        {
            if (amount <= 0f) return;
            Set(Value - amount);
        }

        public void Refill() => Value = MaxValue;

        public void Restore(float amount) => Set(Value + amount);

        private void Set(float v) => Value = Math.Max(0f, Math.Min(MaxValue, v));
    }
}
