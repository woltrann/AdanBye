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
        public float Max = 100f;
        public float RunDrainPerSecond = 12f;
        public float RegenPerSecond = 8f;
        // Koşu bittikten sonra dolum başlamadan önce beklenen süre (koş-dinlen-koş döngüsünü yavaşlatır).
        public float RegenDelaySeconds = 1f;
        // CanRun histerezisi: 0'a inince kapanır, bu eşiğe çıkınca açılır (titremeyi önler).
        public float RunResumeThreshold = 20f;

        // Tavan (kamp dışında geri kazanılamayan üst sınır) kayıpları.
        public float CeilingDrainIdlePerSecond = 0.05f;
        public float CeilingDrainRunPerSecond = 0.3f;
        public float CeilingFloor = 25f;

        public float CollapseDurationSeconds = 4f;
        public float CollapseCeilingPenalty = 10f;
        public float PostCollapseStaminaFraction = 0.15f;

        // Ceiling/Max bu oranın altına inerse yürüme hızı çarpanla yavaşlar.
        public float FatigueSlowdownThreshold = 0.30f;
        public float FatigueSlowdownMultiplier = 0.85f;
    }
}
