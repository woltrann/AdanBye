using System;

namespace AdanBye.Survival
{
    public static class StaminaFillMath
    {
        // HUD fillAmount'ı 0..1 bekler; Max<=0 (henüz kurulmamış model) bölme hatası yerine 0 verir.
        // NaN (max ya da value) UI'ye sızmasın diye 0'a çevrilir; Image.fillAmount'a NaN yazmak grafiği bozar.
        public static float Ratio(float value, float max)
        {
            if (!(max > 0f) || float.IsNaN(value)) return 0f;
            return Math.Max(0f, Math.Min(1f, value / max));
        }
    }
}
