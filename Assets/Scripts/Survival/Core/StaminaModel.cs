using System;

namespace AdanBye.Survival
{
    // Gerçekçi stamina: koşmak hızlı tüketir, ayaktayken de tavan (Ceiling) yavaşça düşer,
    // Current yalnızca Ceiling'e kadar dolar ve Ceiling kendiliğinden artmaz; tam dolum yalnızca RestAtCamp.
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

        // Bitkinlik başladığı anda tek sefer (ses/HUD kancası).
        public event Action Exhausted;
        // Çökme başladığı anda tek sefer.
        public event Action Collapsed;

        public float Max => config.Max;
        public float Current { get; private set; }
        public float Ceiling { get; private set; }
        public bool IsCollapsed { get; private set; }
        public float CollapseRemaining { get; private set; }
        // Stamina 0'a indi, çökmeden önceki ek süre işliyor (nefes nefese: dolum yok, koşu yok).
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
                TickCollapse(dt, fatigueMultiplier);
                return;
            }

            if (IsExhausted)
            {
                TickExhausted(dt, fatigueMultiplier);
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

            if (Current <= 0f)
            {
                BeginExhaustion();
                return;
            }
            UpdateRunHysteresis();
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
            Ceiling = Clamp(ceiling, Math.Min(config.CeilingFloor, config.Max), config.Max);
            Current = Clamp(current, 0f, Ceiling);
            IsCollapsed = false;
            CollapseRemaining = 0f;
            // Current 0 ile yüklendiyse bir sonraki Tick bitkinliği doğal olarak yeniden başlatır.
            IsExhausted = false;
            ExhaustionRemaining = 0f;
            // Yükleme sonrası dolum hemen başlamasın (yükle-dolum istismarını önler).
            timeSinceRun = 0f;
            canRun = Current >= RunThreshold;
        }

        private float RunThreshold => Math.Min(config.RunResumeThreshold, Ceiling);

        private void TickExhausted(float dt, float fatigueMultiplier)
        {
            // Dolum yok: kurtulma şansı verilmez, süre bitince kesin çöker. Yorgunluk Idle gibi birikir.
            DrainCeiling(dt, config.CeilingDrainIdlePerSecond, fatigueMultiplier);
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
        }

        private void TickCollapse(float dt, float fatigueMultiplier)
        {
            // Yorgunluk çökmede de birikir (Idle gibi), ama regen yok.
            DrainCeiling(dt, config.CeilingDrainIdlePerSecond, fatigueMultiplier);
            CollapseRemaining -= dt;
            if (CollapseRemaining > 0f) return;

            IsCollapsed = false;
            CollapseRemaining = 0f;
            Current = Ceiling * config.PostCollapseStaminaFraction;
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
            // Ceza taban altına indirmez; zaten tabandaysa dokunmaz.
            if (Ceiling > config.CeilingFloor)
                Ceiling = Math.Max(config.CeilingFloor, Ceiling - config.CollapseCeilingPenalty);
            Collapsed?.Invoke();
        }

        private void DrainCeiling(float dt, float perSecond, float fatigueMultiplier)
        {
            if (fatigueMultiplier < 0f) fatigueMultiplier = 0f;
            // Zaten tabanın altındaysa (ör. Restore) tavanı yukarı çekmemek için erken çık.
            if (Ceiling <= config.CeilingFloor) return;
            Ceiling = Math.Max(config.CeilingFloor, Ceiling - perSecond * fatigueMultiplier * dt);
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
