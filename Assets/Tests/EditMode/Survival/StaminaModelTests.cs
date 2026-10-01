using NUnit.Framework;

namespace AdanBye.Survival.Tests
{
    public class StaminaModelTests
    {
        private const float Tol = 0.001f;

        private static StaminaModel Create() => new StaminaModel(new StaminaConfig());

        private static void TickFor(StaminaModel m, float seconds, StaminaActivity a, float step = 0.1f)
        {
            int n = (int)(seconds / step + 0.5f);
            for (int i = 0; i < n; i++) m.Tick(step, a);
        }

        // Koşu staminasını 0'a indirir (ana stamina hâlâ yüksek). Geçen süreyi döndürür.
        private static float RunUntilRunStaminaZero(StaminaModel m)
        {
            float t = 0f;
            for (int i = 0; i < 400 && m.Current > 0f; i++) { m.Tick(0.1f, StaminaActivity.Run); t += 0.1f; }
            return t;
        }

        // Ana stamina 0'a inince bitkinlik başlar (çökme henüz yok). Küçük Ceiling ile hızlı ulaşılır (0.2 / 0.05 = 4 sn).
        private static StaminaModel Exhausted(out int exhaustedCount, StaminaActivity a = StaminaActivity.Idle)
        {
            var m = Create();
            int count = 0;
            m.Exhausted += () => count++;
            m.Restore(50f, 0.2f);
            for (int i = 0; i < 200 && !m.IsExhausted; i++) m.Tick(0.1f, a);
            exhaustedCount = count;
            return m;
        }

        // Bitkinlikten sonra çökmeye getirir (ana stamina 0 -> bitkin 3 sn -> çökme).
        private static StaminaModel Collapsed(out int collapseCount, StaminaActivity a = StaminaActivity.Idle)
        {
            var m = Create();
            int count = 0;
            m.Collapsed += () => count++;
            m.Restore(50f, 0.2f);
            for (int i = 0; i < 300 && !m.IsCollapsed; i++) m.Tick(0.1f, a);
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

        // --- Koşu staminası (Current) 0'a inince: yalnızca koşu kapanır ---

        [Test]
        public void RunStaminaZero_OnlyLocksRun_NoExhaustionNoCollapse()
        {
            var m = Create();
            int events = 0;
            m.Exhausted += () => events++;
            m.Collapsed += () => events++;

            RunUntilRunStaminaZero(m);

            Assert.AreEqual(0f, m.Current, Tol);
            Assert.IsFalse(m.CanRun);
            Assert.IsFalse(m.IsExhausted);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(0, events);
            // Yavaşlama yok: ana stamina yüksek, hız çarpanı 1.
            Assert.AreEqual(1f, m.SpeedMultiplier);
        }

        [Test]
        public void RunStaminaZero_WalkContinues_RegensAndHysteresisReenablesRun()
        {
            var m = Create();
            RunUntilRunStaminaZero(m);

            // Koşu istenirse yürüme sayılır: tüketim yok, dolum var; eşik (20) altında CanRun kapalı kalır.
            bool turnedOn = false;
            for (int i = 0; i < 300 && !turnedOn; i++)
            {
                float before = m.Current;
                m.Tick(0.1f, StaminaActivity.Run);
                Assert.GreaterOrEqual(m.Current, before - Tol);
                Assert.IsFalse(m.IsExhausted);
                if (m.CanRun) turnedOn = true;
                else Assert.Less(m.Current, 20f);
            }
            Assert.IsTrue(turnedOn);
            Assert.GreaterOrEqual(m.Current, 20f);

            // Açıldıktan sonra eşiğin altına inse bile 0'a kadar açık kalır.
            m.Tick(0.1f, StaminaActivity.Run);
            Assert.IsTrue(m.CanRun);
        }

        [Test]
        public void RunStaminaDepleting_DrainsMainStaminaOnlyAtRunRate_NeverCollapsesAlone()
        {
            var m = Create();
            float elapsed = RunUntilRunStaminaZero(m);
            // 100 -> 0 koşu staminası ~8.3 sn: ana stamina yalnızca 0.3/sn gider.
            Assert.AreEqual(100f - 0.3f * elapsed, m.Ceiling, 0.05f);
            Assert.Greater(m.Ceiling, 95f);

            // 5 dk boyunca sürekli koşu isteği: ana stamina sıfıra ulaşmadıkça bitkinlik/bayılma olmaz.
            for (int i = 0; i < 3000; i++)
            {
                m.Tick(0.1f, StaminaActivity.Run);
                Assert.IsFalse(m.IsExhausted);
                Assert.IsFalse(m.IsCollapsed);
                Assert.Greater(m.Ceiling, 0f);
            }
        }

        // --- Ana stamina (Ceiling) ---

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
            var acts = new[] { StaminaActivity.Run, StaminaActivity.Walk, StaminaActivity.Idle };
            for (int i = 0; i < 300; i++)
            {
                m.Tick(0.1f, acts[i % 7 % 3]);
                Assert.LessOrEqual(m.Ceiling, last);
                last = m.Ceiling;
            }
        }

        [Test]
        public void Ceiling_StaysZero_WhileExhaustedAndCollapsed()
        {
            // Uyanıştaki artış kasıtlı (zorunlu uyku) istisnadır; ayrıca test edilir. Bitkin/çökme boyunca kendiliğinden artış yok.
            var m = Exhausted(out _);
            TickFor(m, 2.9f, StaminaActivity.Idle);
            Assert.AreEqual(0f, m.Ceiling);
            m.Tick(0.2f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            TickFor(m, 3.5f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(0f, m.Ceiling);
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
        public void Walk_And_Idle_BothDrainMainStamina()
        {
            var walk = Create();
            var idle = Create();
            TickFor(walk, 20f, StaminaActivity.Walk);
            TickFor(idle, 20f, StaminaActivity.Idle);
            Assert.AreEqual(99f, walk.Ceiling, Tol);
            Assert.AreEqual(99f, idle.Ceiling, Tol);
        }

        [Test]
        public void MainStaminaZero_StartsExhaustion_EvenWhileWalking()
        {
            var m = Exhausted(out int count, StaminaActivity.Walk);
            Assert.IsTrue(m.IsExhausted);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(0f, m.Ceiling);
            Assert.AreEqual(0f, m.Current);
            Assert.AreEqual(3f, m.ExhaustionRemaining, Tol);
            Assert.IsFalse(m.CanRun);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void MainStaminaZero_WhileWalking_CollapsesAfterGrace()
        {
            var m = Collapsed(out int count, StaminaActivity.Walk);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Current_NeverExceedsCeiling_WhileCeilingFalls()
        {
            var m = Create();
            m.Restore(100f, 100f);
            // Büyük fatigue çarpanı ana staminayı hızla düşürür; koşu staminası onunla birlikte kısılmalı.
            // Döngü bitkinlik, çökme ve uyanışı da kapsar.
            for (int i = 0; i < 300; i++)
            {
                m.Tick(0.1f, StaminaActivity.Idle, 500f);
                Assert.LessOrEqual(m.Current, m.Ceiling + Tol);
                Assert.GreaterOrEqual(m.Current, 0f);
            }
        }

        // --- Kamp ---

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
        public void RestAtCamp_ClearsExhaustion()
        {
            var m = Exhausted(out _);
            m.RestAtCamp();
            Assert.IsFalse(m.IsExhausted);
            Assert.AreEqual(0f, m.ExhaustionRemaining);
            Assert.AreEqual(100f, m.Current);
            Assert.AreEqual(100f, m.Ceiling);
            Assert.IsTrue(m.CanRun);
        }

        // --- Bitkinlik ve bayılma ---

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
            TickFor(m, 2f, StaminaActivity.Run);
            TickFor(m, 0.5f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsExhausted);
            Assert.AreEqual(0f, m.Current);
            Assert.AreEqual(0f, m.Ceiling);
        }

        [Test]
        public void ExhaustionEnd_CollapsesOnce_WithoutAnyPenalty()
        {
            var m = Exhausted(out _);
            int collapses = 0;
            m.Collapsed += () => collapses++;

            TickFor(m, 3.2f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsExhausted);
            Assert.IsTrue(m.IsCollapsed);
            Assert.AreEqual(1, collapses);
            // Ceza yok: ana stamina zaten 0, çökmeyle ayrıca değişmez.
            Assert.AreEqual(0f, m.Ceiling);

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
        public void ZeroGrace_CollapsesImmediately_FiringBothEventsOnce()
        {
            var m = new StaminaModel(new StaminaConfig { ExhaustionGraceSeconds = 0f });
            int exhausted = 0, collapsed = 0;
            m.Exhausted += () => exhausted++;
            m.Collapsed += () => collapsed++;
            m.Restore(10f, 0.01f);
            m.Tick(1f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsCollapsed);
            Assert.IsFalse(m.IsExhausted);
            Assert.AreEqual(1, exhausted);
            Assert.AreEqual(1, collapsed);
        }

        [Test]
        public void WakingUp_SetsMainStaminaToFraction_AndRunStaminaEqualsIt()
        {
            var m = Collapsed(out _);
            TickFor(m, 4.2f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsCollapsed);
            // Uyanış sonrası artık kareler ana stamina'yı idle hızında az düşürür (tolerans bunun için).
            Assert.AreEqual(25f, m.Ceiling, 0.05f);
            Assert.LessOrEqual(m.Current, m.Ceiling);
            Assert.IsTrue(m.CanRun); // ~25 >= 20 eşiği
        }

        [Test]
        public void WakingUp_UsesConfiguredFraction()
        {
            var m = new StaminaModel(new StaminaConfig { WakeMainStaminaFraction = 0.5f });
            m.Restore(10f, 0.2f);
            for (int i = 0; i < 300 && !m.IsCollapsed; i++) m.Tick(0.1f, StaminaActivity.Idle);
            m.Tick(4f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsCollapsed);
            Assert.AreEqual(50f, m.Ceiling, Tol);
            Assert.AreEqual(50f, m.Current, Tol);
        }

        [Test]
        public void SpeedMultiplier_SlowsOnlyWhenMainStaminaBelowThreshold()
        {
            var m = Create();
            Assert.AreEqual(1f, m.SpeedMultiplier);

            m.Restore(10f, 31f);
            Assert.AreEqual(1f, m.SpeedMultiplier);

            m.Restore(10f, 29f);
            Assert.AreEqual(0.85f, m.SpeedMultiplier, Tol);
        }

        [Test]
        public void WhileExhausted_SpeedIsFatigueTimesExhaustedMultiplier()
        {
            // Bitkinlik ana stamina 0 demektir: yorgunluk (0.85) ve bitkin (0.7) çarpanı birlikte uygulanır.
            var m = Exhausted(out _);
            Assert.AreEqual(0.85f * 0.7f, m.SpeedMultiplier, Tol);
        }

        [Test]
        public void SpeedMultiplier_IsNotZeroWhileCollapsed()
        {
            var m = Collapsed(out _);
            Assert.Greater(m.SpeedMultiplier, 0f);
        }

        [Test]
        public void FatigueMultiplier_ScalesTimeToExhaustion()
        {
            var normal = Create();
            var doubled = Create();
            normal.Restore(10f, 1f);
            doubled.Restore(10f, 1f);
            // 1 / 0.05 = 20 sn; çarpan 2 ile 10 sn. 12. saniyede yalnızca çarpanlı olan bitkin olmalı.
            for (int i = 0; i < 120; i++)
            {
                normal.Tick(0.1f, StaminaActivity.Idle);
                doubled.Tick(0.1f, StaminaActivity.Idle, 2f);
            }
            Assert.IsFalse(normal.IsExhausted);
            Assert.IsTrue(doubled.IsExhausted);
        }

        // --- Giriş doğrulama / Restore / büyük dt ---

        [Test]
        public void Tick_NonPositiveOrNaNDt_HasNoEffect()
        {
            var m = Create();
            m.Restore(50f, 80f);
            m.Tick(0f, StaminaActivity.Run);
            m.Tick(-1f, StaminaActivity.Run);
            m.Tick(float.NaN, StaminaActivity.Run);
            m.Tick(float.PositiveInfinity, StaminaActivity.Run);
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

            // Taban yok: ana stamina 0'a kadar inebilir.
            m.Restore(80f, 10f);
            Assert.AreEqual(10f, m.Ceiling);
            Assert.AreEqual(10f, m.Current);

            m.Restore(80f, -3f);
            Assert.AreEqual(0f, m.Ceiling);
            Assert.AreEqual(0f, m.Current);
        }

        [Test]
        public void Restore_ClearsExhaustion_ButZeroCeilingReStartsItOnNextTick()
        {
            var m = Exhausted(out _);
            int count = 0;
            m.Exhausted += () => count++;
            m.Restore(0f, 0f);
            Assert.IsFalse(m.IsExhausted);
            m.Tick(0.1f, StaminaActivity.Idle);
            Assert.IsTrue(m.IsExhausted);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Restore_ZeroRunStaminaButMainStaminaLeft_DoesNotStartExhaustion()
        {
            var m = Create();
            m.Restore(0f, 80f);
            m.Tick(0.1f, StaminaActivity.Idle);
            Assert.IsFalse(m.IsExhausted);
            Assert.IsFalse(m.CanRun);
        }

        [Test]
        public void HugeDt_MainStaminaToZero_ThenExhaustionEnd_IsConsistent()
        {
            var m = Create();
            int collapses = 0;
            m.Collapsed += () => collapses++;
            m.Restore(10f, 10f);

            // Aşan dt bitkinliğe aktarılmaz: bitkinlik tam süresiyle başlar.
            m.Tick(10000f, StaminaActivity.Idle);
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
        public void HugeDt_RunStaminaToZero_DoesNotExhaust()
        {
            var m = Create();
            m.Tick(10f, StaminaActivity.Run);
            Assert.AreEqual(0f, m.Current);
            Assert.IsFalse(m.IsExhausted);
            Assert.IsFalse(m.CanRun);
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
