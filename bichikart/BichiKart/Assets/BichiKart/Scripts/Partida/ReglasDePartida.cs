using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Números de las reglas de una partida. Se pueden cambiar mientras se juega.
    /// </summary>
    [CreateAssetMenu(menuName = "BichiKart/Reglas de partida", fileName = "ReglasDePartida")]
    public sealed class ReglasDePartida : ScriptableObject
    {
        [Header("Para ganar")]
        [Tooltip("Segundos que hay que aguantar la comida con 2, 3 y 4 jugadores.")]
        public float tiempoDosJugadores = 15f, tiempoTresJugadores = 20f, tiempoCuatroJugadores = 20f;

        [Tooltip("Rondas que hay que ganar (2 = al mejor de 3).")]
        public int rondasParaGanar = 2;

        [Tooltip("Si pasan estos segundos sin ganador, empieza la muerte súbita.")]
        public float muerteSubitaTras = 180f;

        [Tooltip("En la muerte súbita, segundos que hay que aguantar la comida para ganar.")]
        public float tiempoMuerteSubita = 5f;

        [Header("Robo")]
        [Tooltip("Segundos que queda protegido el que roba.")]
        public float proteccionTrasRobo = 1f;

        [Tooltip("Si dos karts tocan al dueño dentro de estos segundos, la comida se cae en vez de ser robada.")]
        public float ventanaDobleToque = 0.1f;

        [Tooltip("Al que se le cayó la comida no la puede volver a agarrar durante estos segundos.")]
        public float esperaTrasCaida = 0.6f;

        [Tooltip("El que tiene la comida va así de rápido (1 = normal).")]
        public float velocidadDuenio = 0.85f;

        [Tooltip("Los que no la tienen van así de rápido mientras alguien la tiene.")]
        public float velocidadPerseguidores = 1.1f;

        [Header("Empujones")]
        [Tooltip("El robado sale despedido: velocidad hacia atrás, hacia arriba y segundos sin poder manejar.")]
        public float empujeRobado = 9f, saltoRobado = 6f, aturdidoRobado = 0.5f;

        [Tooltip("Los perseguidores rebotan entre ellos al chocarse.")]
        public float empujeRebote = 6f, saltoRebote = 2.5f;

        [Header("Tiempos")]
        public float cuentaRegresiva = 3f;
        public float pausaEntreRondas = 3f;
        public float pausaFinDePartida = 6f;

        public float TiempoParaGanar(int jugadores)
        {
            if (jugadores <= 2) return tiempoDosJugadores;
            return jugadores == 3 ? tiempoTresJugadores : tiempoCuatroJugadores;
        }
    }
}
