using System;

namespace AdanBye.Survival
{
    public enum StaminaActivity
    {
        Idle,
        Walk,
        Run
    }

    // Sade alan sınıfı: Unity tarafı bunu inspector'da gösterip modele verir,
    // model ise UnityEngine'e bağlı kalmadan test edilebilir.
    [Serializable]
    public sealed class StaminaConfig
    {
        // Ana stamina ve koşu staminasının üst sınırı.
        public float Max = 100f;

        // --- KOŞU STAMİNASI (StaminaModel.Current) ---
        public float RunDrainPerSecond = 12f;
        public float RegenPerSecond = 8f;
        // Koşu bittikten sonra dolum başlamadan önce beklenen süre (koş-dinlen-koş döngüsünü yavaşlatır).
        public float RegenDelaySeconds = 1f;
        // CanRun histerezisi: 0'a inince kapanır, bu eşiğe çıkınca açılır (titremeyi önler).
        public float RunResumeThreshold = 20f;

        // --- ANA STAMİNA (StaminaModel.Ceiling): kamp dışında geri kazanılamaz, 0'a inince bayılma ---
        public float CeilingDrainIdlePerSecond = 0.05f;
        public float CeilingDrainRunPerSecond = 0.3f;
        // Ana stamina 0'a inince hemen çökmek yerine bu süre "bitkin" kalınır (zırt pırt bayılmayı önler,
        // 3 sn nefes nefese yürüme ile bayılma geliyor hissi verir). 0 = anında bayıl. Bu sürede dolum ve koşu yok.
        public float ExhaustionGraceSeconds = 3f;
        // Bitkinken hız çarpanı (yorgunluk yavaşlatmasıyla çarpılır); 0.7 belirgin ama kilitlemeyen yavaşlık.
        public float ExhaustedSpeedMultiplier = 0.7f;
        // Uyanınca ana stamina Max'ın bu kadarı olur (koşu staminası da buna eşitlenir): zorunlu uyku az da olsa doldurur.
        public float WakeMainStaminaFraction = 0.25f;

        public float CollapseDurationSeconds = 4f;

        // Ana stamina / Max bu oranın altına inerse yürüme/koşu hızı çarpanla yavaşlar.
        public float FatigueSlowdownThreshold = 0.30f;
        public float FatigueSlowdownMultiplier = 0.85f;
    }
}
