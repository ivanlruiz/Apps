using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>Todo lo que se dibuja de una partida: sala, karting, efectos y el aro de tiempo del que lleva la comida.</summary>
    public sealed class MatchView
    {
        public readonly GameObject Root;

        private readonly LevelView _level;
        private readonly KartView[] _karts = new KartView[2];
        private readonly Fx _fx;
        private readonly LineRenderer _ring, _ringBack;
        private readonly Vector3[] _ringPoints = new Vector3[49];
        private float _crumbTimer;
        private readonly float[] _dustTimer = new float[2];

        public MatchView(Transform parent, Match m, Look[] looks, string food)
        {
            Root = new GameObject("Partida");
            Root.transform.SetParent(parent, false);
            _level = new LevelView(Root.transform, m.Level, food);
            for (int i = 0; i < 2; i++) _karts[i] = new KartView(Root.transform, i, looks[i], food);
            _fx = new Fx(Root.transform);

            var material = _karts[0].Root.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            _ringBack = Line("Aro fondo", material, new Color(0.06f, 0.03f, 0.13f, 0.6f), 74, 0.09f);
            _ring = Line("Aro", material, Color.white, 75, 0.06f);
        }

        private LineRenderer Line(string name, Material material, Color color, int order, float width)
        {
            var lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.transform.SetParent(Root.transform, false);
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.numCapVertices = 4;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = order;
            lr.positionCount = 0;
            return lr;
        }

        public void Destroy() => Object.Destroy(Root);

        public void Update(Match m, float dt, float time)
        {
            _level.Update(m, time);
            for (int i = 0; i < 2; i++) _karts[i].Update(m, dt, time);
            _fx.Update(dt);
            UpdateRing(m, time);

            int c = m.Food.Carrier;
            if (c >= 0 && m.State == MatchState.Playing && !m.Karts[c].InPipe)
            {
                _crumbTimer -= dt;
                if (_crumbTimer <= 0f)
                {
                    _crumbTimer = 0.18f;
                    _fx.Crumbs(FoodWorld(m.Karts[c]), Coords.WorldDir(m.Karts[c].Gravity), 1);
                }
            }

            for (int i = 0; i < 2; i++)
            {
                var k = m.Karts[i];
                float along = Dir.IsVertical(k.Gravity) ? k.VX : k.VY;
                if (!k.Grounded || k.InPipe || Mathf.Abs(along) < 300f) continue;
                _dustTimer[i] -= dt;
                if (_dustTimer[i] > 0f) continue;
                _dustTimer[i] = 0.09f;
                var down = Coords.WorldDir(k.Gravity);
                _fx.Dust((Vector2)Coords.ToWorld(k.X, k.Y) + down * 0.22f, -down, 1, 0.6f);
            }
        }

        private static Vector3 FoodWorld(Kart k)
        {
            var up = -Coords.WorldDir(k.Gravity);
            float lift = -(Tuning.Half * (1f - KartView.VisualScale) + Art.FoodY * KartView.VisualScale);
            return Coords.ToWorld(k.X, k.Y) + (Vector3)(up * (lift / Coords.UnitsPerMeter));
        }

        private void UpdateRing(Match m, float time)
        {
            int c = m.Food.Carrier;
            if (c < 0 || m.Karts[c].InPipe || m.State == MatchState.Countdown)
            {
                _ring.positionCount = 0;
                _ringBack.positionCount = 0;
                return;
            }
            var center = FoodWorld(m.Karts[c]);
            float f = m.HoldFraction(c);
            float remaining = m.Config.HoldTime - m.Hold[c];
            float r = 0.34f + (remaining < 3f ? Mathf.Abs(Mathf.Sin(time * 9f)) * 0.04f : 0f);

            _ringBack.positionCount = _ringPoints.Length;
            for (int i = 0; i < _ringPoints.Length; i++)
            {
                float a = Mathf.PI / 2f - i / (float)(_ringPoints.Length - 1) * Mathf.PI * 2f;
                _ringPoints[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            _ringBack.SetPositions(_ringPoints);

            int n = Mathf.Max(2, Mathf.CeilToInt(f * (_ringPoints.Length - 1)) + 1);
            _ring.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI / 2f - Mathf.Min(f, i / (float)(_ringPoints.Length - 1)) * Mathf.PI * 2f;
                _ring.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            _ring.startColor = _ring.endColor = Palette.Player(c);
        }

        /// <summary>Convierte un evento de la simulación en sonido, partículas y textos.</summary>
        public void Handle(GameEvent e, Match m, Sfx sfx, Hud hud, System.Action<float> shake, string[] names)
        {
            var pos = (Vector2)Coords.ToWorld(e.X, e.Y);
            Color pc = e.Kart >= 0 ? Palette.Player(e.Kart) : Color.white;
            switch (e.Type)
            {
                case GameEventType.Countdown:
                    sfx.Play("count");
                    break;
                case GameEventType.Go:
                    sfx.Play("go");
                    hud?.Go();
                    break;
                case GameEventType.Pickup:
                    sfx.Play("pickup");
                    _fx.Burst(pos, Palette.Cheese, 10, 2.5f);
                    hud?.Popup(Coords.ToWorld(e.X, e.Y), "¡Mía!", pc);
                    break;
                case GameEventType.Steal:
                    sfx.Play("steal");
                    _fx.Burst(pos, pc, 16, 4f);
                    _fx.Crumbs(pos, Vector2.down, 8);
                    _fx.Wave(pos, pc, 2.2f);
                    hud?.Popup(Coords.ToWorld(e.X, e.Y), "¡Robo!", pc);
                    shake(0.22f);
                    break;
                case GameEventType.Bump:
                    sfx.Play("bump", 0.6f);
                    _fx.Burst(pos, Color.white, 4, 2f);
                    break;
                case GameEventType.Flip:
                    sfx.Play("flip", 0.8f, e.Value == Dir.Up || e.Value == Dir.Right ? 1.15f : 0.95f);
                    _fx.Wave(pos, pc, 1.2f);
                    break;
                case GameEventType.WallStick:
                    sfx.Play("stick");
                    _fx.Dust(pos, -Coords.WorldDir(e.Value), 6, 1f);
                    break;
                case GameEventType.Jump:
                {
                    sfx.Play("jump", 0.7f);
                    var down = Coords.WorldDir(m.Karts[e.Kart].Gravity);
                    _fx.Dust(pos + down * 0.22f, -down, 4, 0.8f);
                    break;
                }
                case GameEventType.Land:
                    if (e.Value > 350)
                    {
                        sfx.Play("land", Mathf.Clamp01(e.Value / 1200f));
                        var down = Coords.WorldDir(m.Karts[e.Kart].Gravity);
                        _fx.Dust(pos + down * 0.22f, -down, 5, 1f);
                        _karts[e.Kart].Squash(Mathf.Clamp01(e.Value / 1100f));
                    }
                    break;
                case GameEventType.PipeIn:
                    sfx.Play("pipein");
                    _fx.Wave(pos, Palette.Hex(m.Level.Pipes[e.Value].Color), 1.4f);
                    break;
                case GameEventType.PipeOut:
                    sfx.Play("pipeout");
                    _fx.Burst(pos, Palette.Hex(m.Level.Pipes[e.Value].Color), 6, 2.5f);
                    break;
                case GameEventType.Tick:
                    sfx.Play("tick", 0.8f, e.Value == 1 ? 1.4f : 1f);
                    break;
                case GameEventType.RoundWin:
                    sfx.Play("round");
                    _fx.Burst(pos, Palette.Cheese, 24, 5f);
                    hud?.Banner("¡Ronda para " + names[e.Kart] + "!", pc);
                    break;
                case GameEventType.MatchWin:
                    sfx.Play("win");
                    _fx.Burst(pos, Palette.Cheese, 36, 6f);
                    _fx.Burst(pos, pc, 24, 4f);
                    hud?.Banner("¡Ganó " + names[e.Kart] + "!", pc, 3.4f);
                    break;
            }
        }
    }
}
