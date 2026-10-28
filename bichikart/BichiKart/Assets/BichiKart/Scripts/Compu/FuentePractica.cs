using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Rival de práctica: va a buscar la comida si está suelta, persigue al dueño si la tiene otro y escapa
    /// del perseguidor más cercano si la tiene él. Si se traba, retrocede un poco. No usa la gravedad ni
    /// busca las paredes a propósito (eso es para la compu de verdad), pero puede terminar subiendo por
    /// alguna porque las ruedas son magnéticas.
    /// </summary>
    [RequireComponent(typeof(Kart), typeof(Participante))]
    public sealed class FuentePractica : MonoBehaviour, IFuenteDeOrdenes
    {
        [Tooltip("Ángulo hacia el objetivo con el que ya dobla a fondo.")]
        [SerializeField] private float anguloGiroCompleto = 40f;

        [Tooltip("Qué tan lejos mira para esquivar paredes y obstáculos.")]
        [SerializeField] private float distanciaParaEsquivar = 5f;

        [Tooltip("Segundos casi quieto para darse cuenta de que está trabado.")]
        [SerializeField] private float tiempoTrabado = 1f;

        [Tooltip("Segundos de marcha atrás para destrabarse.")]
        [SerializeField] private float tiempoMarchaAtras = 0.7f;

        private Kart _kart;
        private Participante _yo;
        private Partida _partida;
        private float _trabado, _marchaAtras;

        private void Awake()
        {
            _kart = GetComponent<Kart>();
            _yo = GetComponent<Participante>();
            _partida = FindFirstObjectByType<Partida>();
        }

        public OrdenesDelKart LeerOrdenes()
        {
            if (_partida == null || _partida.Arbitro == null) return default;
            float dt = Time.fixedDeltaTime;

            Vector3 haciaDonde = Objetivo() - _kart.transform.position;
            Vector3 enElPiso = Vector3.ProjectOnPlane(haciaDonde, _kart.Arriba);
            float angulo = enElPiso.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(_kart.Adelante, enElPiso, _kart.Arriba) : 0f;
            float giro = Mathf.Clamp(angulo / anguloGiroCompleto, -1f, 1f);
            giro = Esquivar(giro);

            // Si casi no avanza durante un rato (y no está esperando la largada), está trabado.
            if (!_kart.Quieto && Mathf.Abs(_kart.VelocidadAdelante) < 1f) _trabado += dt;
            else _trabado = 0f;
            if (_trabado > tiempoTrabado)
            {
                _trabado = 0f;
                _marchaAtras = tiempoMarchaAtras;
            }
            if (_marchaAtras > 0f)
            {
                _marchaAtras -= dt;
                return new OrdenesDelKart { Avance = -1f, Giro = -giro };
            }

            return new OrdenesDelKart { Avance = Mathf.Abs(angulo) > 110f ? 0.5f : 1f, Giro = giro };
        }

        private Vector3 Objetivo()
        {
            var arbitro = _partida.Arbitro;
            if (arbitro.Duenio == Arbitro.Nadie) return _partida.Comida.Centro;
            if (arbitro.Duenio != _yo.Indice) return _partida.Participantes[arbitro.Duenio].Kart.transform.position;

            // Tiene la comida: escapa del perseguidor más cercano.
            var yo = _kart.transform.position;
            Vector3 cercano = yo;
            float menor = float.MaxValue;
            foreach (var otro in _partida.Participantes)
            {
                if (otro == _yo) continue;
                float d = (otro.Kart.transform.position - yo).sqrMagnitude;
                if (d >= menor) continue;
                menor = d;
                cercano = otro.Kart.transform.position;
            }
            return yo + (yo - cercano).normalized * 10f;
        }

        /// <summary>Si tiene algo adelante que no es piso, dobla para el lado que lo aleja.</summary>
        private float Esquivar(float giro)
        {
            var origen = _kart.transform.position;
            if (!Physics.Raycast(origen, _kart.Adelante, out var golpe, distanciaParaEsquivar, ~0, QueryTriggerInteraction.Ignore)) return giro;
            if (golpe.rigidbody != null || Vector3.Dot(golpe.normal, _kart.Arriba) > 0.6f) return giro;

            Vector3 rebote = Vector3.ProjectOnPlane(Vector3.Reflect(_kart.Adelante, golpe.normal), _kart.Arriba);
            float lado = Mathf.Sign(Vector3.SignedAngle(_kart.Adelante, rebote, _kart.Arriba));
            return Mathf.Clamp(giro + lado, -1f, 1f);
        }
    }
}
