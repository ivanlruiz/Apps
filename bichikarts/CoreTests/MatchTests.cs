using System.Linq;
using Bichikarts.Core;
using NUnit.Framework;

namespace Bichikarts.Tests
{
    public class MatchTests
    {
        private static Match NewMatch(string level = "cocina", float hold = 15f, int rounds = 2)
        {
            return new Match(new MatchConfig { LevelId = level, HoldTime = hold, RoundsToWin = rounds });
        }

        private static void SkipCountdown(Match m)
        {
            while (m.State == MatchState.Countdown) m.Update(Tuning.Step, KartInput.None, KartInput.None);
        }

        private static void Tick(Match m, float seconds, KartInput a = default, KartInput b = default)
        {
            for (int i = 0; i < (int)(seconds / Tuning.Step); i++) m.Update(Tuning.Step, a, b);
        }

        private static void Place(Kart k, float x, float y)
        {
            k.X = x; k.Y = y; k.VX = 0; k.VY = 0;
        }

        [Test]
        public void CountdownBlocksInputThenStartsPlaying()
        {
            var m = NewMatch();
            float x = m.Karts[0].X;
            Tick(m, 1f, new KartInput { X = 1 });
            Assert.That(m.Karts[0].X, Is.EqualTo(x).Within(0.01f));
            SkipCountdown(m);
            Assert.That(m.State, Is.EqualTo(MatchState.Playing));
            Assert.That(m.Events.Count(e => e.Type == GameEventType.Countdown), Is.EqualTo(3));
            Assert.That(m.Events.Any(e => e.Type == GameEventType.Go), Is.True);
        }

        [Test]
        public void TouchingTheFoodPicksItUpAndSlowsTheCarrier()
        {
            var m = NewMatch();
            SkipCountdown(m);
            Place(m.Karts[0], m.Level.FoodX, 700 - Tuning.Half);
            Tick(m, 0.05f);
            Assert.That(m.Food.Carrier, Is.EqualTo(0));
            Assert.That(m.SpeedMultiplier(0), Is.LessThan(1f));
            Assert.That(m.SpeedMultiplier(1), Is.GreaterThan(1f));
        }

        [Test]
        public void HoldingLongEnoughWinsTheRound()
        {
            var m = NewMatch(hold: 2f);
            SkipCountdown(m);
            Place(m.Karts[0], m.Level.FoodX, 700 - Tuning.Half);
            Tick(m, 2.2f);
            Assert.That(m.Wins[0], Is.EqualTo(1));
            Assert.That(m.State, Is.EqualTo(MatchState.RoundOver));
            Assert.That(m.Events.Count(e => e.Type == GameEventType.Tick), Is.EqualTo(2));

            Tick(m, Tuning.RoundOverTime + 0.1f);
            Assert.That(m.Round, Is.EqualTo(2));
            Assert.That(m.State, Is.EqualTo(MatchState.Countdown));
            Assert.That(m.Food.Carrier, Is.EqualTo(-1));
        }

        [Test]
        public void CrashingIntoTheCarrierStealsAndResetsTheTimer()
        {
            var m = NewMatch();
            SkipCountdown(m);
            var a = m.Karts[0];
            var b = m.Karts[1];
            Place(a, 400, 850 - Tuning.Half);
            m.Food.Carrier = 0;
            Tick(m, 1f);
            Assert.That(m.Hold[0], Is.GreaterThan(0.9f));

            Place(b, 400 + Tuning.KartRadius * 2 - 5, 850 - Tuning.Half);
            Tick(m, Tuning.Step);
            Assert.That(m.Food.Carrier, Is.EqualTo(1));
            Assert.That(m.Hold[0], Is.EqualTo(0f));
            Assert.That(m.Hold[1], Is.LessThan(0.05f));
            Assert.That(a.Stun, Is.GreaterThan(0f));
            Assert.That(b.Shield, Is.GreaterThan(0f));
            Assert.That(m.Stats[1].Steals, Is.EqualTo(1));
        }

        [Test]
        public void ShieldPreventsAnInstantStealBack()
        {
            var m = NewMatch();
            SkipCountdown(m);
            var a = m.Karts[0];
            var b = m.Karts[1];
            Place(a, 400, 850 - Tuning.Half);
            m.Food.Carrier = 0;
            Place(b, 440, 850 - Tuning.Half);
            Tick(m, Tuning.Step);
            Assert.That(m.Food.Carrier, Is.EqualTo(1));

            Place(a, 600, 850 - Tuning.Half);
            Place(b, 640, 850 - Tuning.Half);
            a.Stun = 0f;
            Tick(m, Tuning.Step);
            Assert.That(m.Food.Carrier, Is.EqualTo(1), "el ladrón está protegido un rato");
        }

        [Test]
        public void WinningEnoughRoundsEndsTheMatch()
        {
            var m = NewMatch(hold: 1f, rounds: 2);
            for (int r = 0; r < 2; r++)
            {
                SkipCountdown(m);
                Place(m.Karts[1], m.Level.FoodX, 700 - Tuning.Half);
                Tick(m, 1.2f);
                if (r == 0) Tick(m, Tuning.RoundOverTime + 0.1f);
            }
            Assert.That(m.State, Is.EqualTo(MatchState.MatchOver));
            Assert.That(m.Winner, Is.EqualTo(1));
            Tick(m, Tuning.MatchOverTime + 0.1f);
            Assert.That(m.Finished, Is.True);
        }
    }
}
