using System;
using System.Collections.Generic;

namespace Bichikarts.Core
{
    public sealed class MatchConfig
    {
        public string LevelId = "cocina";

        /// <summary>Segundos que hay que aguantar la comida para ganar la ronda.</summary>
        public float HoldTime = 15f;

        public int RoundsToWin = 2;

        /// <summary>Multiplicador de velocidad por jugador (la compu fácil va un poco más lenta).</summary>
        public float[] Handicap = { 1f, 1f };
    }

    public enum MatchState
    {
        Countdown,
        Playing,
        RoundOver,
        MatchOver,
    }

    public sealed class FoodState
    {
        public float X, Y;
        public int Carrier = -1;
    }

    public sealed class PlayerStats
    {
        public int Steals, Flips, Pipes, Pickups;
        public float BestHold;
    }

    /// <summary>
    /// Una partida entre dos karting: rondas, comida, robos y el tiempo de aguante.
    /// No sabe nada de Unity; se avanza con <see cref="Update"/> a paso fijo.
    /// </summary>
    public sealed class Match
    {
        public readonly MatchConfig Config;
        public readonly Level Level;
        public readonly Kart[] Karts = new Kart[2];
        public readonly FoodState Food = new FoodState();
        public readonly float[] Hold = new float[2];
        public readonly int[] Wins = new int[2];
        public readonly PlayerStats[] Stats = { new PlayerStats(), new PlayerStats() };
        public readonly List<GameEvent> Events = new List<GameEvent>();

        public int Round { get; private set; }
        public MatchState State { get; private set; }
        public float StateTimer { get; private set; }
        public int RoundWinner { get; private set; } = -1;
        public int Winner { get; private set; } = -1;
        public bool Finished { get; private set; }
        public float Time { get; private set; }

        private int _lastCount;
        private int _lastTick;
        private float _bumpCooldown;

        public Match(MatchConfig config)
        {
            Config = config;
            Level = Levels.Get(config.LevelId);
            StartRound();
        }

        public float HoldFraction(int i) => Math.Clamp(Hold[i] / Config.HoldTime, 0f, 1f);

        public float SpeedMultiplier(int i)
        {
            int c = Food.Carrier;
            if (c < 0) return 1f;
            return c == i ? Tuning.CarrierSpeed : Tuning.ChaserSpeed;
        }

        public void StartRound()
        {
            Round++;
            for (int i = 0; i < 2; i++)
            {
                Karts[i] = new Kart(i, Level.SpawnX[i], Level.SpawnY[i]) { Handicap = Config.Handicap[i] };
                Hold[i] = 0f;
            }
            Food.X = Level.FoodX;
            Food.Y = Level.FoodY;
            Food.Carrier = -1;
            RoundWinner = -1;
            State = MatchState.Countdown;
            StateTimer = Tuning.CountdownTime;
            _lastCount = 0;
            _lastTick = int.MaxValue;
        }

        public void Update(float dt, in KartInput p1, in KartInput p2)
        {
            Time += dt;
            _bumpCooldown -= dt;

            switch (State)
            {
                case MatchState.Countdown:
                {
                    StateTimer -= dt;
                    int n = (int)MathF.Ceiling(StateTimer);
                    if (n != _lastCount && n >= 1 && n <= 3)
                        Events.Add(new GameEvent(GameEventType.Countdown, -1, Food.X, Food.Y, n));
                    _lastCount = n;
                    Idle(dt);
                    if (StateTimer <= 0f)
                    {
                        State = MatchState.Playing;
                        Events.Add(new GameEvent(GameEventType.Go, -1, Food.X, Food.Y));
                    }
                    return;
                }
                case MatchState.RoundOver:
                    StateTimer -= dt;
                    Idle(dt);
                    if (StateTimer <= 0f) StartRound();
                    return;
                case MatchState.MatchOver:
                    StateTimer -= dt;
                    Idle(dt);
                    if (StateTimer <= 0f) Finished = true;
                    return;
            }

            int before = Events.Count;
            KartPhysics.Step(Karts[0], p1, dt, Level, SpeedMultiplier(0), Events);
            KartPhysics.Step(Karts[1], p2, dt, Level, SpeedMultiplier(1), Events);
            for (int i = before; i < Events.Count; i++)
            {
                var e = Events[i];
                if (e.Kart < 0) continue;
                if (e.Type == GameEventType.Flip || e.Type == GameEventType.WallStick) Stats[e.Kart].Flips++;
                else if (e.Type == GameEventType.PipeIn) Stats[e.Kart].Pipes++;
            }

            CollideKarts();
            UpdateFood(dt);
        }

        private void Idle(float dt)
        {
            KartPhysics.Step(Karts[0], KartInput.None, dt, Level, 1f, null);
            KartPhysics.Step(Karts[1], KartInput.None, dt, Level, 1f, null);
        }

        private void CollideKarts()
        {
            var a = Karts[0];
            var b = Karts[1];
            if (a.InPipe || b.InPipe) return;

            float dx = b.X - a.X, dy = b.Y - a.Y;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            float reach = Tuning.KartRadius * 2f;
            if (d >= reach) return;

            float nx = d > 0.001f ? dx / d : 1f;
            float ny = d > 0.001f ? dy / d : 0f;
            float push = (reach - d) * 0.5f + 0.5f;
            var solids = Level.Solids;
            KartPhysics.MoveX(a, -nx * push, solids);
            KartPhysics.MoveY(a, -ny * push, solids);
            KartPhysics.MoveX(b, nx * push, solids);
            KartPhysics.MoveY(b, ny * push, solids);

            float va = a.VX * nx + a.VY * ny;
            float vb = b.VX * nx + b.VY * ny;
            float closing = va - vb;
            if (closing > 0f)
            {
                float j = closing * 0.9f;
                a.VX -= j * nx; a.VY -= j * ny;
                b.VX += j * nx; b.VY += j * ny;
            }

            int c = Food.Carrier;
            if (c >= 0)
            {
                var carrier = Karts[c];
                var thief = Karts[1 - c];
                if (carrier.Shield <= 0f && thief.Stun <= 0f)
                {
                    Steal(1 - c);
                    return;
                }
            }

            if (_bumpCooldown <= 0f)
            {
                _bumpCooldown = 0.25f;
                Events.Add(new GameEvent(GameEventType.Bump, -1, (a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f));
            }
        }

        private void Steal(int thiefIndex)
        {
            int victimIndex = 1 - thiefIndex;
            var thief = Karts[thiefIndex];
            var victim = Karts[victimIndex];

            Food.Carrier = thiefIndex;
            Hold[thiefIndex] = 0f;
            Hold[victimIndex] = 0f;
            _lastTick = int.MaxValue;

            victim.Stun = Tuning.StealStun;
            thief.Shield = Tuning.StealShield;

            float dx = victim.X - thief.X, dy = victim.Y - thief.Y;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < 0.001f) { dx = thiefIndex == 0 ? 1f : -1f; dy = 0f; d = 1f; }
            victim.VX = dx / d * Tuning.Knockback;
            victim.VY = dy / d * Tuning.Knockback;

            Stats[thiefIndex].Steals++;
            Events.Add(new GameEvent(GameEventType.Steal, thiefIndex, (thief.X + victim.X) * 0.5f, (thief.Y + victim.Y) * 0.5f, victimIndex));
        }

        private void UpdateFood(float dt)
        {
            if (Food.Carrier < 0)
            {
                float reach = Tuning.Half + Tuning.FoodRadius;
                for (int n = 0; n < 2; n++)
                {
                    // Si llegan los dos a la vez, alterna quién tiene prioridad en cada ronda.
                    var k = Karts[(n + Round) % 2];
                    if (k.InPipe) continue;
                    float dx = k.X - Food.X, dy = k.Y - Food.Y;
                    if (dx * dx + dy * dy < reach * reach)
                    {
                        Food.Carrier = k.Index;
                        k.Shield = Tuning.PickupShield;
                        Stats[k.Index].Pickups++;
                        Events.Add(new GameEvent(GameEventType.Pickup, k.Index, Food.X, Food.Y));
                        break;
                    }
                }
                return;
            }

            int c = Food.Carrier;
            Hold[c] += dt;
            Food.X = Karts[c].X;
            Food.Y = Karts[c].Y;
            if (Hold[c] > Stats[c].BestHold) Stats[c].BestHold = Hold[c];

            float remaining = Config.HoldTime - Hold[c];
            int whole = (int)MathF.Ceiling(remaining);
            if (whole <= 3 && whole >= 1 && whole < _lastTick)
                Events.Add(new GameEvent(GameEventType.Tick, c, Food.X, Food.Y, whole));
            _lastTick = Math.Min(_lastTick, whole);

            if (Hold[c] >= Config.HoldTime) WinRound(c);
        }

        private void WinRound(int i)
        {
            Wins[i]++;
            RoundWinner = i;
            var k = Karts[i];
            if (Wins[i] >= Config.RoundsToWin)
            {
                Winner = i;
                State = MatchState.MatchOver;
                StateTimer = Tuning.MatchOverTime;
                Events.Add(new GameEvent(GameEventType.MatchWin, i, k.X, k.Y));
            }
            else
            {
                State = MatchState.RoundOver;
                StateTimer = Tuning.RoundOverTime;
                Events.Add(new GameEvent(GameEventType.RoundWin, i, k.X, k.Y));
            }
        }
    }
}
