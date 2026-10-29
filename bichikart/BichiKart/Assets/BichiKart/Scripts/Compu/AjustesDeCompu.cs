using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Cómo juega la compu. Cada nivel (Fácil, Normal y Difícil) es un archivo de estos con otros números;
    /// los valores de acá son los de Difícil.
    /// </summary>
    [CreateAssetMenu(menuName = "BichiKart/Ajustes de la compu", fileName = "AjustesDeCompu")]
    public sealed class AjustesDeCompu : ScriptableObject
    {
        [Header("Manejo")]
        [Tooltip("Segundos entre una decisión y la siguiente. Más es más lenta para reaccionar.")]
        public float reaccion = 0.25f;

        [Tooltip("Qué parte de su velocidad máxima usa (1 = toda).")]
        [Range(0.5f, 1f)] public float velocidad = 1f;

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

        [Tooltip("Segundos (de mínimo a máximo, al azar) que tarda en reaccionar cuando el kart que persigue, o del que escapa, usa la gravedad.")]
        public Vector2 reaccionAGravedad = new Vector2(0.5f, 0.8f);

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
