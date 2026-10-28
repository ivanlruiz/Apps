using Bichikarts.Core;
using NUnit.Framework;

namespace Bichikarts.Tests
{
    public class PhysicsTests
    {
        private static Level Cocina => Levels.Get("cocina");

        private static void Run(Kart k, Level level, KartInput input, float seconds, float mult = 1f)
        {
            int steps = (int)(seconds / Tuning.Step);
            for (int i = 0; i < steps; i++)
            {
                KartPhysics.Step(k, input, Tuning.Step, level, mult, null);
                input.Flip = false;
                input.Jump = false;
            }
        }

        [Test]
        public void KartFallsAndRestsOnTheFloor()
        {
            var k = new Kart(0, 140, 500);
            Run(k, Cocina, KartInput.None, 1.5f);
            Assert.That(k.Grounded, Is.True);
            Assert.That(k.Y, Is.EqualTo(850 - Tuning.Half).Within(0.01f));
        }

        [Test]
        public void DrivesAtTopSpeedOnTheFloor()
        {
            var k = new Kart(0, 140, 850 - Tuning.Half);
            Run(k, Cocina, new KartInput { X = 1 }, 0.6f);
            Assert.That(k.VX, Is.EqualTo(Tuning.Speed).Within(1f));
            Assert.That(k.Grounded, Is.True);
        }

        [Test]
        public void FlipSendsTheKartToTheCeiling()
        {
            var k = new Kart(0, 500, 850 - Tuning.Half);
            Run(k, Cocina, KartInput.None, 0.1f);
            Run(k, Cocina, new KartInput { Flip = true }, 1.5f);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Up));
            Assert.That(k.Grounded, Is.True);
            Assert.That(k.Y, Is.EqualTo(50 + Tuning.Half).Within(0.01f));
        }

        [Test]
        public void DrivingOnTheCeilingStillUsesScreenDirections()
        {
            var k = new Kart(0, 140, 850 - Tuning.Half);
            Run(k, Cocina, new KartInput { Flip = true }, 1.5f);
            float x = k.X;
            Run(k, Cocina, new KartInput { X = 1 }, 0.5f);
            Assert.That(k.X, Is.GreaterThan(x + 100f));
            Assert.That(k.Gravity, Is.EqualTo(Dir.Up));
        }

        [Test]
        public void FlipWhilePushingAWallSticksToTheWall()
        {
            var k = new Kart(0, 140, 850 - Tuning.Half);
            Run(k, Cocina, new KartInput { X = -1 }, 0.8f);
            Assert.That(k.X, Is.EqualTo(50 + Tuning.Half).Within(0.01f));

            Run(k, Cocina, new KartInput { X = -1, Flip = true }, 0.3f);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Left));
            Assert.That(k.Grounded, Is.True);

            // En la pared, arriba/abajo mueven a lo largo de la pared.
            float y = k.Y;
            Run(k, Cocina, new KartInput { Y = -1 }, 0.3f);
            Assert.That(k.Y, Is.LessThan(y - 50f));
            Assert.That(k.X, Is.EqualTo(50 + Tuning.Half).Within(0.01f));
        }

        [Test]
        public void PushingAwayFromTheSurfaceJumps()
        {
            var k = new Kart(0, 140, 850 - Tuning.Half);
            Run(k, Cocina, KartInput.None, 0.1f);
            KartPhysics.Step(k, new KartInput { Y = -1 }, Tuning.Step, Cocina, 1f, null);
            Assert.That(k.VY, Is.LessThan(-Tuning.JumpSpeed + 50f));
            Run(k, Cocina, new KartInput { Y = -1 }, 0.2f);
            Assert.That(k.Grounded, Is.False);
            Assert.That(k.Y, Is.LessThan(850 - Tuning.Half - 100f));
        }

        [Test]
        public void JumpReachesTheKitchenTable()
        {
            var k = new Kart(0, 380, 850 - Tuning.Half);
            Run(k, Cocina, KartInput.None, 0.1f);
            int steps = (int)(1.5f / Tuning.Step);
            for (int i = 0; i < steps; i++)
            {
                var input = new KartInput { X = 1, Jump = i == 20 };
                KartPhysics.Step(k, input, Tuning.Step, Cocina, 1f, null);
                if (k.X > 700f) break;
            }
            Run(k, Cocina, KartInput.None, 0.5f);
            Assert.That(k.Y, Is.EqualTo(700 - Tuning.Half).Within(0.01f), "debería quedar arriba de la mesa");
        }

        [Test]
        public void OnlyOneFlipInTheAirUntilLanding()
        {
            var k = new Kart(0, 140, 850 - Tuning.Half);
            Run(k, Cocina, KartInput.None, 0.1f);
            Run(k, Cocina, new KartInput { Flip = true }, 0.35f);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Up));
            Run(k, Cocina, new KartInput { Flip = true }, 0.35f);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Down), "el cambio en el aire está permitido una vez");
            Assert.That(k.Grounded, Is.False);
            Run(k, Cocina, new KartInput { Flip = true }, 0.05f);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Down), "el segundo cambio en el aire no está permitido");
        }

        [Test]
        public void FastFallsNeverTunnelThroughThinShelves()
        {
            var level = Levels.Get("cocina");
            var k = new Kart(0, 150, 100) { VY = Tuning.MaxFall };
            for (int i = 0; i < 60; i++)
                KartPhysics.Step(k, KartInput.None, 1f / 20f, level, 1f, null);
            Assert.That(k.Y, Is.EqualTo(430 - Tuning.Half).Within(0.01f));
        }

        [Test]
        public void PipeCarriesTheKartToTheOtherMouthAndFlipsGravity()
        {
            var level = Cocina;
            var mouth = level.Pipes[0].Ends[0];
            var exit = level.Pipes[0].Ends[1];
            var k = new Kart(0, mouth.X, mouth.Y - Tuning.Half);
            Run(k, level, KartInput.None, 0.1f);
            Run(k, level, new KartInput { Y = 1 }, 0.05f);
            Assert.That(k.InPipe, Is.True);

            Run(k, level, KartInput.None, 3f);
            Assert.That(k.InPipe, Is.False);
            Assert.That(k.Gravity, Is.EqualTo(Dir.Opposite(exit.Normal)));
            Assert.That(k.Grounded, Is.True);
            Assert.That(k.X, Is.EqualTo(exit.X).Within(2f));
            Assert.That(k.Y, Is.EqualTo(exit.Y + Tuning.Half).Within(0.01f));
        }

        [Test]
        public void HoldingDownAfterAPipeDoesNotReenterImmediately()
        {
            var level = Cocina;
            var mouth = level.Pipes[0].Ends[0];
            var k = new Kart(0, mouth.X, mouth.Y - Tuning.Half);
            Run(k, level, KartInput.None, 0.1f);
            var down = new KartInput { Y = 1 };
            int entries = 0;
            bool was = false;
            for (int i = 0; i < (int)(4f / Tuning.Step); i++)
            {
                KartPhysics.Step(k, down, Tuning.Step, level, 1f, null);
                if (k.InPipe && !was) entries++;
                was = k.InPipe;
            }
            Assert.That(entries, Is.EqualTo(1));
        }
    }
}
