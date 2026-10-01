namespace AdanBye.Survival
{
    // Oyun saatinin sistemlere açılan yüzü. Survival kodu DayCycle'a (güneş/ışık) değil bu arayüze bağlanır;
    // böylece saat kaynağı değişse ya da testte sahte saat verilse bile mantık aynı kalır.
    public interface IGameClock
    {
        // 1 gerçek saniyede geçen oyun saati (dayDuration=3840 -> 24/3840 = 0.00625; yani 1 oyun saati = 160 sn).
        float GameHoursPerRealSecond { get; }

        // Saati ani ilerletir (çökme/uyku atlaması). Gün sonunda başa sarar.
        void AdvanceHours(float hours);
    }
}
