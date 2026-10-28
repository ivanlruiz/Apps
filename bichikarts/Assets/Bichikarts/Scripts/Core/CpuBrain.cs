using System;
using System.Collections.Generic;

namespace Bichikarts.Core
{
    public enum CpuDifficulty
    {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>
    /// La compu. Cada tanto prueba en una simulación chiquita qué pasaría con varias combinaciones
    /// de "maniobras" (ir para un lado, saltar, cambiar la gravedad, meterse en una tubería) durante
    /// el próximo segundo y elige la mejor. Como usa la misma física que el juego, sirve en cualquier sala.
    ///
    /// - Sin dueño: va a la comida.
    /// - Persiguiendo: va hacia el que tiene la comida (o a la salida de su tubería).
    /// - Escapando: elige un "lugar seguro" al que llega antes que el otro (bocas de tubería,
    ///   techo, paredes, plataformas) y va hacia ahí sin pasar cerca del perseguidor.
    /// </summary>
    public sealed class CpuBrain
    {
        private const int ActNone = 0, ActJump = 1, ActFlip = 2, ActInto = 3;
        private const float RollStep = 0.05f;
        private const int Stage1Steps = 9;
        private const int Stage2Steps = 16;
        private const float Reach = Tuning.KartRadius * 2f - 4f;
        private const float Danger = 170f;

        private struct Macro
        {
            public int Tan;
            public int Act;

            public Macro(int tan, int act)
            {
                Tan = tan; Act = act;
            }
        }

        private sealed class Profile
        {
            public float Replan, Noise, Lead;
            public int NoisePool;
            public bool SmartPipes;
        }

        private struct Accum
        {
            public float MinD;
            public float ReachT;
            public float MinOpp;
            public float OppX, OppY;
            public bool Piped;
        }

        private struct Spot
        {
            public float X, Y;
            public bool Pipe;
        }

        private static readonly Macro[] Macros = BuildMacros();
        private static readonly Dictionary<string, Spot[]> SpotCache = new Dictionary<string, Spot[]>();

        private readonly Match _match;
        private readonly int _me;
        private readonly Profile _profile;
        private readonly Random _rng;
        private readonly Spot[] _spots;
        private readonly float[] _costs = new float[Macros.Length];
        private readonly int[] _order = new int[Macros.Length];
        private readonly Comparison<int> _byCost;

        private Macro _macro;
        private bool _fresh;
        private float _replanTimer;
        private float _exploreTimer;
        private float _progressTimer;
        private float _progressX, _progressY;
        private int _spot = -1;

        // Objetivo del plan actual.
        private bool _flee;
        private float _tx, _ty, _tvx, _tvy, _lead;
        private float _ox, _oy, _oppSpeed;

        public CpuDifficulty Difficulty { get; }

        public CpuBrain(Match match, int me, CpuDifficulty difficulty, int seed = 0)
        {
            _match = match;
            _me = me;
            Difficulty = difficulty;
            _rng = new Random(seed == 0 ? Environment.TickCount ^ (me * 7919) : seed);
            _profile = difficulty switch
            {
                CpuDifficulty.Easy => new Profile { Replan = 0.4f, Noise = 0.3f, NoisePool = 4, Lead = 0f, SmartPipes = false },
                CpuDifficulty.Hard => new Profile { Replan = 0.12f, Noise = 0f, NoisePool = 1, Lead = 0.35f, SmartPipes = true },
                _ => new Profile { Replan = 0.22f, Noise = 0.08f, NoisePool = 3, Lead = 0.2f, SmartPipes = true },
            };
            _spots = SpotsFor(match.Level);
            _byCost = (a, b) => _costs[a].CompareTo(_costs[b]);
        }

        /// <summary>Velocidad relativa que conviene darle a la compu según la dificultad.</summary>
        public static float HandicapFor(CpuDifficulty d) => d == CpuDifficulty.Easy ? 0.9f : 1f;

        private static Macro[] BuildMacros()
        {
            var list = new Macro[12];
            int i = 0;
            for (int tan = -1; tan <= 1; tan++)
                for (int act = 0; act < 4; act++)
                    list[i++] = new Macro(tan, act);
            return list;
        }

        public KartInput Step(float dt)
        {
            var k = _match.Karts[_me];
            if (_match.State != MatchState.Playing || k.InPipe)
            {
                _replanTimer = 0f;
                _progressTimer = 0f;
                return KartInput.None;
            }

            _replanTimer -= dt;
            if (_exploreTimer > 0f)
            {
                _exploreTimer -= dt;
            }
            else if (_replanTimer <= 0f)
            {
                _replanTimer = _profile.Replan * (0.85f + 0.3f * (float)_rng.NextDouble());
                Plan();
            }

            _progressTimer += dt;
            if (_progressTimer >= 1.1f)
            {
                float mx = k.X - _progressX, my = k.Y - _progressY;
                bool stuck = mx * mx + my * my < 30f * 30f;
                if (stuck && Dist(k.X, k.Y, _tx, _ty) > 70f) Explore();
                _progressTimer = 0f;
                _progressX = k.X;
                _progressY = k.Y;
            }

            var input = MacroInput(_macro, k, _fresh);
            _fresh = false;
            return input;
        }

        private void Explore()
        {
            int tan = _rng.Next(2) == 0 ? -1 : 1;
            int act = _rng.Next(3) switch { 0 => ActJump, 1 => ActFlip, _ => ActNone };
            _macro = new Macro(tan, act);
            _fresh = true;
            _exploreTimer = 0.5f;
        }

        private static KartInput MacroInput(Macro m, Kart k, bool first)
        {
            int g = k.Gravity;
            bool vertical = Dir.IsVertical(g);
            int gs = vertical ? Dir.Y[g] : Dir.X[g];
            int normal = m.Act == ActInto ? gs : 0;
            var input = new KartInput();
            if (vertical) { input.X = m.Tan; input.Y = normal; }
            else { input.Y = m.Tan; input.X = normal; }
            if (first)
            {
                input.Flip = m.Act == ActFlip;
                input.Jump = m.Act == ActJump;
            }
            return input;
        }

        // --- Objetivo ---

        private void SetupGoal()
        {
            var me = _match.Karts[_me];
            var op = _match.Karts[1 - _me];
            int carrier = _match.Food.Carrier;
            _tvx = _tvy = 0f;
            _lead = 0f;

            if (carrier < 0)
            {
                _flee = false;
                _spot = -1;
                _tx = _match.Food.X;
                _ty = _match.Food.Y;
                return;
            }

            if (carrier == _me)
            {
                _flee = true;
                StandingPosition(op, out _ox, out _oy);
                _oppSpeed = Tuning.Speed * Tuning.ChaserSpeed * 0.85f;
                ChooseSpot(me);
                _tx = _spots[_spot].X;
                _ty = _spots[_spot].Y;
                return;
            }

            _flee = false;
            _spot = -1;
            if (op.InPipe)
            {
                var pipe = _match.Level.Pipes[op.Pipe.Pipe];
                var end = _profile.SmartPipes ? pipe.Ends[1 - op.Pipe.From] : pipe.Ends[op.Pipe.From];
                _tx = end.X + Dir.X[end.Normal] * Tuning.Half;
                _ty = end.Y + Dir.Y[end.Normal] * Tuning.Half;
                return;
            }
            _tx = op.X;
            _ty = op.Y;
            _tvx = op.VX;
            _tvy = op.VY;
            _lead = _profile.Lead;
        }

        /// <summary>Elige el lugar al que conviene escapar: uno al que llego bastante antes que el otro.</summary>
        private void ChooseSpot(Kart me)
        {
            StandingPosition(me, out float mx, out float my);
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < _spots.Length; i++)
            {
                var s = _spots[i];
                float score = Dist(_ox, _oy, s.X, s.Y) - 1.3f * Dist(mx, my, s.X, s.Y);
                if (s.Pipe) score += 120f;
                if (i == _spot) score += 90f;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            _spot = best;
        }

        private void StandingPosition(Kart k, out float x, out float y)
        {
            if (k.InPipe)
            {
                var exit = KartPhysics.ExitOf(k, _match.Level);
                x = exit.X + Dir.X[exit.Normal] * Tuning.Half;
                y = exit.Y + Dir.Y[exit.Normal] * Tuning.Half;
                return;
            }
            x = k.X; y = k.Y;
        }

        // --- Planificación ---

        private void Plan()
        {
            SetupGoal();
            var start = _match.Karts[_me];
            float mult = _match.SpeedMultiplier(_me);
            float t1 = Stage1Steps * RollStep;
            float t2 = t1 + Stage2Steps * RollStep;

            for (int i = 0; i < Macros.Length; i++)
            {
                var k1 = start.Clone();
                var acc = new Accum { MinD = float.MaxValue, ReachT = -1f, MinOpp = float.MaxValue, OppX = _ox, OppY = _oy };
                bool done = Run(k1, Macros[i], Stage1Steps, 0f, mult, ref acc);

                float best;
                if (done)
                {
                    best = Cost(k1, acc, t1);
                }
                else
                {
                    best = float.MaxValue;
                    for (int j = 0; j < Macros.Length; j++)
                    {
                        var k2 = k1.Clone();
                        var acc2 = acc;
                        Run(k2, Macros[j], Stage2Steps, t1, mult, ref acc2);
                        float c = Cost(k2, acc2, t2) + Penalty(Macros[j]) * 0.5f;
                        if (c < best) best = c;
                    }
                }

                best += Penalty(Macros[i]);
                if (Macros[i].Tan == _macro.Tan && Macros[i].Act == _macro.Act) best -= 12f;
                _costs[i] = best;
                _order[i] = i;
            }

            Array.Sort(_order, _byCost);
            int pick = _order[0];
            if (_profile.Noise > 0f && _rng.NextDouble() < _profile.Noise)
                pick = _order[_rng.Next(Math.Min(_profile.NoisePool, _order.Length))];

            _macro = Macros[pick];
            _fresh = true;
        }

        private static float Penalty(Macro m) => m.Act switch
        {
            ActFlip => 10f,
            ActJump => 3f,
            ActInto => 1f,
            _ => 0f,
        };

        private bool Run(Kart k, Macro m, int steps, float t0, float mult, ref Accum acc)
        {
            var level = _match.Level;
            for (int s = 0; s < steps; s++)
            {
                var input = MacroInput(m, k, s == 0);
                KartPhysics.Step(k, input, RollStep, level, mult, null);
                float t = t0 + (s + 1) * RollStep;
                StandingPosition(k, out float x, out float y);
                float extra = k.InPipe ? (k.Pipe.Duration - k.Pipe.T) * Tuning.Speed : 0f;

                if (_flee)
                {
                    // El perseguidor viene hacia donde estoy, en línea recta.
                    float dx = x - acc.OppX, dy = y - acc.OppY;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    float move = MathF.Min(d, _oppSpeed * RollStep);
                    if (d > 0.001f) { acc.OppX += dx / d * move; acc.OppY += dy / d * move; }
                    float gap = k.InPipe ? d + extra : d - move;
                    if (gap < acc.MinOpp) acc.MinOpp = gap;
                    if (k.InPipe) acc.Piped = true;
                    float ds = Dist(x, y, _tx, _ty) + extra;
                    if (ds < acc.MinD) acc.MinD = ds;
                }
                else
                {
                    TargetAt(t, out float tx, out float ty);
                    float d = Dist(x, y, tx, ty) + extra;
                    if (d < acc.MinD) acc.MinD = d;
                    if (d < Reach && !k.InPipe)
                    {
                        acc.ReachT = t;
                        return true;
                    }
                }
            }
            return false;
        }

        private float Cost(Kart k, Accum acc, float t)
        {
            StandingPosition(k, out float x, out float y);
            float extra = k.InPipe ? (k.Pipe.Duration - k.Pipe.T) * Tuning.Speed : 0f;

            if (_flee)
            {
                float cost = 0.5f * acc.MinD + 0.7f * (Dist(x, y, _tx, _ty) + extra);
                if (acc.MinOpp < Danger) cost += (Danger - acc.MinOpp) * 12f;
                if (acc.Piped) cost -= Dist(_ox, _oy, x, y) < 700f ? 260f : 120f;
                if (!k.InPipe && Cornered(k)) cost += 120f;
                return cost;
            }

            if (acc.ReachT >= 0f) return acc.ReachT * 100f;
            TargetAt(t, out float tx, out float ty);
            float endD = Dist(x, y, tx, ty) + extra;
            float total = 1000f + acc.MinD + 0.5f * endD;
            if (!k.InPipe && Blocked(x, y, tx, ty)) total += 220f;
            return total;
        }

        private void TargetAt(float t, out float x, out float y)
        {
            float lt = MathF.Min(t, _lead);
            x = Math.Clamp(_tx + _tvx * lt, 75f, Tuning.RoomWidth - 75f);
            y = Math.Clamp(_ty + _tvy * lt, 75f, Tuning.RoomHeight - 75f);
        }

        private bool Cornered(Kart k)
        {
            var solids = _match.Level.Solids;
            int count = 0;
            for (int d = 0; d < 4; d++)
                if (KartPhysics.Touching(k, d, solids)) count++;
            return count >= 2;
        }

        /// <summary>True si un bloque interior tapa la línea recta entre los dos puntos.</summary>
        private bool Blocked(float ax, float ay, float bx, float by)
        {
            foreach (var s in _match.Level.Solids)
            {
                if (s.IsFrame) continue;
                if (SegmentHitsBox(ax, ay, bx, by, s.X + 2f, s.Y + 2f, s.Right - 2f, s.Bottom - 2f)) return true;
            }
            return false;
        }

        private static bool SegmentHitsBox(float ax, float ay, float bx, float by, float minX, float minY, float maxX, float maxY)
        {
            float t0 = 0f, t1 = 1f;
            float dx = bx - ax, dy = by - ay;
            if (!Clip(-dx, ax - minX, ref t0, ref t1)) return false;
            if (!Clip(dx, maxX - ax, ref t0, ref t1)) return false;
            if (!Clip(-dy, ay - minY, ref t0, ref t1)) return false;
            if (!Clip(dy, maxY - ay, ref t0, ref t1)) return false;
            return true;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (MathF.Abs(p) < 1e-6f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        private static float Dist(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx, dy = ay - by;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        // --- Lugares seguros de cada sala ---

        private static Spot[] SpotsFor(Level level)
        {
            lock (SpotCache)
            {
                if (SpotCache.TryGetValue(level.Id, out var cached)) return cached;
                var list = new List<Spot>();
                foreach (var p in level.Pipes)
                    foreach (var m in p.Ends)
                        list.Add(new Spot { X = m.X + Dir.X[m.Normal] * Tuning.Half, Y = m.Y + Dir.Y[m.Normal] * Tuning.Half, Pipe = true });

                // Puntos repartidos sobre cada cara de cada bloque, lejos de las esquinas.
                foreach (var s in level.Solids)
                {
                    AddFace(level, list, s.X, s.Y, s.Right, s.Y, 0, -1);
                    AddFace(level, list, s.X, s.Bottom, s.Right, s.Bottom, 0, 1);
                    AddFace(level, list, s.X, s.Y, s.X, s.Bottom, -1, 0);
                    AddFace(level, list, s.Right, s.Y, s.Right, s.Bottom, 1, 0);
                }
                var arr = list.ToArray();
                SpotCache[level.Id] = arr;
                return arr;
            }
        }

        private static void AddFace(Level level, List<Spot> list, float ax, float ay, float bx, float by, int nx, int ny)
        {
            float len = MathF.Abs(bx - ax) + MathF.Abs(by - ay);
            if (len < 120f) return;
            const float margin = 80f;
            int count = Math.Max(1, (int)((len - 2 * margin) / 220f) + 1);
            for (int i = 0; i < count; i++)
            {
                float f = count == 1 ? 0.5f : (margin + i * (len - 2 * margin) / (count - 1)) / len;
                float x = ax + (bx - ax) * f + nx * (Tuning.Half + 0.5f);
                float y = ay + (by - ay) * f + ny * (Tuning.Half + 0.5f);
                if (x < 50f || x > Tuning.RoomWidth - 50f || y < 50f || y > Tuning.RoomHeight - 50f) continue;
                var probe = new Kart(0, x, y);
                if (KartPhysics.Overlapping(probe, level.Solids)) continue;
                list.Add(new Spot { X = x, Y = y });
            }
        }
    }
}
