using System;

namespace AdanBye.Survival
{
    // Atlanacak oyun saatini seçer. Rastgelelik dışarıdan (t01) verilir: testte deterministik olsun diye.
    public static class TimeSkipMath
    {
        // t01 0..1 dışındaysa sınırlanır. min/max ters girilirse ya da negatifse Inspector hatası
        // oyunu bozmasın diye sessizce düzeltilir (negatif -> 0, ters -> yer değiştirir). NaN -> 0 saat.
        public static float PickHours(float minHours, float maxHours, float t01)
        {
            if (float.IsNaN(minHours) || float.IsNaN(maxHours) || float.IsNaN(t01)) return 0f;

            float lo = Math.Max(0f, Math.Min(minHours, maxHours));
            float hi = Math.Max(0f, Math.Max(minHours, maxHours));
            float t = Math.Max(0f, Math.Min(1f, t01));
            return lo + (hi - lo) * t;
        }
    }
}
