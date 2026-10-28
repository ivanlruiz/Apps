using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Formas blancas dibujadas por código para la interfaz (se tiñen con el color de cada imagen):
    /// rectángulos redondeados que se estiran sin deformar las puntas, círculos, anillos y flechas.
    /// Tienen los bordes suavizados para que se vean "inflados" y prolijos.
    /// </summary>
    public static class Dibujos
    {
        private static Sprite _redondeado, _circulo, _anillo, _flecha;

        /// <summary>Rectángulo con las puntas redondeadas, para estirar (barras y paneles).</summary>
        public static Sprite Redondeado => _redondeado != null ? _redondeado : _redondeado = Crear(64, 64, 24, (x, y) => DistanciaRectangulo(x, y, 64, 64, 24));

        public static Sprite Circulo => _circulo != null ? _circulo : _circulo = Crear(64, 64, 0, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) - 31f);

        public static Sprite Anillo => _anillo != null ? _anillo : _anillo = Crear(64, 64, 0, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f));
            return Mathf.Max(d - 31f, 22f - d);
        });

        /// <summary>Flecha con la punta hacia arriba.</summary>
        public static Sprite Flecha => _flecha != null ? _flecha : _flecha = Crear(64, 64, 0, (x, y) =>
        {
            // Triángulo de puntas (32,62), (4,6) y (60,6): distancia con signo a sus tres lados.
            float d1 = Lado(x, y, 32f, 62f, 4f, 6f), d2 = Lado(x, y, 4f, 6f, 60f, 6f), d3 = Lado(x, y, 60f, 6f, 32f, 62f);
            return Mathf.Max(d1, Mathf.Max(d2, d3));
        });

        /// <param name="distancia">Distancia con signo al borde de la forma: negativa adentro.</param>
        private static Sprite Crear(int ancho, int alto, int borde, System.Func<float, float, float> distancia)
        {
            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
            {
                float d = distancia(x + 0.5f, y + 0.5f);
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                px[y * ancho + x] = new Color32(255, 255, 255, a);
            }
            textura.SetPixels32(px);
            textura.Apply();
            var sprite = Sprite.Create(textura, new Rect(0, 0, ancho, alto), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borde, borde, borde, borde));
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static float DistanciaRectangulo(float x, float y, float ancho, float alto, float radio)
        {
            float dx = Mathf.Abs(x - ancho * 0.5f) - (ancho * 0.5f - radio);
            float dy = Mathf.Abs(y - alto * 0.5f) - (alto * 0.5f - radio);
            float afuera = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            return afuera + Mathf.Min(Mathf.Max(dx, dy), 0f) - radio + 1f;
        }

        /// <summary>Distancia con signo a la recta a→b (positiva a la derecha, que es afuera del triángulo).</summary>
        private static float Lado(float x, float y, float ax, float ay, float bx, float by)
        {
            var direccion = new Vector2(bx - ax, by - ay).normalized;
            var normal = new Vector2(direccion.y, -direccion.x);
            return Vector2.Dot(new Vector2(x - ax, y - ay), normal);
        }
    }
}
