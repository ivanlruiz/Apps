using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Lo que se eligió en el menú para la próxima partida. Es estático para que sobreviva al cambio de
    /// escena; el último nivel elegido además queda guardado en la PC.
    /// </summary>
    public static class EleccionDePartida
    {
        public const string EscenaDelMenu = "Menu", EscenaDeLaPartida = "Prototipo";

        /// <summary>Nivel de la compu elegido en el menú (-1 si se entró directo a la escena de la partida).</summary>
        public static int Nivel { get; private set; } = -1;

        /// <summary>El último nivel que se eligió (Normal si nunca se eligió).</summary>
        public static int UltimoNivel => PlayerPrefs.GetInt("UltimoNivel", 1);

        public static void ElegirNivel(int nivel)
        {
            Nivel = nivel;
            PlayerPrefs.SetInt("UltimoNivel", nivel);
            PlayerPrefs.Save();
        }
    }
}
