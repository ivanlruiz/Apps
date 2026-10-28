using System;
using System.Collections.Generic;
using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>Dónde van los ojos, la boca y los accesorios de cada bichito (unidades, Y hacia abajo, centro del cuerpo = 0,0).</summary>
    public sealed class SpeciesRig
    {
        public Vector2[] Eyes = { Vector2.zero };
        public float EyeRadius = 4f;
        public bool DotEyes;
        public Vector2 Mouth = new Vector2(0f, 6f);
        public float HeadTop = -15f;
        public float GlassesY = -3f;
        public bool Antenna;
        public bool Bubble;
        public bool AlwaysBrows;
    }

    /// <summary>
    /// Todo el arte del juego se dibuja desde código con <see cref="Paint"/>, así el proyecto anda sin
    /// importar imágenes. Cada parte es un sprite separado para poder animarla (ojos, antena, ruedas).
    /// Para cambiar el arte por dibujos propios, alcanza con reemplazar lo que devuelven estas funciones.
    /// </summary>
    public static class Art
    {
        /// <summary>Píxeles de textura por unidad para personajes y objetos chicos.</summary>
        public const float S = 2f;

        public const float Outline = 2f;

        /// <summary>Altura del centro del bichito respecto del centro del karting.</summary>
        public static float SeatY(string style) => style == "tostadora" ? -25f : -12f;
        public const float SeatX = -3f;
        public const float FoodY = -50f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Color Pink = Palette.Hex("#ff8fb1");
        private static readonly Color Mouth = Palette.Hex("#5a1530");
        private static readonly Color Chrome = Palette.Hex("#d6dbe4");
        private static readonly Color Steel = Palette.Hex("#9aa3b5");
        private static readonly Color Tire = Palette.Hex("#222230");

        private static string Key(Color c) => ColorUtility.ToHtmlStringRGBA(c);

        private static Sprite Make(string key, float x0, float y0, float w, float h, Action<Paint> draw, float pivotX = 0f, float pivotY = 0f, float scale = S)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = Paint.ForUnits(x0, y0, w, h, scale);
            draw(p);
            s = p.ToSprite(pivotX, pivotY);
            s.name = key;
            Cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ bichitos

        public static SpeciesRig Rig(string species)
        {
            switch (species)
            {
                case "zorbo":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-7, -3), new Vector2(0, -5), new Vector2(7, -3) }, EyeRadius = 4.2f, Mouth = new Vector2(0, 6), HeadTop = -14, GlassesY = -4 };
                case "pulpi":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-6, -2), new Vector2(6, -2) }, EyeRadius = 4.5f, Mouth = new Vector2(0, 6), HeadTop = -16, GlassesY = -2 };
                case "moco":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-9, -20), new Vector2(9, -20) }, EyeRadius = 4f, Mouth = new Vector2(0, 7), HeadTop = -24, GlassesY = -20 };
                case "chispa":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-4, 0), new Vector2(4, 0) }, EyeRadius = 3.4f, Mouth = new Vector2(0, 6), HeadTop = -16, GlassesY = 0 };
                case "glub":
                    return new SpeciesRig { Eyes = new[] { new Vector2(5, 0) }, EyeRadius = 3.6f, Mouth = new Vector2(11, 3), HeadTop = -18, GlassesY = 0, Bubble = true };
                case "cometa":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-4, -2), new Vector2(5, -2) }, EyeRadius = 3.8f, Mouth = new Vector2(1, 6), HeadTop = -14, GlassesY = -2, AlwaysBrows = true };
                case "noqui":
                    return new SpeciesRig { Eyes = new[] { new Vector2(-5, -3), new Vector2(5, -3) }, EyeRadius = 2.6f, DotEyes = true, Mouth = new Vector2(0, 4), HeadTop = -13, GlassesY = -3 };
                default:
                    return new SpeciesRig { Eyes = new[] { new Vector2(0, -3) }, EyeRadius = 7f, Mouth = new Vector2(0, 7), HeadTop = -15, GlassesY = -3, Antenna = true };
            }
        }

        public static Sprite Body(string species, Color c)
        {
            return Make("body_" + species + Key(c), -30, -34, 60, 60, p => DrawBody(p, species, c, 0, 0));
        }

        public static Sprite Antenna(Color c)
        {
            return Make("antenna" + Key(c), -8, -22, 16, 24, p => DrawAntenna(p, c, 0, 0));
        }

        public static Sprite Bubble()
        {
            return Make("bubble", -22, -22, 44, 44, p => DrawBubble(p, 0, 0));
        }

        public static Sprite EyeWhite(float r, bool dot)
        {
            return Make($"eye{r}{dot}", -r - 3, -r - 3, r * 2 + 6, r * 2 + 6, p => DrawEyeWhite(p, 0, 0, r, dot));
        }

        public static Sprite Pupil(float r, bool dot)
        {
            return Make($"pupil{r}{dot}", -r, -r, r * 2, r * 2, p => DrawPupil(p, 0, 0, r, dot));
        }

        public static Sprite HappyEye(float r)
        {
            return Make($"happy{r}", -r - 3, -r - 3, r * 2 + 6, r * 2 + 6, p => p.Arc(0, r * 0.35f, r * 0.8f, 2.2f, 200, 340, Palette.Ink));
        }

        public static Sprite DizzyEye(float r)
        {
            return Make($"dizzy{r}", -r, -r, r * 2, r * 2, p =>
            {
                float px = 0, py = 0;
                for (int i = 1; i < 14; i++)
                {
                    float a = i * 0.75f;
                    float rad = r * 0.08f * i;
                    float nx = Mathf.Cos(a) * rad, ny = Mathf.Sin(a) * rad;
                    p.Line(px, py, nx, ny, 0.7f, Palette.Ink);
                    px = nx; py = ny;
                }
            });
        }

        public static Sprite Brow()
        {
            return Make("brow", -6, -3, 12, 6, p => p.Line(-4, 0, 4, 0, 1.4f, Palette.Ink));
        }

        public static Sprite MouthSprite(string kind)
        {
            return Make("mouth_" + kind, -8, -7, 16, 14, p => DrawMouth(p, kind, 0, 0));
        }

        public static Sprite Hat(string hat)
        {
            return Make("hat_" + hat, -30, -34, 60, 50, p => DrawHat(p, hat, 0, 0));
        }

        /// <summary>Dónde va el accesorio respecto del centro del bichito.</summary>
        public static Vector2 HatAnchor(string hat, SpeciesRig rig)
        {
            switch (hat)
            {
                case "casco": return Vector2.zero;
                case "anteojos": return new Vector2(0, rig.GlassesY);
                default: return new Vector2(0, rig.HeadTop + 1f);
            }
        }

        private static void Outlined(Paint p, float[] pts, Color fill, float width = Outline)
        {
            p.Polygon(pts, Palette.Ink);
            int n = pts.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
                p.Line(pts[j * 2], pts[j * 2 + 1], pts[i * 2], pts[i * 2 + 1], width, Palette.Ink);
            p.Polygon(pts, fill);
        }

        private static float[] Offset(float[] pts, float ox, float oy)
        {
            var r = new float[pts.Length];
            for (int i = 0; i < pts.Length; i += 2) { r[i] = pts[i] + ox; r[i + 1] = pts[i + 1] + oy; }
            return r;
        }

        private static void DrawBody(Paint p, string species, Color c, float ox, float oy)
        {
            var ink = Palette.Ink;
            var light = Palette.Shade(c, 0.35f);
            var dark = Palette.Shade(c, -0.2f);
            switch (species)
            {
                case "zorbo":
                    Outlined(p, Offset(new float[] { -11, -8, -4, -11, -10, -22 }, ox, oy), dark);
                    Outlined(p, Offset(new float[] { 11, -8, 4, -11, 10, -22 }, ox, oy), dark);
                    p.Ellipse(ox, oy, 17 + Outline, 14 + Outline, ink);
                    p.Ellipse(ox, oy, 17, 14, c);
                    p.Ellipse(ox - 5, oy - 7, 6, 3.5f, Palette.WithAlpha(light, 0.6f), -15);
                    p.Circle(ox - 11, oy + 3, 1.8f, dark);
                    p.Circle(ox + 12, oy + 1, 1.4f, dark);
                    p.Circle(ox + 9, oy + 8, 1.2f, dark);
                    break;

                case "pulpi":
                    float[][] arms = { new float[] { -9, 6, -14, 17 }, new float[] { -3, 8, -5, 19 }, new float[] { 3, 8, 5, 19 }, new float[] { 9, 6, 14, 17 } };
                    foreach (var a in arms) p.Line(ox + a[0], oy + a[1], ox + a[2], oy + a[3], 3.2f + Outline, ink);
                    foreach (var a in arms) p.Line(ox + a[0], oy + a[1], ox + a[2], oy + a[3], 3.2f, c);
                    p.Ellipse(ox, oy - 2, 16 + Outline, 15 + Outline, ink);
                    p.Ellipse(ox, oy - 2, 16, 15, c);
                    p.Circle(ox - 7, oy - 10, 2.4f, light);
                    p.Circle(ox + 4, oy - 13, 1.6f, light);
                    p.Circle(ox + 9, oy - 7, 2f, light);
                    p.Circle(ox - 9, oy + 4, 2.4f, Palette.WithAlpha(Pink, 0.55f));
                    p.Circle(ox + 9, oy + 4, 2.4f, Palette.WithAlpha(Pink, 0.55f));
                    break;

                case "moco":
                    p.Line(ox - 4, oy - 6, ox - 9, oy - 19, 2f + Outline, ink);
                    p.Line(ox + 4, oy - 6, ox + 9, oy - 19, 2f + Outline, ink);
                    p.Line(ox - 4, oy - 6, ox - 9, oy - 19, 2f, c);
                    p.Line(ox + 4, oy - 6, ox + 9, oy - 19, 2f, c);
                    p.Polygon(Drop(ox, oy, 14 + Outline), ink);
                    p.Polygon(Drop(ox, oy, 14), c);
                    p.Ellipse(ox - 6, oy - 1, 2.4f, 5f, Palette.WithAlpha(Color.white, 0.5f), 20);
                    p.Line(ox + 9, oy + 13, ox + 10, oy + 18, 2.2f, light);
                    break;

                case "chispa":
                    p.Polygon(Paint.StarPoints(ox, oy, 5, 17 + Outline * 1.6f, 9 + Outline), ink);
                    p.Polygon(Paint.StarPoints(ox, oy, 5, 17, 9), c);
                    p.Polygon(Paint.StarPoints(ox - 2, oy - 2, 5, 7, 3.8f), Palette.WithAlpha(light, 0.7f));
                    break;

                case "glub":
                    Outlined(p, Offset(new float[] { -9, 2, -18, -6, -15, 2, -18, 10 }, ox, oy), dark);
                    Outlined(p, Offset(new float[] { -3, -5, 3, -12, 6, -5 }, ox, oy), dark);
                    p.Ellipse(ox + 1, oy + 2, 12 + Outline, 9 + Outline, ink);
                    p.Ellipse(ox + 1, oy + 2, 12, 9, c);
                    p.Ellipse(ox + 2, oy + 6, 8, 3f, Palette.WithAlpha(light, 0.7f));
                    p.Arc(ox - 3, oy + 2, 4, 1.4f, -60, 60, Palette.Shade(c, -0.35f));
                    break;

                case "cometa":
                    Outlined(p, Offset(new float[] { -6, -11, -27, -22, -16, -9, -29, -5, -12, 3 }, ox, oy), Palette.Hex("#ff9f1c"));
                    p.Polygon(Offset(new float[] { -7, -8, -20, -15, -14, -6, -22, -4, -10, 1 }, ox, oy), Palette.Hex("#ffd23f"));
                    p.Circle(ox, oy, 14 + Outline, ink);
                    p.Circle(ox, oy, 14, c);
                    p.Ellipse(ox - 4, oy - 6, 5, 3, Palette.WithAlpha(light, 0.6f), -20);
                    break;

                case "noqui":
                    p.Ellipse(ox, oy, 15 + Outline, 13 + Outline, ink);
                    p.Ellipse(ox, oy, 15, 13, c);
                    var groove = Palette.Shade(c, -0.25f);
                    for (int i = 0; i < 3; i++) p.Arc(ox, oy + 30, 21 + i * 4.5f, 1.5f, 245, 295, groove);
                    p.Circle(ox - 9, oy + 2, 2.4f, Palette.WithAlpha(Pink, 0.6f));
                    p.Circle(ox + 9, oy + 2, 2.4f, Palette.WithAlpha(Pink, 0.6f));
                    p.Circle(ox - 6, oy - 9, 1.1f, Palette.Cheese);
                    p.Circle(ox + 3, oy - 11, 1f, Palette.Cheese);
                    p.Circle(ox + 8, oy - 7, 1.1f, Palette.Cheese);
                    break;

                default: // blip
                    p.Circle(ox, oy, 15 + Outline, ink);
                    p.Circle(ox, oy, 15, c);
                    p.Ellipse(ox - 5, oy - 6, 6, 4, Palette.WithAlpha(light, 0.65f), -20);
                    p.Circle(ox - 9, oy + 5, 2.4f, Palette.WithAlpha(Pink, 0.55f));
                    p.Circle(ox + 9, oy + 5, 2.4f, Palette.WithAlpha(Pink, 0.55f));
                    break;
            }
        }

        private static float[] Drop(float ox, float oy, float r)
        {
            const int n = 36;
            var pts = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n * Mathf.PI * 2f;
                float up = Mathf.Max(0f, -Mathf.Sin(t));
                pts[i * 2] = ox + r * Mathf.Cos(t) * (1f - 0.35f * up * up);
                pts[i * 2 + 1] = oy + 3 + r * Mathf.Sin(t) - r * 0.5f * up * up * up;
            }
            return pts;
        }

        private static void DrawAntenna(Paint p, Color c, float bx, float by)
        {
            p.Line(bx, by, bx, by - 13, 1.4f, Palette.Ink);
            p.Circle(bx, by - 15, 4.6f, Palette.Ink);
            p.Circle(bx, by - 15, 3.2f, Palette.Shade(c, 0.45f));
        }

        private static void DrawBubble(Paint p, float cx, float cy)
        {
            p.Circle(cx, cy, 18, Palette.WithAlpha(Palette.Hex("#bfe9ff"), 0.18f));
            p.Ring(cx, cy, 18, 1.6f, Palette.WithAlpha(Color.white, 0.8f));
            p.Arc(cx, cy, 14, 2.4f, 200, 250, Palette.WithAlpha(Color.white, 0.85f));
            p.Circle(cx + 9, cy - 9, 1.4f, Palette.WithAlpha(Color.white, 0.7f));
        }

        private static void DrawEyeWhite(Paint p, float x, float y, float r, bool dot)
        {
            if (dot)
            {
                p.Circle(x, y, r, Palette.Ink);
                p.Circle(x - r * 0.3f, y - r * 0.3f, r * 0.38f, Color.white);
                return;
            }
            p.Circle(x, y, r + 1.6f, Palette.Ink);
            p.Circle(x, y, r, Color.white);
        }

        private static void DrawPupil(Paint p, float x, float y, float r, bool dot)
        {
            if (dot) return;
            float pr = r * 0.55f;
            p.Circle(x, y, pr, Palette.Ink);
            p.Circle(x - pr * 0.35f, y - pr * 0.35f, pr * 0.35f, Color.white);
        }

        private static void DrawMouth(Paint p, string kind, float x, float y)
        {
            var ink = Palette.Ink;
            switch (kind)
            {
                case "grin":
                {
                    const int n = 12;
                    var pts = new float[(n + 1) * 2 + 2];
                    pts[0] = x - 5; pts[1] = y - 1.5f;
                    for (int i = 0; i <= n; i++)
                    {
                        float a = Mathf.PI * i / n;
                        pts[2 + i * 2] = x - Mathf.Cos(a) * 5f;
                        pts[3 + i * 2] = y - 1.5f + Mathf.Sin(a) * 5f;
                    }
                    Outlined(p, pts, Mouth, 1.1f);
                    p.Circle(x + 1, y + 2.2f, 2f, Pink);
                    break;
                }
                case "open":
                    p.Ellipse(x, y, 2.8f, 3.4f, ink);
                    p.Ellipse(x, y + 1f, 1.6f, 1.4f, Pink);
                    break;
                case "flat":
                    p.Line(x - 3.5f, y, x + 3.5f, y, 1.1f, ink);
                    break;
                case "wavy":
                    p.Line(x - 4, y, x - 2, y - 1.5f, 0.9f, ink);
                    p.Line(x - 2, y - 1.5f, x, y, 0.9f, ink);
                    p.Line(x, y, x + 2, y - 1.5f, 0.9f, ink);
                    p.Line(x + 2, y - 1.5f, x + 4, y, 0.9f, ink);
                    break;
                default:
                    p.Arc(x, y - 2.5f, 4.5f, 2f, 25, 155, ink);
                    break;
            }
        }

        private static void DrawHat(Paint p, string hat, float ax, float ay)
        {
            var ink = Palette.Ink;
            switch (hat)
            {
                case "casco":
                    p.Circle(ax, ay - 2, 21, Palette.WithAlpha(Palette.Hex("#bfe9ff"), 0.2f));
                    p.Ring(ax, ay - 2, 21, 1.8f, Palette.WithAlpha(Color.white, 0.85f));
                    p.Arc(ax, ay - 2, 16, 2.6f, 200, 245, Palette.WithAlpha(Color.white, 0.9f));
                    p.Circle(ax + 12, ay - 12, 1.5f, Palette.WithAlpha(Color.white, 0.8f));
                    break;
                case "fiesta":
                    Outlined(p, Offset(new float[] { -7, 1, 7, 1, 1, -19 }, ax, ay), Palette.Hex("#ff5d8f"));
                    p.Line(ax - 4, ay - 5, ax + 4, ay - 3, 1.2f, Palette.Cheese);
                    p.Line(ax - 2, ay - 11, ax + 3, ay - 10, 1.1f, Palette.Alien);
                    p.Circle(ax + 1, ay - 20, 3.6f, ink);
                    p.Circle(ax + 1, ay - 20, 2.6f, Color.white);
                    break;
                case "mono":
                {
                    var red = Palette.Hex("#ff4f6d");
                    Outlined(p, Offset(new float[] { 7, 1, 0, -4, 0, 6 }, ax, ay), red, 1.4f);
                    Outlined(p, Offset(new float[] { 7, 1, 14, -4, 14, 6 }, ax, ay), red, 1.4f);
                    p.Circle(ax + 7, ay + 1, 3.2f, ink);
                    p.Circle(ax + 7, ay + 1, 2.2f, Palette.Shade(red, -0.15f));
                    break;
                }
                case "anteojos":
                    p.RoundRect(ax - 12, ay - 3, 10, 7, 2.5f, ink);
                    p.RoundRect(ax + 2, ay - 3, 10, 7, 2.5f, ink);
                    p.Line(ax - 2, ay - 1.5f, ax + 2, ay - 1.5f, 1f, ink);
                    p.Line(ax - 9.5f, ay - 1, ax - 6.5f, ay - 2, 0.8f, Palette.WithAlpha(Color.white, 0.6f));
                    p.Line(ax + 4.5f, ay - 1, ax + 7.5f, ay - 2, 0.8f, Palette.WithAlpha(Color.white, 0.6f));
                    break;
                case "chef":
                    p.Circle(ax - 6, ay - 11, 5.5f + Outline, ink);
                    p.Circle(ax, ay - 14, 6.5f + Outline, ink);
                    p.Circle(ax + 6, ay - 11, 5.5f + Outline, ink);
                    p.RoundRect(ax - 9 - Outline, ay - 8 - Outline, 18 + Outline * 2, 9 + Outline * 2, 3, ink);
                    p.Circle(ax - 6, ay - 11, 5.5f, Color.white);
                    p.Circle(ax, ay - 14, 6.5f, Color.white);
                    p.Circle(ax + 6, ay - 11, 5.5f, Color.white);
                    p.RoundRect(ax - 9, ay - 8, 18, 9, 2, Palette.Hex("#f1f1f7"));
                    break;
                case "corona":
                    Outlined(p, Offset(new float[] { -10, 1, 10, 1, 11, -11, 5, -5, 0, -13, -5, -5, -11, -11 }, ax, ay), Palette.Cheese);
                    p.Circle(ax, ay - 4, 1.8f, Palette.Hex("#e5484d"));
                    p.Circle(ax - 6, ay - 2, 1.3f, Palette.Hex("#3e8ef7"));
                    p.Circle(ax + 6, ay - 2, 1.3f, Palette.Hex("#30a46c"));
                    break;
                case "colador":
                {
                    p.Line(ax - 13, ay + 1, ax - 28, ay - 4, 1.8f + Outline, ink);
                    p.Line(ax - 13, ay + 1, ax - 28, ay - 4, 1.8f, Steel);
                    const int n = 14;
                    var bowl = new float[(n + 1) * 2];
                    for (int i = 0; i <= n; i++)
                    {
                        float a = Mathf.PI + Mathf.PI * i / n;
                        bowl[i * 2] = ax + Mathf.Cos(a) * 14f;
                        bowl[i * 2 + 1] = ay + 3 + Mathf.Sin(a) * 11f;
                    }
                    Outlined(p, bowl, Palette.Hex("#c9d1dc"));
                    for (int i = -2; i <= 2; i++)
                    {
                        p.Circle(ax + i * 4.5f, ay - 2, 0.9f, Palette.Ink);
                        if (Mathf.Abs(i) < 2) p.Circle(ax + i * 4.5f + 2.2f, ay - 6, 0.9f, Palette.Ink);
                    }
                    p.Line(ax - 16, ay + 3, ax + 16, ay + 3, 1.6f, ink);
                    p.Line(ax - 15, ay + 3, ax + 15, ay + 3, 0.9f, Steel);
                    break;
                }
            }
        }

        /// <summary>Dibuja un bichito completo (cuerpo, ojos, boca, accesorio) con su centro en (ox, oy).</summary>
        private static void DrawBichito(Paint p, Look look, float ox, float oy, string mouth = "smile")
        {
            var rig = Rig(look.species);
            var c = Palette.Hex(look.body);
            if (rig.Antenna) DrawAntenna(p, c, ox, oy - 14);
            DrawBody(p, look.species, c, ox, oy);
            foreach (var e in rig.Eyes)
            {
                DrawEyeWhite(p, ox + e.x, oy + e.y, rig.EyeRadius, rig.DotEyes);
                DrawPupil(p, ox + e.x + rig.EyeRadius * 0.2f, oy + e.y, rig.EyeRadius, rig.DotEyes);
            }
            if (rig.AlwaysBrows)
                foreach (var e in rig.Eyes)
                    p.Line(ox + e.x - 3, oy + e.y - rig.EyeRadius - 3.5f, ox + e.x + 3, oy + e.y - rig.EyeRadius - 1.5f, 1.3f, Palette.Ink);
            DrawMouth(p, mouth, ox + rig.Mouth.x, oy + rig.Mouth.y);
            if (look.hat != "casco" && look.hat != "nada")
            {
                var a = HatAnchor(look.hat, rig);
                DrawHat(p, look.hat, ox + a.x, oy + a.y);
            }
            if (rig.Bubble) DrawBubble(p, ox, oy);
            if (look.hat == "casco") DrawHat(p, "casco", ox, oy);
        }

        // ------------------------------------------------------------------ karting

        public static Sprite KartBack(string style, Color c)
        {
            return Make("kback_" + style + Key(c), -42, -30, 84, 50, p => DrawKartBack(p, style, c, 0, 0));
        }

        public static Sprite KartFront(string style, Color c, int number)
        {
            return Make($"kfront_{style}{number}{Key(c)}", -42, -30, 84, 50, p => DrawKartFront(p, style, c, number, 0, 0));
        }

        public static Sprite Wheel()
        {
            return Make("wheel", -12, -12, 24, 24, p => DrawWheel(p, 0, 0));
        }

        public static Sprite Flame()
        {
            return Make("flame", -18, -8, 20, 16, p =>
            {
                Outlined(p, new float[] { 0, -5, -15, 0, 0, 5 }, Palette.Hex("#ff9f1c"), 1.2f);
                p.Polygon(new float[] { 0, -2.5f, -8, 0, 0, 2.5f }, Palette.Cheese);
            });
        }

        private static void DrawKartBack(Paint p, string style, Color c, float ox, float oy)
        {
            var ink = Palette.Ink;
            switch (style)
            {
                case "cohete":
                    Outlined(p, Offset(new float[] { -20, -3, -33, -17, -28, -3 }, ox, oy), Palette.Shade(c, -0.2f));
                    p.RoundRect(ox - 37 - Outline, oy - 1 - Outline, 9 + Outline * 2, 11 + Outline * 2, 3, ink);
                    p.RoundRect(ox - 37, oy - 1, 9, 11, 2, Palette.Hex("#7b8496"));
                    break;
                case "tostadora":
                    p.Line(ox - 26, oy + 6, ox - 35, oy + 9, 1.2f, ink);
                    p.RoundRect(ox - 40, oy + 6, 6, 6, 1.5f, Steel);
                    break;
                default:
                    p.RoundRect(ox - 25 - Outline, oy - 20 - Outline, 9 + Outline * 2, 22 + Outline * 2, 4, ink);
                    p.RoundRect(ox - 25, oy - 20, 9, 22, 3, Palette.Shade(c, -0.35f));
                    p.Line(ox - 27, oy - 2, ox - 30, oy - 14, 1.6f, ink);
                    p.RoundRect(ox - 38 - Outline, oy - 19 - Outline, 14 + Outline * 2, 5 + Outline * 2, 3, ink);
                    p.RoundRect(ox - 38, oy - 19, 14, 5, 2, c);
                    p.RoundRect(ox - 37 - Outline, oy + 3 - Outline, 9 + Outline * 2, 5 + Outline * 2, 3, ink);
                    p.RoundRect(ox - 37, oy + 3, 9, 5, 2, Steel);
                    break;
            }
        }

        private static void DrawKartFront(Paint p, string style, Color c, int number, float ox, float oy)
        {
            var ink = Palette.Ink;
            var light = Palette.Shade(c, 0.35f);
            var dark = Palette.Shade(c, -0.3f);
            switch (style)
            {
                case "cohete":
                    Outlined(p, Offset(new float[] { -30, -4, 12, -4, 32, 4, 12, 12, -30, 12 }, ox, oy), c);
                    p.Polygon(Offset(new float[] { 20, -1, 31, 4, 20, 9 }, ox, oy), light);
                    p.Line(ox - 26, oy + 8, ox + 8, oy + 8, 1.3f, Palette.WithAlpha(Color.white, 0.6f));
                    p.Circle(ox + 3, oy + 3, 5.2f, ink);
                    p.Circle(ox + 3, oy + 3, 3.9f, Palette.Hex("#9fe3ff"));
                    p.Circle(ox + 1.8f, oy + 1.8f, 1.2f, Color.white);
                    p.Line(ox + 8, oy - 3, ox + 12, oy - 10, 1.5f, ink);
                    p.Ellipse(ox + 12, oy - 11, 2.2f, 4.2f, ink, 20);
                    DrawPlate(p, number, ox - 9, oy - 1, 4.2f);
                    break;

                case "tostadora":
                    p.RoundRect(ox + 8, oy - 25, 12, 10, 3, Palette.Hex("#d9a05b"));
                    p.RoundRect(ox + 9.5f, oy - 23.5f, 9, 3, 1.5f, Palette.Hex("#b5651d"));
                    p.RoundRect(ox - 27 - Outline, oy - 16 - Outline, 54 + Outline * 2, 28 + Outline * 2, 8, ink);
                    p.RoundRect(ox - 27, oy - 16, 54, 28, 7, c);
                    p.RoundRect(ox - 27, oy + 4, 54, 8, 4, Chrome);
                    p.Line(ox - 22, oy - 11, ox + 20, oy - 11, 1.2f, Palette.WithAlpha(Color.white, 0.45f));
                    p.RoundRect(ox - 21, oy - 18, 22, 4, 2, ink);
                    p.RoundRect(ox + 5, oy - 18, 18, 4, 2, ink);
                    p.Line(ox + 27, oy - 5, ox + 31, oy - 5, 1.6f, ink);
                    p.Circle(ox + 32, oy - 5, 2.4f, ink);
                    p.Circle(ox + 16, oy - 2, 3.4f, ink);
                    p.Circle(ox + 16, oy - 2, 2.3f, Color.white);
                    DrawPlate(p, number, ox - 12, oy - 3, 5f);
                    break;

                default:
                    Outlined(p, Offset(new float[] { -30, -2, 16, -2, 28, 1, 32, 8, 30, 12, -30, 12 }, ox, oy), c);
                    p.Rect(ox - 29, oy + 8.5f, 58, 3, dark);
                    p.Line(ox - 26, oy + 3, ox + 22, oy + 3, 1.5f, light);
                    p.RoundRect(ox + 28, oy + 6, 6, 6, 2, Palette.Hex("#3a3f52"));
                    p.Circle(ox + 29, oy + 3, 2f, Palette.Cheese);
                    p.Line(ox + 9, oy - 2, ox + 14, oy - 10, 1.6f, ink);
                    p.Ellipse(ox + 14, oy - 11, 2.2f, 4.6f, ink, 20);
                    DrawPlate(p, number, ox + 3, oy + 4, 5.4f);
                    break;
            }
        }

        private static void DrawPlate(Paint p, int number, float cx, float cy, float r)
        {
            p.Circle(cx, cy, r + 1.2f, Palette.Ink);
            p.Circle(cx, cy, r, Color.white);
            float s = r / 5.4f;
            var ink = Palette.Ink;
            if (number == 1)
            {
                p.Line(cx + 0.5f * s, cy - 3.2f * s, cx + 0.5f * s, cy + 3.2f * s, 0.9f * s, ink);
                p.Line(cx + 0.5f * s, cy - 3.2f * s, cx - 1.6f * s, cy - 1.6f * s, 0.9f * s, ink);
            }
            else
            {
                p.Arc(cx, cy - 1.2f * s, 2.1f * s, 1.7f * s, 190, 360, ink);
                p.Line(cx + 2.1f * s, cy - 1.0f * s, cx - 2.2f * s, cy + 3.0f * s, 0.85f * s, ink);
                p.Line(cx - 2.2f * s, cy + 3.0f * s, cx + 2.4f * s, cy + 3.0f * s, 0.85f * s, ink);
            }
        }

        private static void DrawWheel(Paint p, float cx, float cy)
        {
            p.Circle(cx, cy, 10 + 1.2f, Palette.Ink);
            p.Circle(cx, cy, 10, Tire);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f;
                p.Line(cx + Mathf.Cos(a) * 6.5f, cy + Mathf.Sin(a) * 6.5f, cx + Mathf.Cos(a) * 8.5f, cy + Mathf.Sin(a) * 8.5f, 0.8f, Palette.Hex("#4a4a5c"));
            }
            p.Circle(cx, cy, 4.6f, Palette.Hex("#aab2c0"));
            p.Circle(cx, cy, 1.6f, Palette.Hex("#5b6273"));
            p.Circle(cx + 2.6f, cy, 0.8f, Palette.Hex("#5b6273"));
        }

        // ------------------------------------------------------------------ comida

        public static Sprite Food(string id)
        {
            return Make("food_" + id, -24, -24, 48, 48, p => DrawFood(p, id, 0, 0, 1f));
        }

        private static void DrawFood(Paint p, string id, float cx, float cy, float k)
        {
            var ink = Palette.Ink;
            switch (id)
            {
                case "queso":
                    Outlined(p, new[] { cx - 18 * k, cy + 12 * k, cx + 18 * k, cy + 12 * k, cx + 14 * k, cy - 12 * k }, Palette.Hex("#ffd23f"));
                    p.Line(cx - 17 * k, cy + 11 * k, cx + 17 * k, cy + 11 * k, 1.6f * k, Palette.Hex("#f0a500"));
                    p.Circle(cx + 3 * k, cy + 3 * k, 3.2f * k, Palette.Hex("#e8b100"));
                    p.Circle(cx + 10 * k, cy - 4 * k, 2.2f * k, Palette.Hex("#e8b100"));
                    p.Circle(cx - 7 * k, cy + 7 * k, 1.8f * k, Palette.Hex("#e8b100"));
                    p.Circle(cx + 12 * k, cy + 6 * k, 2.4f * k, Palette.Hex("#e8b100"));
                    break;
                case "galletita":
                    p.Circle(cx, cy, 19 * k, ink);
                    p.Circle(cx, cy, 17 * k, Palette.Hex("#d9a05b"));
                    p.Ring(cx, cy, 15 * k, 2.2f * k, Palette.WithAlpha(Palette.Hex("#b57a35"), 0.6f));
                    float[] chips = { -7, -6, 5, -9, 9, 2, -2, 6, -10, 4, 2, -1 };
                    for (int i = 0; i < chips.Length; i += 2)
                    {
                        p.Circle(cx + chips[i] * k, cy + chips[i + 1] * k, 2.5f * k, Palette.Hex("#5a3418"));
                        p.Circle(cx + (chips[i] - 0.7f) * k, cy + (chips[i + 1] - 0.7f) * k, 0.8f * k, Palette.Hex("#8a5a35"));
                    }
                    break;
                case "alfajor":
                    p.Ellipse(cx, cy + 6 * k, 19 * k, 8 * k, ink);
                    p.Ellipse(cx, cy + 6 * k, 17 * k, 6.5f * k, Palette.Hex("#4a2a17"));
                    p.Rect(cx - 17.5f * k, cy - 1 * k, 35 * k, 7 * k, ink);
                    p.Rect(cx - 16.5f * k, cy - 1 * k, 33 * k, 7 * k, Palette.Hex("#d38b3a"));
                    p.Ellipse(cx, cy - 1 * k, 19 * k, 8 * k, ink);
                    p.Ellipse(cx, cy - 1 * k, 17 * k, 6.5f * k, Palette.Hex("#5b3420"));
                    p.Ellipse(cx - 5 * k, cy - 3 * k, 6 * k, 2 * k, Palette.WithAlpha(Color.white, 0.35f), -8);
                    break;
                default: // miguita
                {
                    float[] lobes = { 1f, 0.86f, 0.97f, 0.8f, 1f, 0.9f, 0.78f, 0.95f, 0.88f, 0.92f };
                    p.Polygon(Blob(cx, cy, 17 * k * 1.2f, lobes), ink);
                    p.Polygon(Blob(cx, cy, 17 * k * 1.1f, lobes), Palette.Hex("#c47a32"));
                    p.Polygon(Blob(cx, cy, 17 * k, lobes), Palette.Hex("#f6d79d"));
                    float[] pores = { -6, -4, 4, -7, 7, 3, -2, 6, -9, 4, 1, 0 };
                    for (int i = 0; i < pores.Length; i += 2)
                        p.Ellipse(cx + pores[i] * k, cy + pores[i + 1] * k, 1.8f * k, 1.2f * k, Palette.Hex("#e0b26c"), i * 20);
                    break;
                }
            }
        }

        private static float[] Blob(float cx, float cy, float r, float[] lobes)
        {
            int n = lobes.Length * 3;
            var pts = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float f = i / 3f;
                int a = (int)f % lobes.Length, b = (a + 1) % lobes.Length;
                float rr = r * Mathf.Lerp(lobes[a], lobes[b], f - Mathf.Floor(f));
                float t = i / (float)n * Mathf.PI * 2f;
                pts[i * 2] = cx + Mathf.Cos(t) * rr;
                pts[i * 2 + 1] = cy + Mathf.Sin(t) * rr;
            }
            return pts;
        }

        // ------------------------------------------------------------------ efectos

        public static Sprite Soft()
        {
            return Make("soft", -16, -16, 32, 32, p => p.RadialGlow(0, 0, 16, new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0f)), 0, 0, 2f);
        }

        public static Sprite Dot()
        {
            return Make("dot", -6, -6, 12, 12, p => p.Circle(0, 0, 5, Color.white));
        }

        public static Sprite Crumb()
        {
            return Make("crumb", -5, -5, 10, 10, p =>
            {
                p.Polygon(new float[] { -3, -2, 1, -4, 4, -1, 3, 3, -2, 3 }, Palette.Hex("#c47a32"));
                p.Polygon(new float[] { -2, -1, 1, -3, 3, -1, 2, 2, -1, 2 }, Palette.Hex("#f6d79d"));
            });
        }

        public static Sprite Star()
        {
            return Make("star", -10, -10, 20, 20, p =>
            {
                p.Polygon(Paint.StarPoints(0, 0, 5, 9, 4.2f), Palette.Ink);
                p.Polygon(Paint.StarPoints(0, 0, 5, 7, 3.2f), Color.white);
            });
        }

        public static Sprite Ring()
        {
            return Make("ring", -32, -32, 64, 64, p => p.Ring(0, 0, 28, 3, Color.white));
        }

        public static Sprite Shield()
        {
            return Make("shield", -36, -36, 72, 72, p =>
            {
                p.RadialGlow(0, 0, 33, new Color(1, 1, 1, 0f), new Color(1, 1, 1, 0.25f));
                p.Ring(0, 0, 32, 2.2f, new Color(1, 1, 1, 0.8f));
                p.Arc(0, 0, 27, 2.4f, 205, 250, new Color(1, 1, 1, 0.9f));
            });
        }

        public static Sprite Arrow()
        {
            return Make("arrow", -12, -12, 24, 24, p => Outlined(p, new float[] { -8, -5, 8, -5, 0, 7 }, Color.white, 1.6f));
        }

        // ------------------------------------------------------------------ retratos para la interfaz

        /// <summary>Karting + bichito armados en una sola imagen, mirando a la derecha.</summary>
        public static Sprite Portrait(Look look, int number)
        {
            string key = $"portrait_{look.species}{look.body}{look.kart}{look.style}{look.hat}{number}";
            return Make(key, -46, -64, 92, 92, p =>
            {
                var kc = Palette.Hex(look.kart);
                DrawKartBack(p, look.style, kc, 0, 0);
                DrawBichito(p, look, SeatX, SeatY(look.style));
                DrawKartFront(p, look.style, kc, number, 0, 0);
                DrawWheel(p, -16, 13);
                DrawWheel(p, 16, 13);
            }, 0, -18);
        }

        /// <summary>Solo el bichito con su accesorio (para los botones del garaje y el HUD).</summary>
        public static Sprite Head(Look look)
        {
            string key = $"head_{look.species}{look.body}{look.hat}";
            return Make(key, -30, -42, 60, 64, p => DrawBichito(p, look, 0, 0), 0, -10);
        }

        public static Sprite FoodIcon(string id) => Food(id);

        // ------------------------------------------------------------------ interfaz

        /// <summary>Rectángulo redondeado blanco para teñir (9-slice).</summary>
        public static Sprite UiRound(int radius = 18)
        {
            string key = "ui_round" + radius;
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            int size = radius * 2 + 8;
            var p = new Paint(size, size);
            p.RoundRect(0, 0, size, size, radius, Color.white);
            s = p.ToUiSprite(new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
            Cache[key] = s;
            return s;
        }

        /// <summary>Marco redondeado (solo el borde) para marcar lo elegido.</summary>
        public static Sprite UiFrame()
        {
            const string key = "ui_frame";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(64, 64);
            p.RoundRectStroke(4, 4, 56, 56, 18, 6, Color.white);
            s = p.ToUiSprite(new Vector4(24, 24, 24, 24));
            Cache[key] = s;
            return s;
        }

        /// <summary>Botón con borde de tinta y "sombra" abajo; el relleno blanco se tiñe con Image.color.</summary>
        public static Sprite UiButton()
        {
            const string key = "ui_button";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(64, 72);
            p.RoundRect(0, 0, 64, 72, 18, Palette.Ink);
            p.RoundRect(4, 4, 56, 58, 14, Color.white);
            p.RoundRect(10, 8, 44, 6, 3, new Color(1, 1, 1, 0.55f));
            s = p.ToUiSprite(new Vector4(22, 26, 22, 22));
            Cache[key] = s;
            return s;
        }

        /// <summary>Puntitos de galletita para el relleno de los botones (se repite en mosaico).</summary>
        public static Sprite UiDots()
        {
            const string key = "ui_dots";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(28, 28);
            p.Circle(14, 14, 2.6f, new Color(0.45f, 0.22f, 0.05f, 0.22f));
            s = p.ToUiSprite(Vector4.zero);
            s.texture.wrapMode = TextureWrapMode.Repeat;
            Cache[key] = s;
            return s;
        }

        public static Sprite UiCircle()
        {
            const string key = "ui_circle";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(96, 96);
            p.Circle(48, 48, 46, Color.white);
            s = p.ToUiSprite(Vector4.zero);
            Cache[key] = s;
            return s;
        }

        public static Sprite UiRing()
        {
            const string key = "ui_ring";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(96, 96);
            p.Ring(48, 48, 43, 5, Color.white);
            s = p.ToUiSprite(Vector4.zero);
            Cache[key] = s;
            return s;
        }

        public static Sprite UiWhite()
        {
            const string key = "ui_white";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(4, 4);
            p.Fill(Color.white);
            s = p.ToUiSprite(Vector4.zero);
            Cache[key] = s;
            return s;
        }

        public static Sprite UiTriangle()
        {
            const string key = "ui_triangle";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Paint(64, 64);
            p.Polygon(new float[] { 8, 12, 56, 12, 32, 54 }, Color.white);
            s = p.ToUiSprite(Vector4.zero);
            Cache[key] = s;
            return s;
        }
    }
}
