using System.Collections.Generic;
using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>
    /// Pinta cada sala completa (fondo, adornos, tuberías del fondo, bloques y bocas) en una sola textura.
    /// Lo que se mueve (estrellas que titilan, luces) lo agrega <see cref="LevelView"/> encima.
    /// </summary>
    public static class LevelArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Color Ink = Palette.Ink;

        /// <summary>Sprite de la sala con el pivote en su esquina superior izquierda.</summary>
        public static Sprite Build(Level level, float scale)
        {
            string key = level.Id + scale;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var p = new Paint(Mathf.RoundToInt(Tuning.RoomWidth * scale), Mathf.RoundToInt(Tuning.RoomHeight * scale), scale);
            var rng = new System.Random(level.Id.GetHashCode());

            Background(p, level, rng);
            foreach (var d in level.Decor) DrawDecor(p, level, d, rng);
            foreach (var pipe in level.Pipes) DrawPipe(p, pipe);
            foreach (var s in level.Solids) DrawSolid(p, level, s, rng);
            foreach (var pipe in level.Pipes)
                foreach (var m in pipe.Ends) DrawMouth(p, m, Palette.Hex(pipe.Color));

            var sprite = p.ToSprite(0, 0);
            sprite.name = "sala_" + key;
            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ fondos

        private static void Background(Paint p, Level level, System.Random rng)
        {
            const float w = Tuning.RoomWidth, h = Tuning.RoomHeight;
            switch (level.Theme)
            {
                case "sotano":
                {
                    p.Fill(Palette.Hex("#2a1a26"));
                    for (int row = 0; row * 26 < h; row++)
                    {
                        float off = row % 2 == 0 ? 0 : -30;
                        for (float x = off; x < w; x += 60)
                        {
                            float t = (float)rng.NextDouble();
                            var c = Color.Lerp(Palette.Hex("#5e3236"), Palette.Hex("#77443a"), t);
                            p.RoundRect(x + 2, row * 26 + 2, 56, 22, 4, c);
                        }
                    }
                    p.VGradient(0, 0, w, 300, new Color(0.05f, 0.02f, 0.06f, 0.75f), new Color(0.05f, 0.02f, 0.06f, 0f));
                    p.VGradient(0, 600, w, 300, new Color(0.05f, 0.02f, 0.06f, 0f), new Color(0.05f, 0.02f, 0.06f, 0.6f));
                    break;
                }
                case "estacion":
                {
                    p.VGradient(0, 0, w, h, Palette.Hex("#2b3858"), Palette.Hex("#1f2944"));
                    var seam = Palette.Hex("#18203a");
                    var rivet = Palette.Hex("#4a5878");
                    for (float x = 0; x <= w; x += 200) p.Rect(x - 1.5f, 0, 3, h, seam);
                    for (float y = 0; y <= h; y += 150) p.Rect(0, y - 1.5f, w, 3, seam);
                    for (float x = 0; x <= w; x += 200)
                        for (float y = 0; y <= h; y += 150)
                        {
                            p.Circle(x + 10, y + 10, 3, rivet);
                            p.Circle(x - 10, y + 10, 3, rivet);
                        }
                    for (float x = 150; x < w; x += 300) p.RoundRect(x - 14, 92, 28, 10, 4, Palette.Hex("#3a4668"));
                    break;
                }
                case "living":
                {
                    for (float x = 0; x < w; x += 80)
                    {
                        p.Rect(x, 0, 40, h, Palette.Hex("#f2c46d"));
                        p.Rect(x + 40, 0, 40, h, Palette.Hex("#ecb85e"));
                    }
                    for (float x = 20; x < w; x += 80)
                        for (float y = 30; y < 620; y += 70)
                            p.Polygon(Paint.StarPoints(x + 20, y + (x % 160 == 20 ? 35 : 0), 4, 5, 2, 0), Palette.WithAlpha(Palette.Hex("#d89b3d"), 0.6f));
                    p.Rect(0, 620, w, 230, Palette.Hex("#8a5a3b"));
                    for (float x = 0; x < w; x += 70) p.Rect(x, 620, 3, 230, Palette.Hex("#6e4529"));
                    p.Rect(0, 612, w, 12, Palette.Hex("#6e4529"));
                    p.Rect(0, 614, w, 4, Palette.Hex("#a8734c"));
                    break;
                }
                default: // cocina
                {
                    p.VGradient(0, 0, w, 600, Palette.Hex("#ffe3c2"), Palette.Hex("#f8cfa2"));
                    for (float y = 40; y < 590; y += 56)
                        for (float x = (y / 56) % 2 < 1 ? 20 : 48; x < w; x += 56)
                            p.Polygon(Paint.StarPoints(x, y, 4, 4.5f, 1.8f, 0), Palette.WithAlpha(Palette.Hex("#e9a46b"), 0.55f));
                    p.Rect(0, 600, w, 250, Palette.Hex("#c9eedb"));
                    var grout = Palette.Hex("#a3d9c0");
                    for (float x = 0; x < w; x += 50) p.Rect(x - 1, 600, 2, 250, grout);
                    for (float y = 600; y < 850; y += 50) p.Rect(0, y - 1, w, 2, grout);
                    p.Rect(0, 594, w, 10, Palette.Hex("#e9993f"));
                    p.Rect(0, 594, w, 2, Palette.Hex("#b5651d"));
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ adornos

        private static void DrawDecor(Paint p, Level level, Decor d, System.Random rng)
        {
            switch (d.Kind)
            {
                case "ventana":
                    Window(p, d, rng, level.Theme == "living" ? Palette.Hex("#5b8bd9") : Palette.Hex("#e5484d"));
                    break;
                case "ojo_buey":
                    Porthole(p, d.X + d.W / 2, d.Y + d.H / 2, d.W / 2, rng, false);
                    break;
                case "planeta":
                    Porthole(p, d.X, d.Y, d.W / 2, rng, true);
                    break;
                case "pata":
                    p.RoundRect(d.X - 2, d.Y, d.W + 4, d.H, 4, Ink);
                    p.RoundRect(d.X, d.Y, d.W, d.H, 3, Palette.Hex("#8a4f24"));
                    break;
                case "cadena":
                case "varilla":
                    for (float y = d.Y; y < d.Y + d.H; y += 9)
                        p.Ellipse(d.X + d.W / 2, y + 4, 2.5f, 4.5f, Palette.Hex("#7b8496"));
                    break;
                case "reloj":
                    p.Circle(d.X, d.Y, 42, Ink);
                    p.Circle(d.X, d.Y, 38, Palette.Hex("#fbe6bd"));
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI / 6f;
                        p.Circle(d.X + Mathf.Cos(a) * 30, d.Y + Mathf.Sin(a) * 30, 2.2f, Ink);
                    }
                    p.Line(d.X, d.Y, d.X + 14, d.Y - 16, 2.6f, Ink);
                    p.Line(d.X, d.Y, d.X - 4, d.Y - 26, 1.8f, Ink);
                    p.Circle(d.X, d.Y, 4, Palette.Hex("#e5484d"));
                    break;
                case "lamparita":
                    p.Line(d.X, d.Y, d.X, d.Y + 70, 1.6f, Ink);
                    p.Polygon(new[] { d.X - 16, d.Y + 86, d.X + 16, d.Y + 86, d.X + 190, 850f, d.X - 190, 850f }, new Color(1f, 0.9f, 0.5f, 0.08f));
                    p.RoundRect(d.X - 6, d.Y + 66, 12, 10, 2, Palette.Hex("#5b6273"));
                    p.RadialGlow(d.X, d.Y + 86, 60, new Color(1f, 0.9f, 0.55f, 0.45f), new Color(1f, 0.9f, 0.55f, 0f));
                    p.Circle(d.X, d.Y + 86, 13, Ink);
                    p.Circle(d.X, d.Y + 86, 11, Palette.Hex("#ffe9a3"));
                    break;
                case "valvula":
                {
                    var red = Palette.Hex("#e5484d");
                    p.Ring(d.X, d.Y, 26, 6, Ink);
                    p.Ring(d.X, d.Y, 26, 3.5f, red);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        p.Line(d.X - Mathf.Cos(a) * 25, d.Y - Mathf.Sin(a) * 25, d.X + Mathf.Cos(a) * 25, d.Y + Mathf.Sin(a) * 25, 2.4f, red);
                    }
                    p.Circle(d.X, d.Y, 6, Ink);
                    p.Circle(d.X, d.Y, 4, red);
                    break;
                }
                case "cuadro":
                {
                    p.RoundRect(d.X - 4, d.Y - 4, d.W + 8, d.H + 8, 6, Ink);
                    p.RoundRect(d.X, d.Y, d.W, d.H, 4, Palette.Hex("#d9a441"));
                    p.Rect(d.X + 12, d.Y + 12, d.W - 24, d.H - 24, Palette.Hex("#2e8a7a"));
                    float cx = d.X + d.W / 2, cy = d.Y + d.H / 2 + 6;
                    p.Circle(cx, cy, 26, Ink);
                    p.Circle(cx, cy, 23, Palette.Hex("#79f0c3"));
                    p.Circle(cx, cy - 6, 11, Color.white);
                    p.Circle(cx + 2, cy - 6, 6, Ink);
                    p.Arc(cx, cy + 6, 8, 3, 30, 150, Ink);
                    p.Line(cx, cy - 23, cx, cy - 38, 2, Ink);
                    p.Circle(cx, cy - 40, 5, Palette.Hex("#c9fff0"));
                    break;
                }
                case "lampara":
                    p.Ellipse(d.X, d.Y - 6, 34, 9, Ink);
                    p.Ellipse(d.X, d.Y - 7, 30, 7, Palette.Hex("#3a3f52"));
                    p.Line(d.X, d.Y - 10, d.X, 560, 3.5f, Palette.Hex("#3a3f52"));
                    p.RadialGlow(d.X, 520, 140, new Color(1f, 0.85f, 0.4f, 0.35f), new Color(1f, 0.85f, 0.4f, 0f));
                    p.Polygon(new[] { d.X - 36, 568f, d.X + 36, 568f, d.X + 22, 488f, d.X - 22, 488f }, Ink);
                    p.Polygon(new[] { d.X - 31, 564f, d.X + 31, 564f, d.X + 19, 492f, d.X - 19, 492f }, Palette.Cheese);
                    break;
                case "libros":
                {
                    if (d.Y < 400) p.RoundRect(d.X, 280, Tuning.RoomWidth - Tuning.WallThickness - d.X, 330, 8, Palette.Hex("#5a3720"));
                    float x = d.X + 16;
                    while (x < Tuning.RoomWidth - 80)
                    {
                        float bw = 14 + (float)rng.NextDouble() * 12;
                        float bh = 40 + (float)rng.NextDouble() * 30;
                        var c = Color.HSVToRGB((float)rng.NextDouble(), 0.55f, 0.85f);
                        p.Rect(x - 1.5f, d.Y - bh - 1.5f, bw + 3, bh + 3, Ink);
                        p.Rect(x, d.Y - bh, bw, bh, c);
                        p.Rect(x + 3, d.Y - bh + 8, bw - 6, 3, Palette.WithAlpha(Color.white, 0.6f));
                        x += bw + 4 + (float)rng.NextDouble() * 6;
                    }
                    break;
                }
            }
        }

        private static void Stars(Paint p, float x, float y, float w, float h, System.Random rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float sx = x + (float)rng.NextDouble() * w, sy = y + (float)rng.NextDouble() * h;
                float r = 0.8f + (float)rng.NextDouble() * 1.8f;
                p.Circle(sx, sy, r, new Color(1f, 1f, 1f, 0.5f + (float)rng.NextDouble() * 0.5f));
            }
        }

        private static void Window(Paint p, Decor d, System.Random rng, Color curtain)
        {
            p.RoundRect(d.X - 14, d.Y - 14, d.W + 28, d.H + 28, 14, Ink);
            p.RoundRect(d.X - 10, d.Y - 10, d.W + 20, d.H + 20, 10, Palette.Hex("#a0612b"));
            p.VGradient(d.X, d.Y, d.W, d.H, Palette.Hex("#120c33"), Palette.Hex("#33206a"));
            Stars(p, d.X, d.Y, d.W, d.H, rng, (int)(d.W * d.H / 900));

            float px = d.X + d.W * 0.28f, py = d.Y + d.H * 0.36f, pr = d.H * 0.16f;
            p.Ellipse(px, py, pr * 1.9f, pr * 0.42f, Palette.Hex("#ffd9a8"), -14);
            p.Ellipse(px, py, pr * 1.6f, pr * 0.26f, Palette.Hex("#2a1a55"), -14);
            p.Circle(px, py, pr, Palette.Hex("#ff9f6b"));
            p.Ellipse(px - pr * 0.3f, py - pr * 0.3f, pr * 0.45f, pr * 0.25f, Palette.WithAlpha(Color.white, 0.35f), -20);
            p.Arc(px, py, pr * 1.5f, pr * 0.22f, 10, 160, Palette.Hex("#ffd9a8"));

            float ex = d.X + d.W * 0.78f, ey = d.Y + d.H * 0.72f, er = d.H * 0.09f;
            p.Circle(ex, ey, er, Palette.Hex("#3e8ef7"));
            p.Ellipse(ex - er * 0.3f, ey - er * 0.2f, er * 0.45f, er * 0.3f, Palette.Hex("#3ddc84"), 30);
            p.Ellipse(ex + er * 0.35f, ey + er * 0.35f, er * 0.3f, er * 0.2f, Palette.Hex("#3ddc84"), -10);

            p.Rect(d.X + d.W / 2 - 4, d.Y, 8, d.H, Palette.Hex("#a0612b"));
            p.Rect(d.X, d.Y + d.H / 2 - 4, d.W, 8, Palette.Hex("#a0612b"));

            var dark = Palette.Shade(curtain, -0.25f);
            p.Polygon(new[] { d.X - 24, d.Y - 22, d.X + d.W * 0.2f, d.Y - 22, d.X + d.W * 0.06f, d.Y + d.H * 0.55f, d.X - 4, d.Y + d.H + 22, d.X - 30, d.Y + d.H + 22 }, curtain);
            p.Polygon(new[] { d.X + d.W + 24, d.Y - 22, d.X + d.W * 0.8f, d.Y - 22, d.X + d.W * 0.94f, d.Y + d.H * 0.55f, d.X + d.W + 4, d.Y + d.H + 22, d.X + d.W + 30, d.Y + d.H + 22 }, curtain);
            for (int i = 1; i < 4; i++)
            {
                p.Line(d.X - 22 + i * 8, d.Y - 18, d.X - 24 + i * 6, d.Y + d.H + 18, 1.6f, dark);
                p.Line(d.X + d.W + 22 - i * 8, d.Y - 18, d.X + d.W + 24 - i * 6, d.Y + d.H + 18, 1.6f, dark);
            }
            p.RoundRect(d.X - 40, d.Y - 30, d.W + 80, 12, 6, Palette.Hex("#6e4529"));
            p.RoundRect(d.X - 18, d.Y + d.H + 8, d.W + 36, 14, 5, Ink);
            p.RoundRect(d.X - 16, d.Y + d.H + 10, d.W + 32, 10, 4, Palette.Hex("#c47a32"));
        }

        private static void Porthole(Paint p, float cx, float cy, float r, System.Random rng, bool planet)
        {
            p.Circle(cx, cy, r + 14, Ink);
            p.Circle(cx, cy, r + 11, Palette.Hex("#7b8496"));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                p.Circle(cx + Mathf.Cos(a) * (r + 5.5f), cy + Mathf.Sin(a) * (r + 5.5f), 2.6f, Palette.Hex("#4a5266"));
            }
            p.Circle(cx, cy, r, Palette.Hex("#0e0a26"));
            p.RadialGlow(cx - r * 0.3f, cy + r * 0.2f, r * 0.9f, new Color(0.7f, 0.3f, 0.9f, 0.35f), new Color(0.7f, 0.3f, 0.9f, 0f));
            p.RadialGlow(cx + r * 0.35f, cy - r * 0.3f, r * 0.6f, new Color(0.2f, 0.8f, 0.9f, 0.25f), new Color(0.2f, 0.8f, 0.9f, 0f));
            for (int i = 0; i < (int)(r * r / 90); i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rr = Mathf.Sqrt((float)rng.NextDouble()) * (r - 3);
                p.Circle(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr, 0.8f + (float)rng.NextDouble() * 1.4f, new Color(1, 1, 1, 0.6f + (float)rng.NextDouble() * 0.4f));
            }
            if (planet)
            {
                p.Circle(cx + r * 0.15f, cy + r * 0.2f, r * 0.5f, Palette.Hex("#2ec4d6"));
                p.Ellipse(cx + r * 0.05f, cy + r * 0.1f, r * 0.3f, r * 0.12f, Palette.Hex("#79f0c3"), 20);
                p.Ellipse(cx + r * 0.15f, cy + r * 0.2f, r * 0.85f, r * 0.12f, Palette.WithAlpha(Palette.Hex("#ffd9a8"), 0.8f), -12);
            }
            p.Arc(cx, cy, r - 6, 4, 200, 250, Palette.WithAlpha(Color.white, 0.35f));
        }

        // ------------------------------------------------------------------ tuberías

        private static void DrawPipe(Paint p, Pipe pipe)
        {
            // Semitransparentes: están en la pared del fondo y no se pisan.
            const float alpha = 0.62f;
            var c = Palette.WithAlpha(Palette.Shade(Palette.Hex(pipe.Color), -0.12f), alpha);
            var edge = Palette.WithAlpha(Palette.Shade(Palette.Hex(pipe.Color), -0.6f), alpha);
            var light = Palette.WithAlpha(Palette.Shade(Palette.Hex(pipe.Color), 0.4f), alpha);
            int n = pipe.PointCount;
            var pts = pipe.Path;
            // Se pinta en una capa aparte y se pega con transparencia, así los cruces no se suman.
            var layer = new Paint(p.Width, p.Height, p.Scale);
            var solidC = Palette.WithAlpha(c, 1f);
            var solidEdge = Palette.WithAlpha(edge, 1f);
            var solidLight = Palette.WithAlpha(light, 1f);
            for (int i = 1; i < n; i++) layer.Line(pts[i * 2 - 2], pts[i * 2 - 1], pts[i * 2], pts[i * 2 + 1], 15, solidEdge);
            for (int i = 1; i < n; i++) layer.Line(pts[i * 2 - 2], pts[i * 2 - 1], pts[i * 2], pts[i * 2 + 1], 11.5f, solidC);
            for (int i = 1; i < n; i++) layer.Line(pts[i * 2 - 2] - 4, pts[i * 2 - 1] - 4, pts[i * 2] - 4, pts[i * 2 + 1] - 4, 2.2f, solidLight);
            for (int i = 1; i < n - 1; i++)
            {
                layer.Circle(pts[i * 2], pts[i * 2 + 1], 17, solidEdge);
                layer.Circle(pts[i * 2], pts[i * 2 + 1], 14, Palette.Shade(solidC, -0.12f));
                layer.Circle(pts[i * 2] - 4, pts[i * 2 + 1] - 4, 3.5f, solidLight);
            }
            // Abrazaderas cada tanto para que se lea como un caño.
            for (int i = 1; i < n; i++)
            {
                float ax = pts[i * 2 - 2], ay = pts[i * 2 - 1], bx = pts[i * 2], by = pts[i * 2 + 1];
                float len = Mathf.Abs(bx - ax) + Mathf.Abs(by - ay);
                bool horizontal = Mathf.Abs(bx - ax) > Mathf.Abs(by - ay);
                for (float t = 160; t < len - 60; t += 220)
                {
                    float f = t / len;
                    float x = Mathf.Lerp(ax, bx, f), y = Mathf.Lerp(ay, by, f);
                    if (horizontal) layer.RoundRect(x - 5, y - 16, 10, 32, 3, solidEdge);
                    else layer.RoundRect(x - 16, y - 5, 32, 10, 3, solidEdge);
                }
            }
            p.StampFaded(layer, alpha);
        }

        private static void DrawMouth(Paint p, PipeEnd m, Color c)
        {
            float half = Tuning.MouthWidth / 2f;
            const float lip = 7f;
            int nx = Dir.X[m.Normal], ny = Dir.Y[m.Normal];
            float x, y, w, h;
            if (nx == 0)
            {
                x = m.X - half; w = Tuning.MouthWidth; h = lip + 4;
                y = ny < 0 ? m.Y - lip : m.Y - 4;
            }
            else
            {
                y = m.Y - half; h = Tuning.MouthWidth; w = lip + 4;
                x = nx < 0 ? m.X - lip : m.X - 4;
            }
            p.RoundRect(x - 2.5f, y - 2.5f, w + 5, h + 5, 5, Ink);
            p.RoundRect(x, y, w, h, 4, c);
            p.RoundRect(x + 2, y + 2, nx == 0 ? w - 4 : 3, ny == 0 ? h - 4 : 3, 1.5f, Palette.WithAlpha(Palette.Shade(c, 0.45f), 0.8f));
            float hx = m.X + nx * (lip * 0.4f), hy = m.Y + ny * (lip * 0.4f);
            if (nx == 0) p.Ellipse(hx, hy, half - 9, 3.4f, Palette.Hex("#120a1e"));
            else p.Ellipse(hx, hy, 3.4f, half - 9, Palette.Hex("#120a1e"));
        }

        // ------------------------------------------------------------------ bloques

        private static void DrawSolid(Paint p, Level level, Solid s, System.Random rng)
        {
            switch (s.Kind)
            {
                case "piso": Floor(p, level.Theme, s, rng); return;
                case "techo": Ceiling(p, level.Theme, s); return;
                case "pared": Wall(p, level.Theme, s); return;
            }

            switch (s.Kind)
            {
                case "mesa":
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 8, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 6, Palette.Hex("#f4f1de"));
                    for (float x = s.X + 4; x < s.Right - 8; x += 24)
                    {
                        p.Rect(x, s.Y + 2, 12, s.H * 0.5f - 2, Palette.WithAlpha(Palette.Hex("#e5484d"), 0.85f));
                        p.Rect(x + 12, s.Y + s.H * 0.5f, 12, s.H * 0.5f - 2, Palette.WithAlpha(Palette.Hex("#e5484d"), 0.85f));
                    }
                    p.Rect(s.X + 4, s.Y + 2, s.W - 8, 3, Palette.WithAlpha(Color.white, 0.6f));
                    break;
                case "estante":
                case "colgante":
                case "biblioteca":
                    Wood(p, s, Palette.Hex("#b8742f"));
                    if (s.Kind == "estante")
                    {
                        Bracket(p, s.X + s.W * 0.3f, s.Bottom);
                        Bracket(p, s.X + s.W * 0.75f, s.Bottom);
                    }
                    break;
                case "mesita":
                    Wood(p, s, Palette.Hex("#a0612b"));
                    break;
                case "ventilador":
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 10, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 8, Palette.Hex("#8a5a3b"));
                    p.Rect(s.X + 8, s.Y + 3, s.W - 16, 3, Palette.WithAlpha(Color.white, 0.35f));
                    p.Circle(s.X + s.W / 2, s.Y + s.H / 2, 16, Ink);
                    p.Circle(s.X + s.W / 2, s.Y + s.H / 2, 13, Palette.Hex("#c9a36b"));
                    break;
                case "sofa":
                {
                    var blue = Palette.Hex("#3e6fd8");
                    p.RoundRect(s.X - 4, s.Y - 104, s.W + 8, 130, 30, Ink);
                    p.RoundRect(s.X, s.Y - 100, s.W, 122, 26, Palette.Shade(blue, -0.18f));
                    for (float x = s.X + s.W / 3; x < s.Right - 10; x += s.W / 3) p.Line(x, s.Y - 86, x, s.Y - 8, 1.6f, Palette.Shade(blue, -0.35f));
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 20, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 18, blue);
                    p.Rect(s.X + 14, s.Y + 4, s.W - 28, 4, Palette.WithAlpha(Color.white, 0.35f));
                    for (float x = s.X + s.W / 3; x < s.Right - 10; x += s.W / 3) p.Line(x, s.Y + 6, x, s.Y + 40, 1.4f, Palette.Shade(blue, -0.3f));
                    p.Rect(s.X + 10, s.Y + 48, s.W - 20, 3, Palette.Shade(blue, -0.3f));
                    break;
                }
                case "caldera":
                {
                    var red = Palette.Hex("#c4472b");
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 20, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 18, red);
                    p.Rect(s.X + 6, s.Y + 4, s.W - 12, 5, Palette.WithAlpha(Color.white, 0.3f));
                    for (float y = s.Y + 30; y < s.Bottom - 10; y += 34)
                    {
                        p.Rect(s.X + 2, y, s.W - 4, 4, Palette.Shade(red, -0.3f));
                        for (float x = s.X + 14; x < s.Right - 10; x += 24) p.Circle(x, y + 2, 2.2f, Palette.Shade(red, 0.3f));
                    }
                    float gx = s.X + 40, gy = s.Y + 45;
                    p.Circle(gx, gy, 19, Ink);
                    p.Circle(gx, gy, 16, Palette.Hex("#fbe6bd"));
                    p.Line(gx, gy, gx + 9, gy - 9, 1.8f, Palette.Hex("#e5484d"));
                    p.Circle(gx, gy, 2.6f, Ink);
                    float fx = s.X + s.W / 2 + 20, fy = s.Bottom - 46;
                    p.RoundRect(fx - 32, fy - 18, 64, 38, 10, Ink);
                    p.RoundRect(fx - 28, fy - 14, 56, 30, 8, Palette.Hex("#3a0d0d"));
                    p.RadialGlow(fx, fy + 6, 30, new Color(1f, 0.75f, 0.2f, 0.95f), new Color(1f, 0.3f, 0.1f, 0f));
                    for (int i = 0; i < 4; i++) p.Line(fx - 21 + i * 14, fy - 12, fx - 21 + i * 14, fy + 14, 1.6f, Ink);
                    break;
                }
                case "reja":
                case "repisa":
                {
                    var steel = Palette.Hex(s.Kind == "reja" ? "#5b6273" : "#4d5a7c");
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 6, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 5, steel);
                    p.Rect(s.X + 4, s.Y + 2, s.W - 8, 3, Palette.WithAlpha(Color.white, 0.35f));
                    if (s.Kind == "reja")
                        for (float x = s.X + 10; x < s.Right - 12; x += 16) p.RoundRect(x, s.Y + 9, 9, s.H - 15, 2, Palette.Hex("#2e3242"));
                    else
                        for (float x = s.X + 12; x < s.Right - 8; x += 30) p.Circle(x, s.Y + s.H / 2 + 2, 2.6f, Palette.Hex("#7b8bab"));
                    break;
                }
                case "plataforma":
                {
                    var steel = Palette.Hex("#4d5a7c");
                    p.RoundRect(s.X + 16, s.Bottom, s.W - 32, 18, 9, new Color(0.18f, 0.77f, 0.84f, 0.18f));
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 10, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 8, steel);
                    p.Rect(s.X + 8, s.Y + 3, s.W - 16, 3, Palette.WithAlpha(Color.white, 0.35f));
                    p.RoundRect(s.X + 14, s.Bottom - 8, s.W - 28, 4, 2, Palette.Hex("#2ec4d6"));
                    break;
                }
                case "pilar":
                {
                    p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 8, Ink);
                    p.RoundRect(s.X, s.Y, s.W, s.H, 6, Palette.Hex("#ffd23f"));
                    for (float y = s.Y - 14; y < s.Bottom; y += 28)
                    {
                        float Clip(float v) => Mathf.Clamp(v, s.Y, s.Bottom);
                        p.Polygon(new[] { s.X, Clip(y + 14), s.Right, Clip(y), s.Right, Clip(y + 12), s.X, Clip(y + 26) }, Palette.Hex("#2a2a35"));
                    }
                    p.Rect(s.X, s.Y - 4, s.W, 4, Ink);
                    p.Rect(s.X, s.Bottom, s.W, 4, Ink);
                    break;
                }
                default:
                    p.RoundRect(s.X, s.Y, s.W, s.H, 6, Palette.Hex("#7b8496"));
                    break;
            }
        }

        private static void Wood(Paint p, Solid s, Color c)
        {
            p.RoundRect(s.X - 3, s.Y - 3, s.W + 6, s.H + 6, 7, Ink);
            p.RoundRect(s.X, s.Y, s.W, s.H, 5, c);
            p.Rect(s.X + 4, s.Y + 2, s.W - 8, 3, Palette.WithAlpha(Color.white, 0.35f));
            p.Line(s.X + 20, s.Y + s.H * 0.62f, s.Right - 60, s.Y + s.H * 0.62f, 0.9f, Palette.Shade(c, -0.25f));
        }

        private static void Bracket(Paint p, float x, float y)
        {
            p.Polygon(new[] { x - 14, y - 1, x + 14, y - 1, x - 8, y + 26 }, Ink);
            p.Polygon(new[] { x - 10, y, x + 8, y, x - 7, y + 20 }, Palette.Hex("#8a4f24"));
        }

        private static void Floor(Paint p, string theme, Solid s, System.Random rng)
        {
            switch (theme)
            {
                case "sotano":
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#6d6a75"));
                    for (int i = 0; i < 160; i++)
                        p.Circle(s.X + (float)rng.NextDouble() * s.W, s.Y + 6 + (float)rng.NextDouble() * (s.H - 6), 1.2f, Palette.Hex("#57545f"));
                    p.Line(300, s.Y + 10, 330, s.Y + 30, 1, Palette.Hex("#4a4752"));
                    p.Line(1100, s.Y + 8, 1150, s.Y + 24, 1, Palette.Hex("#4a4752"));
                    break;
                case "estacion":
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#3d4866"));
                    Hazard(p, s.X, s.Y + 3, s.W, 9);
                    break;
                case "living":
                    for (int row = 0; row < 2; row++)
                        for (float x = row * -60; x < s.W; x += 120)
                        {
                            var c = (int)(x / 120 + row) % 2 == 0 ? Palette.Hex("#a86b3c") : Palette.Hex("#b97a47");
                            p.Rect(x + 1, s.Y + row * 25, 118, 24, c);
                        }
                    break;
                default:
                    for (float x = 0; x < s.W; x += 50)
                        for (int row = 0; row < 2; row++)
                            p.Rect(x, s.Y + row * 25, 50, 25, ((int)(x / 50) + row) % 2 == 0 ? Palette.Hex("#2b2440") : Palette.Hex("#f4efe6"));
                    break;
            }
            p.Rect(s.X, s.Y, s.W, 3, Ink);
            p.Rect(s.X, s.Y + 3, s.W, 3, Palette.WithAlpha(Color.white, 0.25f));
        }

        private static void Ceiling(Paint p, string theme, Solid s)
        {
            switch (theme)
            {
                case "sotano":
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#4a4655"));
                    for (float x = 60; x < s.W; x += 160) p.Circle(x, s.Y + s.H / 2, 3, Palette.Hex("#6d6a75"));
                    break;
                case "estacion":
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#3d4866"));
                    Hazard(p, s.X, s.Bottom - 12, s.W, 9);
                    break;
                case "living":
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#f4ead8"));
                    p.Rect(s.X, s.Bottom - 12, s.W, 6, Palette.Hex("#d9c9a8"));
                    break;
                default:
                    p.Rect(s.X, s.Y, s.W, s.H, Palette.Hex("#8a4f24"));
                    for (float x = 0; x < s.W; x += 120) p.Rect(x, s.Y, 3, s.H, Palette.Hex("#6e3d1b"));
                    break;
            }
            p.Rect(s.X, s.Bottom - 3, s.W, 3, Ink);
        }

        private static void Wall(Paint p, string theme, Solid s)
        {
            Color c;
            switch (theme)
            {
                case "sotano": c = Palette.Hex("#4a3040"); break;
                case "estacion": c = Palette.Hex("#333e5c"); break;
                case "living": c = Palette.Hex("#c99248"); break;
                default: c = Palette.Hex("#d98a5a"); break;
            }
            p.Rect(s.X, s.Y, s.W, s.H, c);
            for (float y = 0; y < s.H; y += 60) p.Rect(s.X, y, s.W, 2, Palette.Shade(c, -0.2f));
            bool left = s.X < 1f;
            p.Rect(left ? s.Right - 3 : s.X, s.Y, 3, s.H, Ink);
            p.Rect(left ? s.Right - 6 : s.X + 3, s.Y, 3, s.H, Palette.WithAlpha(Color.white, 0.2f));
        }

        private static void Hazard(Paint p, float x, float y, float w, float h)
        {
            p.Rect(x, y, w, h, Palette.Cheese);
            for (float sx = x; sx < x + w; sx += 24)
                p.Polygon(new[] { sx, y + h, sx + 10, y, sx + 20, y, sx + 10, y + h }, Palette.Hex("#2a2a35"));
        }
    }
}
