using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Texturas dibujadas por código para el prototipo: granito, azulejos, madera y el espacio con planetas.
    /// </summary>
    public static class Texturas
    {
        public static Texture2D Granito()
        {
            const int lado = 512;
            var t = Nueva(lado, lado);
            var rng = new System.Random(7);
            var px = new Color32[lado * lado];
            for (int i = 0; i < px.Length; i++)
            {
                float r = (float)rng.NextDouble();
                byte b = (byte)(r < 0.04f ? 120 : r < 0.12f ? 200 : 245);
                px[i] = new Color32(b, b, (byte)Mathf.Min(255, b + 6), 255);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>Dos por dos azulejos con su junta.</summary>
        public static Texture2D Azulejos()
        {
            const int lado = 128;
            var t = Nueva(lado, lado);
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                bool junta = x % 64 < 3 || y % 64 < 3;
                px[y * lado + x] = junta ? new Color32(205, 192, 182, 255) : new Color32(255, 255, 255, 255);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>Tablas de madera con vetas suaves, para la parte de abajo de la alacena.</summary>
        public static Texture2D Madera()
        {
            const int ancho = 128, alto = 256;
            var t = Nueva(ancho, alto);
            var px = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
            {
                bool junta = x % 32 < 2;
                float veta = 0.9f + 0.1f * Mathf.Sin(y * 0.11f + Mathf.Sin(x * 0.4f) * 2f + (x / 32) * 1.7f);
                byte v = (byte)(junta ? 150 : 235 * veta);
                px[y * ancho + x] = new Color32(v, v, v, 255);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        public static Texture2D Espacio()
        {
            const int w = 512, h = 300;
            var t = Nueva(w, h);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var arriba = Formas.Hex("#120c33");
            var abajo = Formas.Hex("#3a2475");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = Color.Lerp(abajo, arriba, y / (float)h);

            var rng = new System.Random(3);
            for (int i = 0; i < 700; i++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                float brillo = 0.4f + (float)rng.NextDouble() * 0.6f;
                px[y * w + x] = Color.Lerp(px[y * w + x], Color.white, brillo);
            }
            Planeta(px, w, h, 130, 190, 46, Formas.Hex("#ff9f6b"));
            Planeta(px, w, h, 400, 80, 26, Formas.Hex("#3e8ef7"));
            Planeta(px, w, h, 300, 230, 12, Formas.Hex("#79f0c3"));
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        private static Texture2D Nueva(int ancho, int alto)
        {
            // El filtrado anisotrópico evita que el piso se vea borroso a lo lejos.
            return new Texture2D(ancho, alto, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
        }

        private static void Planeta(Color[] px, int w, int h, int cx, int cy, int r, Color color)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > r) continue;
                float luz = Mathf.Clamp01(1.1f - ((x - cx + r * 0.4f) * 0.5f + (cy - y + r * 0.4f) * 0.5f) / (r * 2f));
                px[y * w + x] = Color.Lerp(color * 0.55f, color, luz);
            }
        }
    }
}
