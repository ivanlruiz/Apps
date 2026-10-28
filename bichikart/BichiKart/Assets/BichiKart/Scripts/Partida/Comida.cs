using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// La rebanada de pan gigante. Suelta, flota y gira despacio sobre la superficie donde quedó; cuando un
    /// kart la tiene, viaja chiquita arriba de él. Las reglas las lleva el <see cref="Arbitro"/>: este
    /// componente solo la muestra. Se arma desde código y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class Comida : MonoBehaviour
    {
        [Tooltip("Altura a la que flota suelta, sobre la superficie.")]
        [SerializeField] private float alturaSuelta = 1.8f;

        [Tooltip("Altura sobre el kart cuando alguien la tiene.")]
        [SerializeField] private float alturaSobreKart = 2.3f;

        [Tooltip("Tamaño arriba del kart, comparado con el tamaño suelta.")]
        [SerializeField] private float tamanoSobreKart = 0.55f;

        private Formas _formas;
        private Transform _modelo;
        private Kart _duenio;
        private Vector3 _lugarSuelta;
        private Vector3 _arribaSuelta = Vector3.up;
        private float _giro;

        /// <summary>Quién la tiene (null si está suelta).</summary>
        public Kart Duenio => _duenio;

        /// <summary>Punto que hay que tocar para agarrarla.</summary>
        public Vector3 Centro => transform.position;

        public Vector3 Arriba => _duenio != null ? _duenio.Arriba : _arribaSuelta;

        private void OnEnable() => Construir();

        private void OnDisable()
        {
            _formas?.Limpiar();
            _formas = null;
        }

        /// <summary>Queda suelta, flotando sobre un lugar de una superficie.</summary>
        public void Soltar(Vector3 sobreLaSuperficie, Vector3 arriba)
        {
            _duenio = null;
            _arribaSuelta = arriba.normalized;
            _lugarSuelta = sobreLaSuperficie + _arribaSuelta * alturaSuelta;
        }

        public void Seguir(Kart kart) => _duenio = kart;

        private void Update()
        {
            if (!Application.isPlaying || _modelo == null) return;
            float dt = Time.deltaTime;
            float t = Time.time;
            _giro += dt * (_duenio != null ? 70f : 35f);

            Vector3 lugar;
            Vector3 arriba;
            float tamano;
            if (_duenio != null)
            {
                arriba = _duenio.Arriba;
                lugar = _duenio.Chasis.position + arriba * alturaSobreKart;
                tamano = tamanoSobreKart;
            }
            else
            {
                arriba = _arribaSuelta;
                lugar = _lugarSuelta + arriba * (Mathf.Sin(t * 2f) * 0.15f);
                tamano = 1f;
            }

            // Arriba del kart la sigue pegada; suelta va y viene suave.
            float suavizado = 1f - Mathf.Exp(-(_duenio != null ? 25f : 6f) * dt);
            transform.position = Vector3.Lerp(transform.position, lugar, suavizado);
            var giro = Quaternion.LookRotation(Vector3.ProjectOnPlane(Quaternion.AngleAxis(_giro, arriba) * Perpendicular(arriba), arriba), arriba);
            transform.rotation = Quaternion.Slerp(transform.rotation, giro * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f) * 6f), suavizado);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * tamano, 1f - Mathf.Exp(-8f * dt));
        }

        private static Vector3 Perpendicular(Vector3 v) => Mathf.Abs(Vector3.Dot(v, Vector3.forward)) < 0.9f ? Vector3.Cross(v, Vector3.forward) : Vector3.Cross(v, Vector3.right);

        private void Construir()
        {
            _formas?.Limpiar();
            _formas = new Formas();

            // La rebanada parada, con la corteza tostada, la miga clara y un brillo suave.
            _modelo = _formas.Nodo("Rebanada de pan", transform);
            var corteza = _formas.Mat(Formas.Hex("#c98a4b"), 0.2f);
            var miga = _formas.Mat(Formas.Hex("#f6dfae"), 0.1f);
            _formas.Caja("Corteza", _modelo, Vector3.zero, new Vector3(2.2f, 2f, 0.45f), 0.2f, corteza);
            _formas.Esfera("Copete izquierdo", _modelo, new Vector3(-0.55f, 1f, 0f), new Vector3(1.25f, 0.95f, 0.45f), corteza);
            _formas.Esfera("Copete derecho", _modelo, new Vector3(0.55f, 1f, 0f), new Vector3(1.25f, 0.95f, 0.45f), corteza);
            _formas.Caja("Miga", _modelo, new Vector3(0f, -0.05f, -0.03f), new Vector3(1.9f, 1.75f, 0.42f), 0.15f, miga);
            _formas.Esfera("Miga izquierda", _modelo, new Vector3(-0.5f, 0.85f, -0.03f), new Vector3(1.05f, 0.8f, 0.42f), miga);
            _formas.Esfera("Miga derecha", _modelo, new Vector3(0.5f, 0.85f, -0.03f), new Vector3(1.05f, 0.8f, 0.42f), miga);
            _formas.Luz("Brillo", _modelo, Vector3.zero, Formas.Hex("#ffe2a8"), 6f, 5f, false);
        }
    }
}
