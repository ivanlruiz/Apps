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

        [Header("Paredes, techo y gravedad")]
        [Tooltip("Ruedas magnéticas: la gravedad tira hacia la superficie que el kart tiene abajo, así se puede subir por las paredes hasta el techo.")]
        public bool ruedasMagneticas = true;

        [Tooltip("Velocidad con la que despega hacia la superficie de enfrente al apretar el botón de gravedad.")]
        public float impulsoGravedad = 9f;

        [Tooltip("Segundos que tarda en recargarse el botón de gravedad sin la comida.")]
        public float esperaGravedad = 2.5f;

        [Tooltip("Segundos que tarda en recargarse el botón de gravedad llevando la comida (así no se escapa siempre).")]
        public float esperaGravedadConComida = 5f;

        [Tooltip("Hasta qué distancia busca la superficie de enfrente.")]
        public float alcanceGravedad = 80f;

        [Header("Salto")]
        [Tooltip("Velocidad con la que despega al saltar.")]
        public float impulsoSalto = 9f;

        [Header("Derrape y mini turbo")]
        [Tooltip("Velocidad mínima para empezar a derrapar.")]
        public float velocidadMinimaDerrape = 7f;

        [Tooltip("Cuánto dobla derrapando, de lo más abierto (doblando para el otro lado) a lo más cerrado, comparado con el giro normal.")]
        public Vector2 giroDerrape = new Vector2(0.45f, 1.35f);

        [Tooltip("Cuánto se desliza hacia afuera de la curva al derrapar, comparado con la velocidad.")]
        [Range(0f, 1f)] public float deslizamientoDerrape = 0.3f;

        [Tooltip("Qué tan rápido se acomoda ese deslizamiento. Más bajo es más resbaloso.")]
        public float agarreDerrape = 4f;

        [Tooltip("Segundos de derrape para cargar el mini turbo.")]
        public float cargaMiniTurbo = 0.9f;

        [Tooltip("Segundos que dura el mini turbo.")]
        public float duracionTurbo = 0.8f;

        [Tooltip("Velocidad máxima con turbo, comparada con la normal.")]
        public float velocidadTurbo = 1.35f;

        [Tooltip("Cuánto gana de velocidad por segundo con turbo.")]
        public float aceleracionTurbo = 45f;

        [Tooltip("Qué parte del empujón del mini turbo recibe el que lleva la comida (de 0 a 1).")]
        [Range(0f, 1f)] public float turboConComida = 0.3f;
    }
}
