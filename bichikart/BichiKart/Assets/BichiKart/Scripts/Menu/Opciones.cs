using System;
using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Las opciones del jugador: cámara, sonido y pantalla. Se guardan en la PC (PlayerPrefs) y valen para
    /// el menú y para la partida. La pantalla la guarda Unity solo; en el editor no cambia nada.
    /// </summary>
    public static class Opciones
    {
        public const int SensibilidadMinima = 1, SensibilidadMaxima = 12, VolumenMaximo = 10;

        /// <summary>Cada paso de sensibilidad son estos grados por punto que se mueve el mouse (el 5 es 0,25°).</summary>
        private const float GradosPorPaso = 0.05f;

        private static Vector2Int? _resolucionElegida;

        /// <summary>Cambió alguna opción (para que la pantalla de opciones se actualice).</summary>
        public static event Action AlCambiar;

        /// <summary>Sensibilidad del mouse, de 1 a 12.</summary>
        public static int Sensibilidad
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("Sensibilidad", 5), SensibilidadMinima, SensibilidadMaxima);
            set => Guardar("Sensibilidad", Mathf.Clamp(value, SensibilidadMinima, SensibilidadMaxima));
        }

        /// <summary>Grados que gira la cámara por cada punto que se mueve el mouse.</summary>
        public static float GradosPorPunto => Sensibilidad * GradosPorPaso;

        public static bool InvertirVertical
        {
            get => PlayerPrefs.GetInt("InvertirVertical", 0) == 1;
            set => Guardar("InvertirVertical", value ? 1 : 0);
        }

        /// <summary>La cámara acompaña más despacio los giros del kart en las paredes y el techo.</summary>
        public static bool AntiMareo
        {
            get => PlayerPrefs.GetInt("AntiMareo", 0) == 1;
            set => Guardar("AntiMareo", value ? 1 : 0);
        }

        /// <summary>Volumen general, de 0 a 10.</summary>
        public static int Volumen
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("Volumen", 8), 0, VolumenMaximo);
            set => Guardar("Volumen", Mathf.Clamp(value, 0, VolumenMaximo));
        }

        public static bool PantallaCompleta
        {
            get => Screen.fullScreenMode != FullScreenMode.Windowed;
            set
            {
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                AlCambiar?.Invoke();
            }
        }

        /// <summary>La resolución elegida (Unity la aplica en el cuadro siguiente).</summary>
        public static Vector2Int Resolucion
        {
            get => _resolucionElegida ?? new Vector2Int(Screen.width, Screen.height);
            set
            {
                _resolucionElegida = value;
                Screen.SetResolution(value.x, value.y, Screen.fullScreenMode);
                AlCambiar?.Invoke();
            }
        }

        /// <summary>Las resoluciones de la pantalla, sin repetir, de la más chica a la más grande.</summary>
        public static List<Vector2Int> Resoluciones()
        {
            var lista = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var tamano = new Vector2Int(r.width, r.height);
                if (!lista.Contains(tamano)) lista.Add(tamano);
            }
            if (!lista.Contains(Resolucion)) lista.Add(Resolucion);
            lista.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return lista;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlArrancar() => Aplicar();

        private static void Aplicar() => AudioListener.volume = Volumen / (float)VolumenMaximo;

        private static void Guardar(string clave, int valor)
        {
            PlayerPrefs.SetInt(clave, valor);
            PlayerPrefs.Save();
            Aplicar();
            AlCambiar?.Invoke();
        }
    }
}
