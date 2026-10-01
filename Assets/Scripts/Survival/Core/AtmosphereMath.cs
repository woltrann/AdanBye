using System;

namespace AdanBye.Survival
{
    public static class AtmosphereMath
    {
        // İç mekanda dış taban yoğunluk yok sayılır ama hacimler yine ekler:
        // "kirli iç mekan" (ör. zehirli oda) tasarlanabilsin diye.
        public static float Density(bool isIndoor, float baseOutdoor, float volumeSum, float maxDensity)
        {
            float raw = (isIndoor ? 0f : baseOutdoor) + volumeSum;
            return Math.Max(0f, Math.Min(maxDensity, raw));
        }
    }
}
