using System;

namespace AdanBye.Survival
{
    public static class StaminaFillMath
    {
        // HUD fillAmount'ı 0..1 bekler; Max<=0 (henüz kurulmamış model) bölme hatası yerine 0 verir.
        public static float Ratio(float value, float max)
        {
            if (!(max > 0f)) return 0f;
            return Math.Max(0f, Math.Min(1f, value / max));
        }
    }
}
