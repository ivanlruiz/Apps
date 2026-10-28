using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Cómo juega la compu. Por ahora hay una sola dificultad; más adelante cada nivel (Fácil, Normal,
    /// Difícil) va a ser un archivo de estos con otros números.
    /// </summary>
    [CreateAssetMenu(menuName = "BichiKart/Ajustes de la compu", fileName = "AjustesDeCompu")]
    public sealed class AjustesDeCompu : ScriptableObject
    {
        [Header("Manejo")]
        [Tooltip("Segundos entre una decisión y la siguiente. Más es más lenta para reaccionar.")]
        public float reaccion = 0.25f;

        [Tooltip("Ángulo hacia el objetivo con el que ya dobla a fondo.")]
        public float anguloGiroCompleto = 35f;

        [Tooltip("Cuánto se adelanta a donde va a estar el que persigue, en segundos.")]
        public float anticipacion = 0.35f;

        [Tooltip("Qué tan lejos mira para esquivar el terrón y el frasco.")]
        public float distanciaParaEsquivar = 5f;

        [Header("Derrape")]
        [Tooltip("Derrapa en las curvas cerradas (y suelta el mini turbo al salir).")]
        public bool derrapa = true;

        [Tooltip("Ángulo hacia el objetivo a partir del cual derrapa.")]
        public float anguloParaDerrapar = 55f;

        [Header("Paredes, techo y gravedad")]
        [Tooltip("Usa el botón de gravedad cuando el objetivo está en la superficie de enfrente, más o menos encima.")]
        public bool usaGravedad = true;

        [Tooltip("Qué tan cerca (de costado) tiene que estar el objetivo en la superficie de enfrente para tirarse.")]
        public float distanciaParaGravedad = 8f;

        [Tooltip("Segundos de espera entre dos usos de la gravedad.")]
        public float esperaEntreGravedades = 4f;

        [Header("Caño")]
        [Tooltip("Usa el caño para llegar a la otra punta o para escapar.")]
        public bool usaCanio = true;

        [Tooltip("Qué tan cerca de una boca tiene que estar para ir a buscarla.")]
        public float distanciaABoca = 10f;

        [Tooltip("Qué tan cerca de la otra boca tiene que estar el objetivo para que convenga el caño.")]
        public float objetivoCercaDeLaOtraBoca = 14f;

        [Header("Con la comida")]
        [Tooltip("Cuando tiene la comida y nadie está cerca, se esconde detrás del terrón o del frasco.")]
        public bool seEsconde = true;

        [Tooltip("Si un perseguidor se acerca a menos de esto, escapa.")]
        public float distanciaParaHuir = 9f;
    }
}
