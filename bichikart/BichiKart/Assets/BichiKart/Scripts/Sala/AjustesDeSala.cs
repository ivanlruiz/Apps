using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Medidas de la sala. Se pueden cambiar mientras se juega: la sala se vuelve a armar sola.
    /// </summary>
    [CreateAssetMenu(menuName = "BichiKart/Ajustes de sala", fileName = "AjustesDeSala")]
    public sealed class AjustesDeSala : ScriptableObject
    {
        [Tooltip("Ancho (x), alto (y) y largo (z) de la sala. Un kart mide unos 2,2 de largo.")]
        public Vector3 medidas = new Vector3(40f, 14f, 32f);

        [Tooltip("Radio de los rincones redondeados por donde se pasa manejando del piso a las paredes y al techo.")]
        public float radioRincones = 3.5f;

        [Tooltip("Tamaño de cada azulejo de las paredes.")]
        public float tamanoAzulejo = 1.2f;

        /// <summary>Cambia cuando cambia cualquier medida, para saber cuándo volver a armar la sala.</summary>
        public int Huella() => System.HashCode.Combine(medidas, radioRincones, tamanoAzulejo);

        private void OnValidate()
        {
            medidas = Vector3.Max(medidas, new Vector3(12f, 6f, 12f));
            radioRincones = Mathf.Clamp(radioRincones, 0.5f, Mathf.Min(medidas.x, medidas.y, medidas.z) * 0.45f);
            tamanoAzulejo = Mathf.Max(0.2f, tamanoAzulejo);
        }
    }
}
