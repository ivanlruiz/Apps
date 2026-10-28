using System.Collections.Generic;
using System.Linq;
using Bichikarts.Core;
using NUnit.Framework;

namespace Bichikarts.Tests
{
    /// <summary>
    /// Prueban que la compu sabe jugar en todas las salas: llega a la comida, roba y escapa.
    /// También sirven para detectar salas donde la comida quedó inalcanzable.
    /// </summary>
    public class CpuTests
    {
        public static IEnumerable<string> LevelIds => Levels.All.Select(l => l.Id);

        private static float PlayUntil(Match m, CpuBrain a, CpuBrain b, float limit, System.Func<bool> done)
        {
            float t = 0f;
            while (t < limit && !done())
            {
                var ia = a != null ? a.Step(Tuning.Step) : KartInput.None;
                var ib = b != null ? b.Step(Tuning.Step) : KartInput.None;
                m.Update(Tuning.Step, ia, ib);
                t += Tuning.Step;
            }
            return t;
        }

        private static void SkipCountdown(Match m)
        {
            while (m.State == MatchState.Countdown) m.Update(Tuning.Step, KartInput.None, KartInput.None);
        }

        [TestCaseSource(nameof(LevelIds))]
        public void CpuReachesTheFoodFromEitherSide(string id)
        {
            for (int side = 0; side < 2; side++)
            {
                var m = new Match(new MatchConfig { LevelId = id });
                var cpu = new CpuBrain(m, side, CpuDifficulty.Hard, seed: 11 + side);
                SkipCountdown(m);
                float t = side == 0
                    ? PlayUntil(m, cpu, null, 15f, () => m.Food.Carrier >= 0)
                    : PlayUntil(m, null, cpu, 15f, () => m.Food.Carrier >= 0);
                Assert.That(m.Food.Carrier, Is.EqualTo(side), $"{id}: la compu del lado {side} no llegó a la comida");
                TestContext.WriteLine($"{id} lado {side}: comida en {t:F1}s");
            }
        }

        [TestCaseSource(nameof(LevelIds))]
        public void CpuStealsFromAPlayerWhoStandsStill(string id)
        {
            var m = new Match(new MatchConfig { LevelId = id, HoldTime = 60f });
            var cpu = new CpuBrain(m, 1, CpuDifficulty.Normal, seed: 5);
            SkipCountdown(m);
            m.Food.Carrier = 0;
            float t = PlayUntil(m, null, cpu, 20f, () => m.Food.Carrier == 1);
            Assert.That(m.Food.Carrier, Is.EqualTo(1), $"{id}: la compu no pudo robar");
            TestContext.WriteLine($"{id}: robo en {t:F1}s");
        }

        [TestCaseSource(nameof(LevelIds))]
        public void CpuVersusCpuFinishesAMatch(string id)
        {
            var m = new Match(new MatchConfig { LevelId = id, HoldTime = 10f, RoundsToWin = 2 });
            var a = new CpuBrain(m, 0, CpuDifficulty.Normal, seed: 21);
            var b = new CpuBrain(m, 1, CpuDifficulty.Normal, seed: 37);
            float t = PlayUntil(m, a, b, 600f, () => m.Finished);
            Assert.That(m.Finished, Is.True, $"{id}: la partida no terminó");
            Assert.That(m.Winner, Is.InRange(0, 1));
            foreach (var k in m.Karts) Assert.That(KartPhysics.Overlapping(k, m.Level.Solids), Is.False);
            int possessions = m.Stats.Sum(st => st.Steals + st.Pickups);
            TestContext.WriteLine($"{id}: posesión promedio {t / possessions:F1}s, partida en {t:F0}s, robos {m.Stats[0].Steals}+{m.Stats[1].Steals}, tuberías {m.Stats[0].Pipes}+{m.Stats[1].Pipes}, gravedad {m.Stats[0].Flips}+{m.Stats[1].Flips}");
        }

        [Test]
        public void CarrierCpuKeepsTheFoodLongerThanAnIdleCarrier()
        {
            // Una compu con la comida debería escapar un rato antes de que la alcance la otra.
            float total = 0f;
            int runs = 0;
            foreach (var id in LevelIds)
            {
                var m = new Match(new MatchConfig { LevelId = id, HoldTime = 60f });
                var runner = new CpuBrain(m, 0, CpuDifficulty.Hard, seed: 3);
                var chaser = new CpuBrain(m, 1, CpuDifficulty.Normal, seed: 4);
                SkipCountdown(m);
                m.Food.Carrier = 0;
                total += PlayUntil(m, runner, chaser, 30f, () => m.Food.Carrier == 1);
                runs++;
            }
            TestContext.WriteLine($"aguante promedio escapando: {total / runs:F1}s");
            Assert.That(total / runs, Is.GreaterThan(5f));
        }
    }
}
