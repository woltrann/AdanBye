using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class StaminaModelTests
    {
        private const float Tol = 0.001f;
        private const float ExhaustSeconds = 3f; // StaminaConfig.ExhaustionGraceSeconds varsayılanı

        private static StaminaModel Create() => new StaminaModel(new StaminaConfig());

        private static void TickFor(StaminaModel m, float seconds, StaminaActivity a, float step = 0.1f)
        {
            int n = (int)(seconds / step + 0.5f);
            for (int i = 0; i < n; i++) m.Tick(step, a);
        }

        // Tam dolu modeli koşturarak önce bitkinliğe sokar (çökme henüz yok).
        private static StaminaModel Exhausted(out int exhaustedCount)
        {
            var m = Create();
            int count = 0;
            m.Exhausted += () => count++;
            for (int i = 0; i < 200 && !m.IsExhausted; i++) m.Tick(0.1f, StaminaActivity.Run);
            exhaustedCount = count;
            return m;
        }

        // Tam dolu modeli koşturarak çökmeye getirir (0 -> bitkin 3 sn -> çökme).
        private static StaminaModel Collapsed(out int collapseCount)
        {
            var m = Create();
            int count = 0;
            m.Collapsed += () => count++;
            for (int i = 0; i < 300 && !m.IsCollapsed; i++) m.Tick(0.1f, StaminaActivity.Run);
            collapseCount = count;
            return m;
        }

        [Test]
        public void StartsFull()
        {
            var m = Create();
            Assert.AreEqual(100f, m.Current);
            Assert.AreEqual(100f, m.Ceiling);
            Assert.IsTrue(m.CanRun);
            Assert.IsFalse(m.IsCollapsed);
        }

        [Test]
        public void Run_DrainsFasterThanWalk()
        {
            var run = Create();
            var walk = Create();
            run.Restore(50f, 100f);
            walk.Restore(50f, 100f);

            // 0.5 sn < RegenDelay: yürüyen henüz dolmaz, koşan 6 kaybeder.
            run.Tick(0.5f, StaminaActivity.Run);
            walk.Tick(0.5f, StaminaActivity.Walk);

            Assert.AreEqual(44f, run.Current, Tol);
            Assert.AreEqual(50f, walk.Current, Tol);
        }

        [Test]
        public void Rest_NeverExceedsCeiling()
        {
            var m = Create();
            m.Restore(50f, 60f);
            TickFor(m, 30f, StaminaActivity.Idle);
            Assert.LessOrEqual(m.Current, m.Ceiling);
            Assert.AreEqual(m.Ceiling, m.Current, Tol);
            Assert.Less(m.Ceiling, 60f);
        }

        [Test]
        public void Regen_WaitsForDelayAfterRun()
        {
            var m = Create();
            m.Restore(50f, 100f);
            m.Tick(0.5f, StaminaActivity.Idle);
            Assert.AreEqual(50f, m.Current, Tol);

            // Gecikmenin bitişinden sonraki 0.5 sn dolar: 0.5 * 8.
            m.Tick(1f, StaminaActivity.Idle);
            Assert.AreEqual(54f, m.Current, Tol);
        }

        [Test]
        public void Run_ResetsRegenDelay()
        {
            var m = Create();
            m.Restore(50f, 100f);
            m.Tick(2f, StaminaActivity.Idle);
            float afterRegen = m.Current;
            m.Tick(0.1f, StaminaActivity.Run);
            float afterRun = m.Current;
            m.Tick(0.5f, StaminaActivity.Idle);
            Assert.AreEqual(afterRun, m.Current, Tol);
            Assert.Greater(afterRegen, afterRun);
        }

        [Test]
        public void Ceiling_DropsSlowlyWhenIdle_FastWhenRunning()
        {
            var idle = Create();
            var run = Create();
            TickFor(idle, 10f, StaminaActivity.Idle);
            TickFor(run, 5f, StaminaActivity.Run);

            Assert.AreEqual(99.5f, idle.Ceiling, Tol);
            Assert.AreEqual(98.5f, run.Ceiling, Tol);
        }

        [Test]
        public void Ceiling_NeverRisesOnItsOwn()
        {
            var m = Create();
            float last = m.Ceiling;
            for (int i = 0; i < 300; i++)
            {
                m.Tick(0.1f, i % 7 == 0 ? StaminaActivity.Run : StaminaActivity.Idle);
                Assert.LessOrEqual(m.Ceiling, last);
                last = m.Ceiling;
            }
        }

        [Test]
        public void FatigueMultiplier_ScalesCeilingDrain()
        {
            var normal = Create();
            var doubled = Create();
            for (int i = 0; i < 100; i++)
            {
                normal.Tick(0.1f, StaminaActivity.Idle);
                doubled.Tick(0.1f, StaminaActivity.Idle, 2f);
            }
            Assert.AreEqual(99.5f, normal.Ceiling, Tol);
            Assert.AreEqual(99f, doubled.Ceiling, Tol);
        }

        [Test]
        public void RestAtCamp_RefillsEverything()
        {
            var m = Collapsed(out _);
            m.RestAtCamp();
            Assert.AreEqual(100f, m.Current);
            Assert.AreEqual(100f, m.Ceiling);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(0f, m.CollapseRemaining);
            Assert.IsTrue(m.CanRun);
        }

        [Test]
        public void Collapse_FiresExactlyOnce_AndLocksRun()
        {
            var m = Collapsed(out int count);
            Assert.IsTrue(m.IsCollapsed);
            Assert.IsFalse(m.CanRun);
            Assert.AreEqual(0f, m.Current);
            Assert.AreEqual(4f, m.CollapseRemaining, Tol);

            // Çökme sürerken koşu girdisi yeni çökme tetiklemez.
            int extra = 0;
            m.Collapsed += () => extra++;
            TickFor(m, 3f, StaminaActivity.Run);
            Assert.AreEqual(1, count);
            Assert.AreEqual(0, extra);
            Assert.IsTrue(m.IsCollapsed);
            Assert.IsFalse(m.CanRun);
            Assert.AreEqual(0f, m.Current);
        }

        [Test]
        public void Collapse_AppliesCeilingPenalty()
        {
            var m = Collapsed(out _);
            // 8.4 sn koşuda 2.52 + 3 sn bitkinlikte (Idle hızı) ~0.15 tavan kaybı + 10 ceza.
            Assert.AreEqual(87.33f, m.Ceiling, 0.05f);
        }

        [Test]
        public void Collapse_PenaltyDoesNotGoBelowFloor()
        {
            var m = Create();
            m.Restore(20f, 26f);
            m.Tick(2f, StaminaActivity.Run);
            m.Tick(ExhaustSeconds, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(25f, m.Ceiling, Tol);
        }

        [Test]
        public void Collapse_Ends_WithFractionOfCeiling()
        {
            var m = Collapsed(out int count);
            float ceiling = m.Ceiling;
            TickFor(m, 4.2f, StaminaActivity.Idle);

            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(1, count);
            // Çökme sırasında da tavan idle hızında düşer; oran güncel tavana uygulanır.
            Assert.AreEqual(m.Ceiling * 0.5f, m.Current, 0.01f); // uyanış sonrası artık kareler tavanı ~0.005 düşürür
            Assert.Less(m.Ceiling, ceiling + Tol);
        }

        [Test]
        public void RunHysteresis_NoFlicker_UntilThreshold()
        {
            // Tavan tabanda (25): uyanınca 12.5 < 20 eşiği, histerezis devrede.
            var m = Create();
            m.Restore(20f, 25f);
            m.Tick(2f, StaminaActivity.Run);
            m.Tick(ExhaustSeconds, StaminaActivity.Idle);
            TickFor(m, 4.2f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(12.5f, m.Current, Tol);
            Assert.IsFalse(m.CanRun);

            // Eşiğe kadar CanRun kapalı kalır; koşu istenirse yürüme sayılır (tüketim yok).
            bool wasFalseBelowThreshold = true;
            bool turnedOn = false;
            for (int i = 0; i < 300 && !turnedOn; i++)
            {
                float before = m.Current;
                m.Tick(0.1f, StaminaActivity.Run);
                Assert.GreaterOrEqual(m.Current, before - Tol);
                if (m.CanRun) turnedOn = true;
                else if (m.Current >= 20f) wasFalseBelowThreshold = false;
            }
            Assert.IsTrue(turnedOn);
            Assert.IsTrue(wasFalseBelowThreshold);
            Assert.GreaterOrEqual(m.Current, 20f);

            // Açıldıktan sonra eşiğin altına inse bile 0'a kadar açık kalır.
            m.Tick(0.1f, StaminaActivity.Run);
            Assert.IsTrue(m.CanRun);
        }

        [Test]
        public void SpeedMultiplier_SlowsOnlyBelowThreshold()
        {
            var m = Create();
            Assert.AreEqual(1f, m.SpeedMultiplier);

            m.Restore(10f, 31f);
            Assert.AreEqual(1f, m.SpeedMultiplier);

            m.Restore(10f, 29f);
            Assert.AreEqual(0.85f, m.SpeedMultiplier, Tol);
        }

        [Test]
        public void SpeedMultiplier_IsNotZeroWhileCollapsed()
        {
            var m = Collapsed(out _);
            Assert.Greater(m.SpeedMultiplier, 0f);
        }

        [Test]
        public void Tick_NonPositiveOrNaNDt_HasNoEffect()
        {
            var m = Create();
            m.Restore(50f, 80f);
            m.Tick(0f, StaminaActivity.Run);
            m.Tick(-1f, StaminaActivity.Run);
            m.Tick(float.NaN, StaminaActivity.Run);
            Assert.AreEqual(50f, m.Current);
            Assert.AreEqual(80f, m.Ceiling);
        }

        [Test]
        public void Restore_ClampsValues()
        {
            var m = Create();
            m.Restore(500f, 500f);
            Assert.AreEqual(100f, m.Ceiling);
            Assert.AreEqual(100f, m.Current);

            m.Restore(-5f, 50f);
            Assert.AreEqual(0f, m.Current);

            m.Restore(80f, 10f);
            Assert.AreEqual(25f, m.Ceiling);
            Assert.AreEqual(25f, m.Current);
        }

        [Test]
        public void Current_AlwaysWithinZeroAndCeiling()
        {
            var m = Create();
            m.Restore(100f, 100f);
            // Büyük fatigue çarpanı tavanı hızla düşürür; Current onunla birlikte kısılmalı.
            for (int i = 0; i < 200; i++)
            {
                m.Tick(0.1f, StaminaActivity.Idle, 500f);
                Assert.LessOrEqual(m.Current, m.Ceiling + Tol);
                Assert.GreaterOrEqual(m.Current, 0f);
            }
            Assert.AreEqual(25f, m.Ceiling, Tol);
        }

        // --- Bitkinlik (exhausted) ---

        [Test]
        public void ReachingZero_StartsExhaustion_NotCollapse()
        {
            var m = Exhausted(out _);
            Assert.IsTrue(m.IsExhausted);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(0f, m.Current);
            Assert.AreEqual(3f, m.ExhaustionRemaining, Tol);
            Assert.IsFalse(m.CanRun);
        }

        [Test]
        public void Exhausted_FiresExactlyOnce()
        {
            var m = Exhausted(out int count);
            TickFor(m, 2f, StaminaActivity.Run);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void WhileExhausted_RunDoesNotDrain_AndNoRegen()
        {
            var m = Exhausted(out _);
            float ceiling = m.Ceiling;
            TickFor(m, 2f, StaminaActivity.Run);
            TickFor(m, 0.5f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsExhausted);
            Assert.AreEqual(0f, m.Current);
            // Tavan Idle hızında düşer (Run hızı 0.3/sn olsaydı 0.75 düşerdi).
            Assert.AreEqual(ceiling - 0.05f * 2.5f, m.Ceiling, 0.01f);
        }

        [Test]
        public void ExhaustionEnd_CollapsesOnce_WithPenaltyOnce()
        {
            var m = Exhausted(out _);
            int collapses = 0;
            m.Collapsed += () => collapses++;
            float ceilingBefore = m.Ceiling;

            TickFor(m, 3.2f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsExhausted);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(1, collapses);
            // Ceza bir kez: 10 + ~0.15 idle kaybı; ikinci ceza olsaydı ~20 düşerdi.
            Assert.AreEqual(ceilingBefore - 10f, m.Ceiling, 0.3f);

            TickFor(m, 1f, StaminaActivity.Run);
            Assert.AreEqual(1, collapses);
        }

        [Test]
        public void ExactGraceDuration_TriggersCollapse()
        {
            var m = Exhausted(out _);
            int collapses = 0;
            m.Collapsed += () => collapses++;
            // 2.5 + 0.5 ikili tabanda tam: float birikim hatası testi flaky yapmasın.
            m.Tick(2.5f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsCollapsed);
            m.Tick(0.5f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(1, collapses);
        }

        [Test]
        public void WakingUp_RestoresHalfOfCeiling_AndEnablesRun()
        {
            var m = Collapsed(out _);
            TickFor(m, 4.2f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(m.Ceiling * 0.5f, m.Current, 0.01f); // uyanış sonrası artık kareler tavanı ~0.005 düşürür
            Assert.GreaterOrEqual(m.Current, 20f);
            Assert.IsTrue(m.CanRun);
        }

        [Test]
        public void WhileExhausted_SpeedIsFatigueTimesExhaustedMultiplier()
        {
            var m = Exhausted(out _);
            Assert.AreEqual(0.7f, m.SpeedMultiplier, Tol);

            // Tavan eşiğin altındaysa iki çarpan birlikte uygulanır: 0.85 * 0.7.
            var low = Create();
            low.Restore(25f, 29f); // CanRun açık olsun diye eşiğin (20) üstünde
            low.Tick(3f, StaminaActivity.Run);
            Assert.IsTrue(low.IsExhausted);
            Assert.AreEqual(0.85f * 0.7f, low.SpeedMultiplier, Tol);
        }

        [Test]
        public void RestAtCamp_ClearsExhaustion()
        {
            var m = Exhausted(out _);
            m.RestAtCamp();
            Assert.IsFalse(m.IsExhausted);
            Assert.AreEqual(0f, m.ExhaustionRemaining);
            Assert.AreEqual(100f, m.Current);
            Assert.IsTrue(m.CanRun);
        }

        [Test]
        public void Restore_ClearsExhaustion_ButZeroCurrentReStartsItOnNextTick()
        {
            var m = Exhausted(out _);
            int count = 0;
            m.Exhausted += () => count++;
            m.Restore(0f, 80f);
            Assert.IsFalse(m.IsExhausted);
            m.Tick(0.1f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsExhausted);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void HugeDt_RunningToZero_ThenExhaustionEnd_IsConsistent()
        {
            var m = Create();
            int collapses = 0;
            m.Collapsed += () => collapses++;

            // Aşan dt bitkinliğe aktarılmaz: bitkinlik tam süresiyle başlar.
            m.Tick(10f, StaminaActivity.Run);
            Assert.IsTrue(m.IsExhausted);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(3f, m.ExhaustionRemaining, Tol);

            // Aşan dt çökmeye de aktarılmaz: çökme tam süresiyle başlar, olay tek sefer.
            m.Tick(10f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(1, collapses);
            Assert.AreEqual(4f, m.CollapseRemaining, Tol);
        }

        [Test]
        public void WhileExhausted_NonPositiveOrNaNDt_HasNoEffect()
        {
            var m = Exhausted(out _);
            m.Tick(0f, StaminaActivity.Run);
            m.Tick(-1f, StaminaActivity.Run);
            m.Tick(float.NaN, StaminaActivity.Run);
            Assert.IsTrue(m.IsExhausted);
            Assert.AreEqual(3f, m.ExhaustionRemaining, Tol);
        }
    }
}
