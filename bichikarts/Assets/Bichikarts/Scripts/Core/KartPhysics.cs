using System;
using System.Collections.Generic;

namespace Bichikarts.Core
{
    /// <summary>
    /// Movimiento de un karting con gravedad propia. Cada karting anda pegado a la superficie hacia
    /// la que tira su gravedad: piso, techo, paredes o cualquier cara de un bloque.
    ///
    /// Reglas de los controles:
    /// - Las flechas mueven a lo largo de la superficie.
    /// - Empujar hacia afuera de la superficie salta.
    /// - Empujar hacia adentro, parado sobre la boca de una tubería, te mete en la tubería.
    /// - El botón de gravedad la invierte (piso ↔ techo). Si estás empujando contra una pared que
    ///   tocás, te pegás a esa pared. En el aire se puede usar una sola vez hasta volver a apoyarse.
    /// </summary>
    public static class KartPhysics
    {
        private const float Eps = 0.01f;
        private const float Contact = 1f;

        public static void Step(Kart k, in KartInput input, float dt, Level level, float speedMult, List<GameEvent> events)
        {
            k.FlipCooldown -= dt;
            k.Stun -= dt;
            k.Shield -= dt;
            k.PipeCooldown -= dt;
            k.CoyoteTimer -= dt;
            k.JumpBufferTimer -= dt;

            if (k.Pipe.Active)
            {
                AdvancePipe(k, dt, level, events);
                return;
            }

            var solids = level.Solids;
            bool control = k.Stun <= 0f;
            int ix = control ? input.X : 0;
            int iy = control ? input.Y : 0;

            if (control && input.Flip && k.FlipCooldown <= 0f && (k.Grounded || k.AirFlips > 0))
                SwitchGravity(k, ix, iy, solids, events);

            int g = k.Gravity;
            bool vertical = Dir.IsVertical(g);
            int gs = vertical ? Dir.Y[g] : Dir.X[g];
            int tangentIn = vertical ? ix : iy;
            int normalIn = vertical ? iy : ix;
            bool away = normalIn == -gs;
            bool into = normalIn == gs;

            if (control && away && !k.AwayHeld) k.JumpBufferTimer = Tuning.JumpBuffer;
            if (control && input.Jump) k.JumpBufferTimer = Tuning.JumpBuffer;
            k.AwayHeld = away;
            if (!into) k.IntoLock = false;

            float vt = vertical ? k.VX : k.VY;
            float vn = (vertical ? k.VY : k.VX) * gs;

            float max = Tuning.Speed * speedMult * k.Handicap;
            if (tangentIn != 0)
            {
                float target = tangentIn * max;
                float accel = k.Grounded ? Tuning.AccelGround : Tuning.AccelAir;
                if (vt * tangentIn < 0f) accel *= 1.8f;
                if (MathF.Abs(vt) > max && vt * tangentIn > 0f)
                    vt = Approach(vt, target, Tuning.Friction * 0.5f * dt);
                else
                    vt = Approach(vt, target, accel * dt);
            }
            else
            {
                vt = Approach(vt, 0f, (k.Grounded ? Tuning.Friction : Tuning.AirDrag) * dt);
            }

            vn = MathF.Min(vn + Tuning.Gravity * dt, Tuning.MaxFall);

            if (control && k.JumpBufferTimer > 0f && (k.Grounded || k.CoyoteTimer > 0f))
            {
                vn = -Tuning.JumpSpeed;
                k.JumpBufferTimer = 0f;
                k.CoyoteTimer = 0f;
                k.Grounded = false;
                events?.Add(new GameEvent(GameEventType.Jump, k.Index, k.X, k.Y));
            }

            if (vertical) { k.VX = vt; k.VY = vn * gs; }
            else { k.VY = vt; k.VX = vn * gs; }

            float impact = MathF.Max(0f, vn);
            MoveX(k, k.VX * dt, solids);
            MoveY(k, k.VY * dt, solids);

            bool was = k.Grounded;
            k.Grounded = Touching(k, k.Gravity, solids);
            if (k.Grounded)
            {
                k.CoyoteTimer = Tuning.CoyoteTime;
                k.AirFlips = 1;
                if (!was) events?.Add(new GameEvent(GameEventType.Land, k.Index, k.X, k.Y, (int)impact));
            }

            if (control && k.Grounded && into && !k.IntoLock && k.PipeCooldown <= 0f)
                TryEnterPipe(k, level, events);

            float along = Dir.IsVertical(k.Gravity) ? k.VX : k.VY;
            if (MathF.Abs(along) > 30f) k.Facing = along > 0f ? 1 : -1;
            k.WheelTurn += along * dt / 10f * (k.Grounded ? 1f : 0.3f);
        }

        private static void SwitchGravity(Kart k, int ix, int iy, List<Solid> solids, List<GameEvent> events)
        {
            bool vertical = Dir.IsVertical(k.Gravity);
            int tangentIn = vertical ? ix : iy;
            int next = Dir.Opposite(k.Gravity);
            bool wall = false;
            if (tangentIn != 0)
            {
                int d = vertical ? Dir.FromVector(tangentIn, 0) : Dir.FromVector(0, tangentIn);
                if (Touching(k, d, solids)) { next = d; wall = true; }
            }

            if (!k.Grounded) k.AirFlips--;
            k.Gravity = next;
            k.FlipCooldown = Tuning.FlipCooldown;
            k.Grounded = false;
            k.CoyoteTimer = 0f;
            k.IntoLock = true;
            k.AwayHeld = true;
            k.VX += Dir.X[next] * Tuning.FlipKick;
            k.VY += Dir.Y[next] * Tuning.FlipKick;
            events?.Add(new GameEvent(wall ? GameEventType.WallStick : GameEventType.Flip, k.Index, k.X, k.Y, next));
        }

        private static bool TryEnterPipe(Kart k, Level level, List<GameEvent> events)
        {
            int g = k.Gravity;
            float cx = k.X + Dir.X[g] * Tuning.Half;
            float cy = k.Y + Dir.Y[g] * Tuning.Half;
            bool vertical = Dir.IsVertical(g);
            foreach (var p in level.Pipes)
            {
                for (int e = 0; e < 2; e++)
                {
                    var m = p.Ends[e];
                    if (Dir.Opposite(m.Normal) != g) continue;
                    float along = vertical ? cx - m.X : cy - m.Y;
                    float across = vertical ? cy - m.Y : cx - m.X;
                    if (MathF.Abs(along) > Tuning.MouthTolerance || MathF.Abs(across) > 1.5f) continue;

                    k.Pipe = new PipeTravel
                    {
                        Active = true,
                        Pipe = p.Id,
                        From = e,
                        T = 0f,
                        Duration = MathF.Max(Tuning.PipeMinTime, p.Length / Tuning.PipeSpeed),
                    };
                    k.VX = k.VY = 0f;
                    k.Grounded = false;
                    events?.Add(new GameEvent(GameEventType.PipeIn, k.Index, m.X, m.Y, p.Id));
                    return true;
                }
            }
            return false;
        }

        private static void AdvancePipe(Kart k, float dt, Level level, List<GameEvent> events)
        {
            var p = level.Pipes[k.Pipe.Pipe];
            k.Pipe.T += dt;
            float f = MathF.Min(1f, k.Pipe.T / k.Pipe.Duration);
            p.PointAt(k.Pipe.From == 0 ? f : 1f - f, out k.X, out k.Y);
            if (k.Pipe.T >= k.Pipe.Duration) ExitPipe(k, p, 1 - k.Pipe.From, events);
        }

        private static void ExitPipe(Kart k, Pipe p, int end, List<GameEvent> events)
        {
            var m = p.Ends[end];
            int n = m.Normal;
            k.Pipe = default;
            k.Gravity = Dir.Opposite(n);
            k.X = m.X + Dir.X[n] * (Tuning.Half + 0.5f);
            k.Y = m.Y + Dir.Y[n] * (Tuning.Half + 0.5f);
            k.VX = Dir.X[n] * Tuning.PipePop;
            k.VY = Dir.Y[n] * Tuning.PipePop;
            k.Grounded = false;
            k.PipeCooldown = Tuning.PipeCooldown;
            k.IntoLock = true;
            k.AwayHeld = true;
            k.AirFlips = 1;
            events?.Add(new GameEvent(GameEventType.PipeOut, k.Index, m.X, m.Y, p.Id));
        }

        /// <summary>Dónde sale un karting que está viajando por una tubería.</summary>
        public static PipeEnd ExitOf(Kart k, Level level)
        {
            if (!k.Pipe.Active) return null;
            return level.Pipes[k.Pipe.Pipe].Ends[1 - k.Pipe.From];
        }

        // --- Choques contra los bloques (barrido por eje, sin atravesar bloques finos) ---

        private static bool OverlapsY(Kart k, Solid s) => k.Y + Tuning.Half > s.Y + Eps && k.Y - Tuning.Half < s.Bottom - Eps;
        private static bool OverlapsX(Kart k, Solid s) => k.X + Tuning.Half > s.X + Eps && k.X - Tuning.Half < s.Right - Eps;

        public static bool MoveX(Kart k, float dx, List<Solid> solids)
        {
            if (dx == 0f) return false;
            float h = Tuning.Half;
            float nx = k.X + dx;
            bool hit = false;
            foreach (var s in solids)
            {
                if (!OverlapsY(k, s)) continue;
                if (dx > 0f)
                {
                    float edge = s.X - h;
                    if (k.X <= edge + Eps && nx > edge) { nx = edge; hit = true; }
                }
                else
                {
                    float edge = s.Right + h;
                    if (k.X >= edge - Eps && nx < edge) { nx = edge; hit = true; }
                }
            }
            k.X = nx;
            if (hit) k.VX = 0f;
            return hit;
        }

        public static bool MoveY(Kart k, float dy, List<Solid> solids)
        {
            if (dy == 0f) return false;
            float h = Tuning.Half;
            float ny = k.Y + dy;
            bool hit = false;
            foreach (var s in solids)
            {
                if (!OverlapsX(k, s)) continue;
                if (dy > 0f)
                {
                    float edge = s.Y - h;
                    if (k.Y <= edge + Eps && ny > edge) { ny = edge; hit = true; }
                }
                else
                {
                    float edge = s.Bottom + h;
                    if (k.Y >= edge - Eps && ny < edge) { ny = edge; hit = true; }
                }
            }
            k.Y = ny;
            if (hit) k.VY = 0f;
            return hit;
        }

        /// <summary>True si hay un bloque pegado al karting en la dirección d.</summary>
        public static bool Touching(Kart k, int d, List<Solid> solids)
        {
            float h = Tuning.Half;
            foreach (var s in solids)
            {
                switch (d)
                {
                    case Dir.Down:
                        if (OverlapsX(k, s) && MathF.Abs(s.Y - (k.Y + h)) < Contact) return true;
                        break;
                    case Dir.Up:
                        if (OverlapsX(k, s) && MathF.Abs(s.Bottom - (k.Y - h)) < Contact) return true;
                        break;
                    case Dir.Left:
                        if (OverlapsY(k, s) && MathF.Abs(s.Right - (k.X - h)) < Contact) return true;
                        break;
                    case Dir.Right:
                        if (OverlapsY(k, s) && MathF.Abs(s.X - (k.X + h)) < Contact) return true;
                        break;
                }
            }
            return false;
        }

        /// <summary>True si el karting se superpone con algún bloque (no debería pasar nunca).</summary>
        public static bool Overlapping(Kart k, List<Solid> solids)
        {
            foreach (var s in solids)
                if (OverlapsX(k, s) && OverlapsY(k, s)) return true;
            return false;
        }

        public static float Approach(float v, float target, float step)
        {
            return v < target ? MathF.Min(v + step, target) : MathF.Max(v - step, target);
        }
    }
}
