using System;

namespace AdanBye.Survival
{
    // İKİ HAVUZ:
    //  - Koşu staminası (Current): koşunca azalır, durunca dolar; 0'da yalnızca koşu kapanır (bayılma/bitkinlik yok).
    //  - Ana stamina (Ceiling): aktiviteden bağımsız yavaşça azalır, kendiliğinden artmaz (tam dolum yalnızca RestAtCamp);
    //    0'a inince bitkinlik -> bayılma (yürüse bile). Koşu staminası her zaman ana staminayı aşamaz.
    // Alan adları kayıt uyumu için korundu (currentStamina = Current, staminaCeiling = Ceiling).
    // Zamanı dt ile çağıran verir; model sahne/Unity bilmez.
    public sealed class StaminaModel
    {
        private readonly StaminaConfig config;
        private float timeSinceRun;
        private bool canRun = true;

        public StaminaModel(StaminaConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (config.Max <= 0f) throw new ArgumentOutOfRangeException(nameof(config), "Max > 0 olmalı");
            Ceiling = config.Max;
            Current = config.Max;
            timeSinceRun = config.RegenDelaySeconds;
        }

        // Ana stamina 0'a indiği anda tek sefer (ses/HUD kancası).
        public event Action Exhausted;
        // Çökme başladığı anda tek sefer.
        public event Action Collapsed;

        public float Max => config.Max;
        // Koşu staminası.
        public float Current { get; private set; }
        // Ana stamina (eski adıyla yorgunluk tavanı).
        public float Ceiling { get; private set; }
        public bool IsCollapsed { get; private set; }
        public float CollapseRemaining { get; private set; }
        // Ana stamina 0'a indi, çökmeden önceki ek süre işliyor (nefes nefese: dolum yok, koşu yok).
        public bool IsExhausted { get; private set; }
        public float ExhaustionRemaining { get; private set; }
        public bool CanRun => canRun && !IsCollapsed && !IsExhausted;

        // Çökmede 0 değildir: hareket kilidi ayrı bir sorumluluk (IsCollapsed'ı okuyan taraf).
        // Bitkinlikte yorgunluk yavaşlatmasının üstüne ek çarpan biner.
        public float SpeedMultiplier =>
            (Ceiling / Max < config.FatigueSlowdownThreshold ? config.FatigueSlowdownMultiplier : 1f)
            * (IsExhausted ? config.ExhaustedSpeedMultiplier : 1f);

        public void Tick(float dt, StaminaActivity activity, float fatigueMultiplier = 1f)
        {
            // !(dt > 0) NaN'ı da eler; sonsuz dt durumu bozar.
            if (!(dt > 0f) || float.IsInfinity(dt)) return;

            if (IsCollapsed)
            {
                TickCollapse(dt);
                return;
            }

            if (IsExhausted)
            {
                TickExhausted(dt);
                return;
            }

            // CanRun kapalıyken Run istenirse yürüme sayılır: model kuralı kendisi zorlar.
            bool running = activity == StaminaActivity.Run && canRun;

            DrainCeiling(dt, running ? config.CeilingDrainRunPerSecond : config.CeilingDrainIdlePerSecond, fatigueMultiplier);

            if (running)
            {
                timeSinceRun = 0f;
                Current -= config.RunDrainPerSecond * dt;
            }
            else
            {
                Regenerate(dt);
            }

            Current = Clamp(Current, 0f, Ceiling);

            // Bayılma yalnızca ana staminaya bağlı; koşu staminasının 0'ı sadece koşuyu kapatır.
            if (Ceiling <= 0f)
            {
                BeginExhaustion();
                return;
            }
            if (Current <= 0f) canRun = false;
            else UpdateRunHysteresis();
        }

        public void RestAtCamp()
        {
            Ceiling = config.Max;
            Current = config.Max;
            IsCollapsed = false;
            CollapseRemaining = 0f;
            IsExhausted = false;
            ExhaustionRemaining = 0f;
            timeSinceRun = config.RegenDelaySeconds;
            canRun = true;
        }

        // Kayıttan yükleme için; bozuk/aralık dışı değerleri sessizce sınırlar.
        public void Restore(float current, float ceiling)
        {
            Ceiling = Clamp(ceiling, 0f, config.Max);
            Current = Clamp(current, 0f, Ceiling);
            IsCollapsed = false;
            CollapseRemaining = 0f;
            // Ceiling 0 ile yüklendiyse bir sonraki Tick bitkinliği doğal olarak yeniden başlatır.
            IsExhausted = false;
            ExhaustionRemaining = 0f;
            // Yükleme sonrası dolum hemen başlamasın (yükle-dolum istismarını önler).
            timeSinceRun = 0f;
            canRun = Current >= RunThreshold;
        }

        private float RunThreshold => Math.Min(config.RunResumeThreshold, Ceiling);

        // Bitkinken ve çökmedeyken ana stamina zaten 0'dır (bitkinliği tetikleyen odur); düşürülecek bir şey kalmaz,
        // bu yüzden bu iki durumda ayrıca tüketim uygulanmaz. Uyku sırasında yorulma modellenmez (basitlik).
        private void TickExhausted(float dt)
        {
            // Dolum yok: kurtulma şansı verilmez, süre bitince kesin çöker.
            Current = 0f;
            ExhaustionRemaining -= dt;
            if (ExhaustionRemaining > 0f) return;

            // Büyük dt'de artan süre çökmeye aktarılmaz: çökme kendi süresini baştan alır (tek adımda tutarlı).
            IsExhausted = false;
            ExhaustionRemaining = 0f;
            BeginCollapse();
        }

        private void BeginExhaustion()
        {
            IsExhausted = true;
            ExhaustionRemaining = config.ExhaustionGraceSeconds;
            canRun = false;
            Current = 0f;
            Exhausted?.Invoke();
            // Grace 0 ise bir sonraki Tick'i beklemeden çök: "0 = anında" sözleşmesi.
            if (ExhaustionRemaining <= 0f)
            {
                IsExhausted = false;
                ExhaustionRemaining = 0f;
                BeginCollapse();
            }
        }

        private void TickCollapse(float dt)
        {
            CollapseRemaining -= dt;
            if (CollapseRemaining > 0f) return;

            IsCollapsed = false;
            CollapseRemaining = 0f;
            // Zorunlu uyku az da olsa ana staminayı doldurur; koşu staminası ana staminanın tamamına kadar dolu uyanır.
            Ceiling = Clamp(config.Max * config.WakeMainStaminaFraction, 0f, config.Max);
            Current = Ceiling;
            // Hemen dolum başlamasın; toparlanma sonrası da bekleme uygulanır.
            timeSinceRun = 0f;
            UpdateRunHysteresis();
        }

        private void BeginCollapse()
        {
            IsCollapsed = true;
            CollapseRemaining = config.CollapseDurationSeconds;
            canRun = false;
            Current = 0f;
            Collapsed?.Invoke();
        }

        private void DrainCeiling(float dt, float perSecond, float fatigueMultiplier)
        {
            if (fatigueMultiplier < 0f) fatigueMultiplier = 0f;
            Ceiling = Math.Max(0f, Ceiling - perSecond * fatigueMultiplier * dt);
        }

        private void Regenerate(float dt)
        {
            timeSinceRun += dt;
            // Gecikmenin bittiği andan sonraki kısım için dolum yapılır.
            float usable = Math.Min(dt, timeSinceRun - config.RegenDelaySeconds);
            if (usable <= 0f) return;
            Current += config.RegenPerSecond * usable;
        }

        private void UpdateRunHysteresis()
        {
            if (Current >= RunThreshold) canRun = true;
        }

        private static float Clamp(float v, float min, float max) => Math.Max(min, Math.Min(max, v));
    }
}
