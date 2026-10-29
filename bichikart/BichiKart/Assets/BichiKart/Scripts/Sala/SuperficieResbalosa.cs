using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Una superficie que resbala, como si fuera apenas abombada: encima el kart agarra menos y lo empuja
    /// hacia el borde, más fuerte cuanto más lejos del centro. Está arriba del terrón y del frasco, para
    /// que no se pueda aguantar la comida ahí para siempre.
    /// </summary>
    public sealed class SuperficieResbalosa : MonoBehaviour
    {
        [Tooltip("Qué parte del agarre y del rozamiento queda encima (de 0 a 1). Menos es más resbaloso.")]
        [Range(0f, 1f)] public float agarre = 0.2f;

        [Tooltip("Empuje hacia el borde, en el centro.")]
        public float empuje = 2f;

        [Tooltip("Cuánto crece el empuje por cada unidad que el kart se aleja del centro.")]
        public float empujePorDistancia = 1.5f;

        /// <summary>Aceleración hacia el borde para un kart en <paramref name="lugar"/>, apoyado con <paramref name="arriba"/>.</summary>
        public Vector3 Empuje(Vector3 lugar, Vector3 arriba)
        {
            var afuera = Vector3.ProjectOnPlane(lugar - transform.position, arriba);
            float distancia = afuera.magnitude;
            if (distancia < 1e-3f) return Vector3.zero;
            return afuera / distancia * (empuje + empujePorDistancia * distancia);
        }
    }
}
