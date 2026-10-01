using AdanBye.Survival;

// Survival sistemleri saati DayCycle'a doğrudan bağlanmadan bulsun diye tek nokta: sahne bağlantısı gerektirmez
// (DayCycle.Awake'te Instance'ı kendi atar). Saat kaynağı değişirse sadece burası değişir.
public static class GameClockLocator
{
    // Bulunamazsa null; çağıran güvenli davranmalı. DayCycle destroy edildiyse Unity null'ı da null döner.
    public static IGameClock Find() => DayCycle.Instance;
}
