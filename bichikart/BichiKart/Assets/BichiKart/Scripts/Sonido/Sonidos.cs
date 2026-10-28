using System;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Sonidos generados por código para el prototipo, hasta que lleguen los de verdad.
    /// </summary>
    public static class Sonidos
    {
        private const int Frecuencia = 44100;

        /// <summary>Bocina de juguete: dos tonos juntos que suben un poquito al final.</summary>
        public static AudioClip Bocina(float tono = 520f)
        {
            const float duracion = 0.32f;
            return Crear("Bocina", duracion, t =>
            {
                float subida = 1f + 0.06f * Mathf.Clamp01((t - duracion * 0.6f) / (duracion * 0.4f));
                float vibrato = 1f + 0.012f * Mathf.Sin(t * 2f * Mathf.PI * 7f);
                float f = tono * subida * vibrato;
                float onda = Cuadrada(t, f) * 0.6f + Cuadrada(t, f * 1.26f) * 0.4f;
                return onda * Envolvente(t, duracion, 0.015f, 0.08f) * 0.35f;
            });
        }

        /// <summary>"Plop" corto y redondo, para robar la comida y salir de los caños.</summary>
        public static AudioClip Plop()
        {
            const float duracion = 0.16f;
            return Crear("Plop", duracion, t =>
            {
                float f = Mathf.Lerp(720f, 240f, t / duracion);
                return Mathf.Sin(2f * Mathf.PI * f * t) * Envolvente(t, duracion, 0.004f, 0.12f) * 0.6f;
            });
        }

        /// <summary>Succión de aire que sube, para entrar a los caños.</summary>
        public static AudioClip Succion()
        {
            const float duracion = 0.45f;
            var rng = new System.Random(5);
            float filtrado = 0f;
            return Crear("Succión", duracion, t =>
            {
                float ruido = (float)rng.NextDouble() * 2f - 1f;
                float corte = Mathf.Lerp(0.05f, 0.4f, t / duracion);
                filtrado += (ruido - filtrado) * corte;
                return filtrado * Envolvente(t, duracion, 0.1f, 0.1f) * 0.8f;
            });
        }

        /// <summary>"Fiuuu" que sube, para el mini turbo.</summary>
        public static AudioClip Turbo()
        {
            const float duracion = 0.5f;
            float fase = 0f;
            return Crear("Turbo", duracion, t =>
            {
                float f = Mathf.Lerp(180f, 820f, Mathf.Sqrt(t / duracion));
                fase += 2f * Mathf.PI * f / Frecuencia;
                return (Mathf.Sin(fase) * 0.7f + Mathf.Sin(fase * 2.01f) * 0.3f) * Envolvente(t, duracion, 0.02f, 0.25f) * 0.4f;
            });
        }

        private static AudioClip Crear(string nombre, float duracion, Func<float, float> muestra)
        {
            int cantidad = Mathf.CeilToInt(duracion * Frecuencia);
            var datos = new float[cantidad];
            for (int i = 0; i < cantidad; i++) datos[i] = Mathf.Clamp(muestra(i / (float)Frecuencia), -1f, 1f);
            var clip = AudioClip.Create(nombre, cantidad, 1, Frecuencia, false);
            clip.SetData(datos, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }

        /// <summary>Onda cuadrada suavizada (menos áspera que una cuadrada pura).</summary>
        private static float Cuadrada(float t, float f) => (float)Math.Tanh(Mathf.Sin(2f * Mathf.PI * f * t) * 4f);

        private static float Envolvente(float t, float duracion, float entrada, float salida)
        {
            float subir = Mathf.Clamp01(t / entrada);
            float bajar = Mathf.Clamp01((duracion - t) / salida);
            return subir * bajar;
        }
    }
}
