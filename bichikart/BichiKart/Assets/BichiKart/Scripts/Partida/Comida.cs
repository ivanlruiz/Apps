using System.Collections.Generic;
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
        [SerializeField] private float tamanoSobreKart = 0.42f;

        [Tooltip("Partes del aro de tiempo que rodea la comida mientras alguien la tiene.")]
        [SerializeField] private int partesDelAro = 20;

        private Formas _formas;
        private Transform _modelo;
        private Transform _aro;
        private readonly List<Renderer> _partesDelAro = new List<Renderer>();
        private Material _apagado, _encendido;
        private float _progreso;
        private Color _colorDelAro = Color.white;
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

        /// <summary>Cuánto le falta al dueño para ganar (de 0 a 1) y su color, para el aro.</summary>
        public void MostrarProgreso(float progreso, Color color)
        {
            _progreso = progreso;
            _colorDelAro = color;
        }

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
            ActualizarAro();
        }

        /// <summary>El aro solo se ve cuando alguien tiene la comida, siempre de frente a la cámara.</summary>
        private void ActualizarAro()
        {
            var camara = Camera.main;
            bool visible = _duenio != null && camara != null;
            _aro.gameObject.SetActive(visible);
            if (!visible) return;

            _aro.rotation = Quaternion.LookRotation(_aro.position - camara.transform.position, camara.transform.up);
            _encendido.SetColor("_BaseColor", _colorDelAro);
            _encendido.SetColor("_EmissionColor", _colorDelAro * 1.6f);
            int prendidas = Mathf.RoundToInt(_progreso * _partesDelAro.Count);
            for (int i = 0; i < _partesDelAro.Count; i++) _partesDelAro[i].sharedMaterial = i < prendidas ? _encendido : _apagado;
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
            ArmarAro();
        }

        private void ArmarAro()
        {
            // Partes redondeadas en círculo, que se prenden con el color del dueño a medida que aguanta.
            _aro = _formas.Nodo("Aro de tiempo", transform);
            _aro.localPosition = new Vector3(0f, 0.25f, 0f);
            _apagado = _formas.Mat(new Color(0.92f, 0.9f, 0.96f), 0.6f);
            _encendido = _formas.Emisivo(Color.white, 1.6f);
            _partesDelAro.Clear();
            const float radio = 1.9f;
            for (int i = 0; i < partesDelAro; i++)
            {
                float angulo = i * 360f / partesDelAro;
                var parte = _formas.Caja("Parte", _aro, Quaternion.Euler(0f, 0f, -angulo) * new Vector3(0f, radio, 0f), new Vector3(0.42f, 0.2f, 0.14f), 0.07f, _apagado);
                parte.localRotation = Quaternion.Euler(0f, 0f, -angulo);
                Formas.SinSombra(parte);
                _partesDelAro.Add(parte.GetComponent<Renderer>());
            }
            _aro.gameObject.SetActive(false);
        }
    }
}
