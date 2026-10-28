using System.Collections.Generic;
using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>La sala dibujada más las cositas que se mueven: estrellas que titilan, luces y la comida suelta.</summary>
    public sealed class LevelView
    {
        public readonly GameObject Root;

        private readonly List<(SpriteRenderer sr, float phase, float speed, float baseAlpha)> _twinkles =
            new List<(SpriteRenderer, float, float, float)>();
        private readonly Transform _food;
        private readonly SpriteRenderer _foodSprite, _foodGlow;
        private readonly SpriteRenderer[] _hints = new SpriteRenderer[2];

        public LevelView(Transform parent, Level level, string food)
        {
            Root = new GameObject("Sala " + level.Name);
            Root.transform.SetParent(parent, false);

            var bg = new GameObject("Fondo").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(Root.transform, false);
            bg.sprite = LevelArt.Build(level, 1f);
            bg.transform.position = Coords.ToWorld(0, 0);
            bg.sortingOrder = -100;

            var rng = new System.Random(level.Id.GetHashCode() ^ 77);
            foreach (var d in level.Decor)
            {
                switch (d.Kind)
                {
                    case "ventana":
                        for (int i = 0; i < 6; i++) AddTwinkle(d.X + 20 + (float)rng.NextDouble() * (d.W - 40), d.Y + 20 + (float)rng.NextDouble() * (d.H - 40), 0.12f, Color.white, rng);
                        break;
                    case "ojo_buey":
                    case "planeta":
                        float cx = d.Kind == "planeta" ? d.X : d.X + d.W / 2, cy = d.Kind == "planeta" ? d.Y : d.Y + d.H / 2;
                        for (int i = 0; i < 4; i++)
                        {
                            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = (float)rng.NextDouble() * d.W * 0.4f;
                            AddTwinkle(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, 0.12f, Color.white, rng);
                        }
                        break;
                    case "lamparita":
                        AddTwinkle(d.X, d.Y + 86, 1.6f, new Color(1f, 0.9f, 0.55f, 0.35f), rng, 9f);
                        break;
                    case "luces":
                        for (float x = 150; x < Tuning.RoomWidth; x += 300)
                            AddTwinkle(x, 97, 0.35f, rng.Next(2) == 0 ? Palette.Hex("#ff5d8f") : Palette.Hex("#2ec4d6"), rng, 3f);
                        break;
                }
            }

            _food = new GameObject("Comida suelta").transform;
            _food.SetParent(Root.transform, false);
            _foodGlow = _food.gameObject.AddComponent<SpriteRenderer>();
            _foodGlow.sprite = Art.Soft();
            _foodGlow.color = new Color(1f, 0.95f, 0.6f, 0.55f);
            _foodGlow.sortingOrder = 9;
            _foodGlow.transform.localScale = Vector3.one * 2.2f;
            var item = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            item.transform.SetParent(_food, false);
            item.transform.localScale = Vector3.one / 2.2f;
            item.sortingOrder = 10;
            _foodSprite = item;
            SetFood(food);

            for (int i = 0; i < 2; i++)
            {
                _hints[i] = new GameObject("Pista tubería").AddComponent<SpriteRenderer>();
                _hints[i].transform.SetParent(Root.transform, false);
                _hints[i].sprite = Art.Arrow();
                _hints[i].color = Palette.Player(i);
                _hints[i].sortingOrder = 60;
                _hints[i].gameObject.SetActive(false);
            }
        }

        public void SetFood(string food) => _foodSprite.sprite = Art.Food(food);

        private void AddTwinkle(float x, float y, float size, Color c, System.Random rng, float speed = 0f)
        {
            var sr = new GameObject("Brillito").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(Root.transform, false);
            sr.transform.position = Coords.ToWorld(x, y);
            sr.transform.localScale = Vector3.one * size;
            sr.sprite = Art.Soft();
            sr.color = c;
            sr.sortingOrder = -90;
            _twinkles.Add((sr, (float)rng.NextDouble() * 10f, speed > 0f ? speed : 1.5f + (float)rng.NextDouble() * 2f, c.a));
        }

        public void Update(Match m, float time)
        {
            foreach (var t in _twinkles)
            {
                var c = t.sr.color;
                c.a = t.baseAlpha * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(time * t.speed + t.phase)));
                t.sr.color = c;
            }

            bool free = m.Food.Carrier < 0;
            _food.gameObject.SetActive(free);
            if (free)
            {
                _food.position = Coords.ToWorld(m.Food.X, m.Food.Y + Mathf.Sin(time * 3f) * 4f);
                _food.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 2f) * 8f);
                _foodGlow.color = new Color(1f, 0.95f, 0.6f, 0.4f + Mathf.Sin(time * 4f) * 0.15f);
            }

            // Flechita sobre la boca de la tubería cuando un karting está parado encima.
            for (int i = 0; i < 2; i++)
            {
                var k = m.Karts[i];
                var mouth = !k.InPipe && k.Grounded ? MouthUnder(k, m.Level) : null;
                _hints[i].gameObject.SetActive(mouth != null && m.State == MatchState.Playing);
                if (mouth == null) continue;
                int n = mouth.Normal;
                float lift = 62f + Mathf.Sin(time * 8f) * 5f;
                _hints[i].transform.position = Coords.ToWorld(mouth.X + Dir.X[n] * lift, mouth.Y + Dir.Y[n] * lift);
                _hints[i].transform.rotation = Quaternion.Euler(0, 0, Coords.GravityAngle(Dir.Opposite(n)));
            }
        }

        private static PipeEnd MouthUnder(Kart k, Level level)
        {
            int g = k.Gravity;
            float cx = k.X + Dir.X[g] * Tuning.Half, cy = k.Y + Dir.Y[g] * Tuning.Half;
            bool vertical = Dir.IsVertical(g);
            foreach (var p in level.Pipes)
                foreach (var m in p.Ends)
                {
                    if (Dir.Opposite(m.Normal) != g) continue;
                    float along = vertical ? cx - m.X : cy - m.Y;
                    float across = vertical ? cy - m.Y : cx - m.X;
                    if (Mathf.Abs(along) <= Tuning.MouthTolerance && Mathf.Abs(across) <= 1.5f) return m;
                }
            return null;
        }
    }
}
