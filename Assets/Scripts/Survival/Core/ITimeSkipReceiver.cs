namespace AdanBye.Survival
{
    // Zaman atlandığında "o sürede ne olurdu"yu uygulayan sistemler (açlık, şarj, gaz...).
    // Receiver'ın saat bulucuya bağımlı olmaması için iki birim de hazır verilir:
    // gameHours oyun-saati tabanlı sistemler (gaz/zehir), realSecondsEquivalent saniye tabanlı
    // sistemler (açlık aralığı, şarj hızı) içindir. Yoğunluk/durum atlama anındaki değerinde sabit varsayılır.
    public interface ITimeSkipReceiver
    {
        void OnTimeSkipped(float gameHours, float realSecondsEquivalent);
    }
}
