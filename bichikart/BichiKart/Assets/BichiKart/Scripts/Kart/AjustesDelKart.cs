using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Números del manejo. Se pueden cambiar mientras se juega para encontrar cómo se siente mejor.
    /// </summary>
    [CreateAssetMenu(menuName = "BichiKart/Ajustes del kart", fileName = "AjustesDelKart")]
    public sealed class AjustesDelKart : ScriptableObject
    {
        [Header("Avance")]
        [Tooltip("Velocidad máxima hacia adelante.")]
        public float velocidadMaxima = 16f;

        [Tooltip("Cuánto gana de velocidad por segundo al acelerar.")]
        public float aceleracion = 18f;

        [Tooltip("Cuánto pierde de velocidad por segundo al frenar.")]
        public float frenado = 40f;

        [Tooltip("Velocidad máxima en marcha atrás.")]
        public float velocidadMarchaAtras = 7f;

        [Tooltip("Cuánto pierde de velocidad por segundo sin acelerar ni frenar.")]
        public float rozamiento = 8f;

        [Header("Giro")]
        [Tooltip("Grados por segundo que dobla a toda velocidad.")]
        public float giro = 150f;

        [Tooltip("Por debajo de esta velocidad dobla menos (quieto no dobla).")]
        public float velocidadParaGiroCompleto = 5f;

        [Tooltip("Qué tan rápido se frena el deslizamiento de costado. Más es más pegado al piso.")]
        public float agarre = 8f;

        [Tooltip("Cuánto se puede doblar en el aire (de 0 a 1).")]
        [Range(0f, 1f)] public float controlEnElAire = 0.3f;

        [Header("Cuerpo")]
        [Tooltip("Fuerza de gravedad.")]
        public float gravedad = 30f;

        [Tooltip("Radio de la esfera invisible que choca. La parte de abajo es donde apoyan las ruedas.")]
        public float radioEsfera = 0.7f;

        [Tooltip("Hasta qué distancia del piso se considera que el kart está apoyado.")]
        public float distanciaSuelo = 0.3f;

        [Tooltip("Qué tan rápido se acomoda el kart a la inclinación del piso.")]
        public float alineacion = 12f;

        [Tooltip("Suavizado del kart que se ve. Más es más pegado a la física.")]
        public float suavizadoChasis = 20f;
    }
}
