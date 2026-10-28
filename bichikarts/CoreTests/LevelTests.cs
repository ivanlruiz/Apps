using System.Collections.Generic;
using System.Linq;
using Bichikarts.Core;
using NUnit.Framework;

namespace Bichikarts.Tests
{
    public class LevelTests
    {
        public static IEnumerable<string> LevelIds => Levels.All.Select(l => l.Id);

        [Test]
        public void LevelIdsAreUnique()
        {
            Assert.That(LevelIds.Distinct().Count(), Is.EqualTo(Levels.All.Count));
        }

        [TestCaseSource(nameof(LevelIds))]
        public void SpawnsAreFreeAndOnTheFloor(string id)
        {
            var level = Levels.Get(id);
            for (int i = 0; i < 2; i++)
            {
                var k = new Kart(i, level.SpawnX[i], level.SpawnY[i]);
                Assert.That(KartPhysics.Overlapping(k, level.Solids), Is.False, $"spawn {i}");
                Assert.That(KartPhysics.Touching(k, Dir.Down, level.Solids), Is.True, $"spawn {i}");
            }
        }

        [TestCaseSource(nameof(LevelIds))]
        public void FoodIsNotInsideABlock(string id)
        {
            var level = Levels.Get(id);
            var probe = new Kart(0, level.FoodX, level.FoodY);
            foreach (var s in level.Solids)
            {
                bool inside = level.FoodX > s.X && level.FoodX < s.Right && level.FoodY > s.Y && level.FoodY < s.Bottom;
                Assert.That(inside, Is.False, s.Kind);
            }
            Assert.That(probe.X, Is.InRange(50f, 1550f));
        }

        [TestCaseSource(nameof(LevelIds))]
        public void EveryPipeMouthSitsOnASurfaceAndHasRoomToExit(string id)
        {
            var level = Levels.Get(id);
            foreach (var p in level.Pipes)
            {
                Assert.That(p.Path[0], Is.EqualTo(p.Ends[0].X));
                Assert.That(p.Path[1], Is.EqualTo(p.Ends[0].Y));
                Assert.That(p.Path[p.Path.Length - 2], Is.EqualTo(p.Ends[1].X));
                Assert.That(p.Path[p.Path.Length - 1], Is.EqualTo(p.Ends[1].Y));

                foreach (var m in p.Ends)
                {
                    float cx = m.X + Dir.X[m.Normal] * (Tuning.Half + 0.5f);
                    float cy = m.Y + Dir.Y[m.Normal] * (Tuning.Half + 0.5f);
                    var k = new Kart(0, cx, cy);
                    Assert.That(KartPhysics.Overlapping(k, level.Solids), Is.False, $"{id} tubería {p.Id} boca ({m.X},{m.Y})");

                    // Apoyado sobre la boca con la gravedad hacia la superficie.
                    k.X = m.X + Dir.X[m.Normal] * Tuning.Half;
                    k.Y = m.Y + Dir.Y[m.Normal] * Tuning.Half;
                    Assert.That(KartPhysics.Touching(k, Dir.Opposite(m.Normal), level.Solids), Is.True,
                        $"{id} tubería {p.Id} boca ({m.X},{m.Y}) no está sobre una superficie");
                }
            }
        }

        [TestCaseSource(nameof(LevelIds))]
        public void EveryPipeWorksInBothDirections(string id)
        {
            var level = Levels.Get(id);
            foreach (var p in level.Pipes)
            {
                for (int e = 0; e < 2; e++)
                {
                    var m = p.Ends[e];
                    var exit = p.Ends[1 - e];
                    var k = new Kart(0, m.X + Dir.X[m.Normal] * Tuning.Half, m.Y + Dir.Y[m.Normal] * Tuning.Half)
                    {
                        Gravity = Dir.Opposite(m.Normal),
                    };
                    var into = new KartInput { X = -Dir.X[m.Normal], Y = -Dir.Y[m.Normal] };
                    KartPhysics.Step(k, KartInput.None, Tuning.Step, level, 1f, null);
                    for (int i = 0; i < 10 && !k.InPipe; i++)
                        KartPhysics.Step(k, into, Tuning.Step, level, 1f, null);
                    Assert.That(k.InPipe, Is.True, $"{id} tubería {p.Id} boca {e}");
                    for (int i = 0; i < 600 && k.InPipe; i++)
                        KartPhysics.Step(k, KartInput.None, Tuning.Step, level, 1f, null);
                    Assert.That(k.InPipe, Is.False);
                    Assert.That(k.Gravity, Is.EqualTo(Dir.Opposite(exit.Normal)));
                    Assert.That(KartPhysics.Overlapping(k, level.Solids), Is.False);
                }
            }
        }
    }
}
