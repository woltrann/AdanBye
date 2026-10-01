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

        // Tam dolu modeli koşturarak çökmeye getirir.
        private static StaminaModel Collapsed(out int collapseCount)
        {
            var m = Create();
            int count = 0;
            m.Collapsed += () => count++;
            for (int i = 0; i < 200 && !m.IsCollapsed; i++) m.Tick(0.1f, StaminaActivity.Run);
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
            // ~8.3 sn koşuda ~2.5 tavan kaybı + 10 ceza.
            Assert.AreEqual(87.5f, m.Ceiling, 0.1f);
        }

        [Test]
        public void Collapse_PenaltyDoesNotGoBelowFloor()
        {
            var m = Create();
            m.Restore(20f, 26f);
            m.Tick(2f, StaminaActivity.Run);
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
            Assert.AreEqual(m.Ceiling * 0.15f, m.Current, Tol);
            Assert.Less(m.Ceiling, ceiling + Tol);
        }

        [Test]
        public void RunHysteresis_NoFlicker_UntilThreshold()
        {
            var m = Collapsed(out _);
            TickFor(m, 4f, StaminaActivity.Idle);
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
    }
}
