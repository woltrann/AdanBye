using System;

namespace AdanBye.Survival
{
    // Oyun saati <-> gerçek saniye dönüşümü. Hız = "1 gerçek saniyede kaç oyun saati geçer"
    // (DayCycle için 24 / dayDuration). Geçersiz girdi (NaN/sonsuz/<=0) 0 döner:
    // saat bozukken yanlış büyük bir doz uygulamaktansa hiçbir şey yapmamak güvenli.
    public static class GameTimeConversion
    {
        public static float RealSecondsToGameHours(float realSeconds, float gameHoursPerRealSecond)
        {
            if (!IsPositiveFinite(realSeconds) || !IsPositiveFinite(gameHoursPerRealSecond)) return 0f;
            return realSeconds * gameHoursPerRealSecond;
        }

        public static float GameHoursToRealSeconds(float gameHours, float gameHoursPerRealSecond)
        {
            if (!IsPositiveFinite(gameHours) || !IsPositiveFinite(gameHoursPerRealSecond)) return 0f;
            return gameHours / gameHoursPerRealSecond;
        }

        private static bool IsPositiveFinite(float v) => v > 0f && !float.IsInfinity(v);
    }
}
