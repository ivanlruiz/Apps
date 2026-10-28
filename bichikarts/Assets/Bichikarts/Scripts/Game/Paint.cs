using UnityEngine;

namespace Bichikarts
{
    /// <summary>
    /// Pinta figuras suavizadas sobre un lienzo de píxeles para crear las texturas del juego desde código
    /// (bichitos, karting, comida, salas). Las coordenadas están en "unidades" con Y hacia abajo; Scale dice
    /// cuántos píxeles de textura hay por unidad y Origin qué unidad cae en la esquina superior izquierda.
    /// </summary>
    public sealed class Paint
    {
        public readonly int Width, Height;
        public readonly float Scale;
        public readonly float OriginX, OriginY;
        private readonly Color32[] _px;

        public Paint(int width, int height, float scale = 1f, float originX = 0f, float originY = 0f)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Scale = scale;
            OriginX = originX;
            OriginY = originY;
            _px = new Color32[Width * Height];
        }

        /// <summary>Lienzo que cubre el rectángulo de unidades [x0, x0+w] x [y0, y0+h].</summary>
        public static Paint ForUnits(float x0, float y0, float w, float h, float scale)
        {
            return new Paint(Mathf.CeilToInt(w * scale), Mathf.CeilToInt(h * scale), scale, x0, y0);
        }

        public Color32[] Pixels => _px;

        // --- Conversión ---

        private float PX(float x) => (x - OriginX) * Scale;
        private float PY(float y) => (y - OriginY) * Scale;

        private int Row(int py) => Height - 1 - py;

        private void Blend(int px, int py, Color c, float coverage)
        {
            float a = c.a * coverage;
            if (a <= 0.003f) return;
            int i = Row(py) * Width + px;
            var d = _px[i];
            if (a >= 0.997f)
            {
                _px[i] = new Color32((byte)(c.r * 255f + 0.5f), (byte)(c.g * 255f + 0.5f), (byte)(c.b * 255f + 0.5f), 255);
                return;
            }
            float da = d.a / 255f;
            float oa = a + da * (1f - a);
            if (oa <= 0f) return;
            float k = da * (1f - a);
            float r = (c.r * a + d.r / 255f * k) / oa;
            float g = (c.g * a + d.g / 255f * k) / oa;
            float b = (c.b * a + d.b / 255f * k) / oa;
            _px[i] = new Color32((byte)(r * 255f + 0.5f), (byte)(g * 255f + 0.5f), (byte)(b * 255f + 0.5f), (byte)(oa * 255f + 0.5f));
        }

        private void Bounds(float x0, float y0, float x1, float y1, out int ax, out int ay, out int bx, out int by)
        {
            ax = Mathf.Max(0, Mathf.FloorToInt(PX(x0)) - 1);
            ay = Mathf.Max(0, Mathf.FloorToInt(PY(y0)) - 1);
            bx = Mathf.Min(Width - 1, Mathf.CeilToInt(PX(x1)) + 1);
            by = Mathf.Min(Height - 1, Mathf.CeilToInt(PY(y1)) + 1);
        }

        // --- Figuras ---

        public void Fill(Color c)
        {
            var c32 = (Color32)c;
            for (int i = 0; i < _px.Length; i++) _px[i] = c32;
        }

        /// <summary>Rectángulo con bordes suavizados (cobertura exacta por píxel).</summary>
        public void Rect(float x, float y, float w, float h, Color c)
        {
            if (w <= 0f || h <= 0f) return;
            float x0 = PX(x), x1 = PX(x + w), y0 = PY(y), y1 = PY(y + h);
            Bounds(x, y, x + w, y + h, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float cy = Mathf.Min(py + 1f, y1) - Mathf.Max(py, y0);
                if (cy <= 0f) continue;
                if (cy > 1f) cy = 1f;
                for (int px = ax; px <= bx; px++)
                {
                    float cx = Mathf.Min(px + 1f, x1) - Mathf.Max(px, x0);
                    if (cx <= 0f) continue;
                    if (cx > 1f) cx = 1f;
                    Blend(px, py, c, cx * cy);
                }
            }
        }

        public void VGradient(float x, float y, float w, float h, Color top, Color bottom)
        {
            float y0 = PY(y), y1 = PY(y + h);
            float x0 = PX(x), x1 = PX(x + w);
            Bounds(x, y, x + w, y + h, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float cy = Mathf.Clamp01(Mathf.Min(py + 1f, y1) - Mathf.Max(py, y0));
                if (cy <= 0f) continue;
                var c = Color.Lerp(top, bottom, Mathf.InverseLerp(y0, y1, py + 0.5f));
                for (int px = ax; px <= bx; px++)
                {
                    float cx = Mathf.Clamp01(Mathf.Min(px + 1f, x1) - Mathf.Max(px, x0));
                    if (cx > 0f) Blend(px, py, c, cx * cy);
                }
            }
        }

        public void Circle(float cx, float cy, float r, Color c)
        {
            float pcx = PX(cx), pcy = PY(cy), pr = r * Scale;
            Bounds(cx - r, cy - r, cx + r, cy + r, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float dy = py + 0.5f - pcy;
                for (int px = ax; px <= bx; px++)
                {
                    float dx = px + 0.5f - pcx;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - pr;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        /// <summary>Círculo con degradé radial (centro → borde), útil para brillos.</summary>
        public void RadialGlow(float cx, float cy, float r, Color inner, Color outer)
        {
            float pcx = PX(cx), pcy = PY(cy), pr = r * Scale;
            Bounds(cx - r, cy - r, cx + r, cy + r, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float dy = py + 0.5f - pcy;
                for (int px = ax; px <= bx; px++)
                {
                    float dx = px + 0.5f - pcx;
                    float t = Mathf.Sqrt(dx * dx + dy * dy) / pr;
                    if (t >= 1f) continue;
                    var c = Color.Lerp(inner, outer, t);
                    Blend(px, py, c, 1f);
                }
            }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c, float angleDeg = 0f)
        {
            float pcx = PX(cx), pcy = PY(cy), prx = rx * Scale, pry = ry * Scale;
            float m = Mathf.Max(rx, ry);
            float cos = Mathf.Cos(angleDeg * Mathf.Deg2Rad), sin = Mathf.Sin(angleDeg * Mathf.Deg2Rad);
            float minR = Mathf.Min(prx, pry);
            Bounds(cx - m, cy - m, cx + m, cy + m, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float dy0 = py + 0.5f - pcy;
                for (int px = ax; px <= bx; px++)
                {
                    float dx0 = px + 0.5f - pcx;
                    float dx = dx0 * cos + dy0 * sin;
                    float dy = -dx0 * sin + dy0 * cos;
                    float q = Mathf.Sqrt(dx * dx / (prx * prx) + dy * dy / (pry * pry));
                    float d = (q - 1f) * minR;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        public void RoundRect(float x, float y, float w, float h, float r, Color c)
        {
            if (w <= 0f || h <= 0f) return;
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            float pcx = PX(x + w * 0.5f), pcy = PY(y + h * 0.5f);
            float hx = w * 0.5f * Scale, hy = h * 0.5f * Scale, pr = r * Scale;
            Bounds(x, y, x + w, y + h, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float qy = Mathf.Abs(py + 0.5f - pcy) - hy + pr;
                for (int px = ax; px <= bx; px++)
                {
                    float qx = Mathf.Abs(px + 0.5f - pcx) - hx + pr;
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - pr;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        /// <summary>Solo el borde de un rectángulo redondeado (el centro queda transparente).</summary>
        public void RoundRectStroke(float x, float y, float w, float h, float r, float thickness, Color c)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            float pcx = PX(x + w * 0.5f), pcy = PY(y + h * 0.5f);
            float hx = w * 0.5f * Scale, hy = h * 0.5f * Scale, pr = r * Scale, ht = thickness * 0.5f * Scale;
            Bounds(x - thickness, y - thickness, x + w + thickness, y + h + thickness, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float qy = Mathf.Abs(py + 0.5f - pcy) - hy + pr;
                for (int px = ax; px <= bx; px++)
                {
                    float qx = Mathf.Abs(px + 0.5f - pcx) - hx + pr;
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Abs(Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - pr) - ht;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        public void Ring(float cx, float cy, float r, float thickness, Color c)
        {
            float pcx = PX(cx), pcy = PY(cy), pr = r * Scale, ht = thickness * 0.5f * Scale;
            float e = r + thickness;
            Bounds(cx - e, cy - e, cx + e, cy + e, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float dy = py + 0.5f - pcy;
                for (int px = ax; px <= bx; px++)
                {
                    float dx = px + 0.5f - pcx;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - pr) - ht;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        /// <summary>Línea gruesa con puntas redondeadas.</summary>
        public void Line(float ax, float ay, float bx, float by, float radius, Color c)
        {
            float pax = PX(ax), pay = PY(ay), pbx = PX(bx), pby = PY(by), pr = radius * Scale;
            float vx = pbx - pax, vy = pby - pay;
            float len2 = vx * vx + vy * vy;
            Bounds(Mathf.Min(ax, bx) - radius, Mathf.Min(ay, by) - radius, Mathf.Max(ax, bx) + radius, Mathf.Max(ay, by) + radius,
                out int x0, out int y0, out int x1, out int y1);
            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    float wx = px + 0.5f - pax, wy = py + 0.5f - pay;
                    float t = len2 > 0f ? Mathf.Clamp01((wx * vx + wy * vy) / len2) : 0f;
                    float dx = wx - vx * t, dy = wy - vy * t;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - pr;
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        /// <summary>Arco de círculo (ángulos en grados, 0 = derecha, sentido horario porque Y va hacia abajo).</summary>
        public void Arc(float cx, float cy, float r, float thickness, float fromDeg, float toDeg, Color c)
        {
            int steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 8f));
            float px = cx + Mathf.Cos(fromDeg * Mathf.Deg2Rad) * r;
            float py = cy + Mathf.Sin(fromDeg * Mathf.Deg2Rad) * r;
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                float nx = cx + Mathf.Cos(a) * r, ny = cy + Mathf.Sin(a) * r;
                Line(px, py, nx, ny, thickness * 0.5f, c);
                px = nx; py = ny;
            }
        }

        /// <summary>Polígono relleno (puntos x,y alternados) con bordes suavizados.</summary>
        public void Polygon(float[] pts, Color c)
        {
            int n = pts.Length / 2;
            if (n < 3) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            var p = new float[pts.Length];
            for (int i = 0; i < n; i++)
            {
                minX = Mathf.Min(minX, pts[i * 2]); maxX = Mathf.Max(maxX, pts[i * 2]);
                minY = Mathf.Min(minY, pts[i * 2 + 1]); maxY = Mathf.Max(maxY, pts[i * 2 + 1]);
                p[i * 2] = PX(pts[i * 2]);
                p[i * 2 + 1] = PY(pts[i * 2 + 1]);
            }
            Bounds(minX, minY, maxX, maxY, out int ax, out int ay, out int bx, out int by);
            for (int py = ay; py <= by; py++)
            {
                float sy = py + 0.5f;
                for (int px = ax; px <= bx; px++)
                {
                    float sx = px + 0.5f;
                    bool inside = false;
                    float best = float.MaxValue;
                    for (int i = 0, j = n - 1; i < n; j = i++)
                    {
                        float xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
                        if ((yi > sy) != (yj > sy) && sx < (xj - xi) * (sy - yi) / (yj - yi) + xi) inside = !inside;
                        float ex = xj - xi, ey = yj - yi;
                        float l2 = ex * ex + ey * ey;
                        float t = l2 > 0f ? Mathf.Clamp01(((sx - xi) * ex + (sy - yi) * ey) / l2) : 0f;
                        float dx = sx - xi - ex * t, dy = sy - yi - ey * t;
                        float d2 = dx * dx + dy * dy;
                        if (d2 < best) best = d2;
                    }
                    float d = Mathf.Sqrt(best) * (inside ? -1f : 1f);
                    if (d < 0.5f) Blend(px, py, c, Mathf.Clamp01(0.5f - d));
                }
            }
        }

        /// <summary>Estrella de n puntas centrada en (cx, cy).</summary>
        public static float[] StarPoints(float cx, float cy, int points, float outer, float inner, float rotDeg = -90f)
        {
            var pts = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (rotDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? outer : inner;
                pts[i * 2] = cx + Mathf.Cos(a) * r;
                pts[i * 2 + 1] = cy + Mathf.Sin(a) * r;
            }
            return pts;
        }

        /// <summary>Copia otro lienzo encima (mezclando), con su origen en la unidad (x, y) de este.</summary>
        public void Stamp(Paint src, float x, float y)
        {
            int ox = Mathf.RoundToInt(PX(x)), oy = Mathf.RoundToInt(PY(y));
            for (int sy = 0; sy < src.Height; sy++)
            {
                int py = oy + sy;
                if (py < 0 || py >= Height) continue;
                for (int sx = 0; sx < src.Width; sx++)
                {
                    int px = ox + sx;
                    if (px < 0 || px >= Width) continue;
                    var s = src._px[(src.Height - 1 - sy) * src.Width + sx];
                    if (s.a == 0) continue;
                    Blend(px, py, new Color(s.r / 255f, s.g / 255f, s.b / 255f, 1f), s.a / 255f);
                }
            }
        }

        /// <summary>Pega encima un lienzo del mismo tamaño con una transparencia general.</summary>
        public void StampFaded(Paint src, float alpha)
        {
            int w = Mathf.Min(Width, src.Width), h = Mathf.Min(Height, src.Height);
            for (int row = 0; row < h; row++)
            {
                int py = Height - 1 - row;
                for (int x = 0; x < w; x++)
                {
                    var s = src._px[row * src.Width + x];
                    if (s.a == 0) continue;
                    Blend(x, py, new Color(s.r / 255f, s.g / 255f, s.b / 255f, 1f), s.a / 255f * alpha);
                }
            }
        }

        // --- Salida ---

        public Texture2D ToTexture(bool readable = false)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(_px);
            tex.Apply(false, !readable);
            return tex;
        }

        /// <summary>Sprite cuyo pivote es la unidad (pivotX, pivotY). PixelsPerUnit hace que 100 unidades = 1 metro de Unity.</summary>
        public Sprite ToSprite(float pivotX, float pivotY, Vector4 border = default)
        {
            var tex = ToTexture();
            var pivot = new Vector2(PX(pivotX) / Width, 1f - PY(pivotY) / Height);
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), pivot, Coords.UnitsPerMeter * Scale, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Sprite para la interfaz (9-slice): border en píxeles de textura.</summary>
        public Sprite ToUiSprite(Vector4 border, float pixelsPerUnit = 100f)
        {
            var tex = ToTexture();
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }
    }
}
