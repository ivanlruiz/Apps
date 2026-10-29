using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Un rival de la compu. Cada tanto decide qué hacer y, mientras tanto, maneja hacia su objetivo:
    /// <list type="bullet">
    /// <item>Si la comida está suelta, va a buscarla.</item>
    /// <item>Si la tiene otro, lo persigue (un poco adelantado a donde va a estar). Si el dueño está en la
    /// superficie de enfrente, más o menos encima, usa la gravedad; si está cerca de la otra boca del caño,
    /// se mete por el caño (apretando usar sobre la boca).</item>
    /// <item>Si la tiene él, se esconde detrás del terrón o del frasco; si alguien se acerca, escapa, y si lo
    /// tienen encima usa la gravedad o el caño para zafar.</item>
    /// </list>
    /// Como una persona, cuando el kart que mira usa la gravedad tarda un poquito en darse cuenta. Para
    /// llegar a una pared o al techo alcanza con manejar hacia donde está el objetivo: las ruedas magnéticas
    /// hacen el resto. Esquiva los obstáculos, derrapa en las curvas cerradas y, si se traba, retrocede.
    /// </summary>
    [RequireComponent(typeof(Kart), typeof(Participante))]
    public sealed class FuenteCompu : MonoBehaviour, IFuenteDeOrdenes
    {
        [SerializeField] private AjustesDeCompu ajustes;

        private Kart _kart;
        private Participante _yo;
        private Partida _partida;
        private Tuberia _tuberia;
        private Vector3 _objetivo;
        private bool _quiereGravedad, _llego;
        private int _boca = -1;
        private Kart _mirando;
        private float _proximaDecision, _reaccionHasta;
        private float _trabado, _marchaAtras, _derrape;
        private float _ladoMarchaAtras = 1f;

        /// <summary>Qué está haciendo ahora (para ver en el Inspector y en las pruebas).</summary>
        public string Intencion { get; private set; } = "";

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(AjustesDeCompu nuevosAjustes) => ajustes = nuevosAjustes;

        private void Awake()
        {
            _kart = GetComponent<Kart>();
            _yo = GetComponent<Participante>();
            _partida = FindFirstObjectByType<Partida>();
            _tuberia = FindFirstObjectByType<Tuberia>();
        }

        private void Start()
        {
            if (_partida == null) return;
            foreach (var otro in _partida.Participantes)
            {
                if (otro == _yo) continue;
                var kart = otro.Kart;
                kart.AlCambiarGravedad += () => VioGravedad(kart);
            }
        }

        /// <summary>Si el que mira usa la gravedad, sigue con lo que venía haciendo un ratito antes de reaccionar.</summary>
        private void VioGravedad(Kart kart)
        {
            if (ajustes == null || kart != _mirando) return;
            _reaccionHasta = Time.time + Random.Range(ajustes.reaccionAGravedad.x, ajustes.reaccionAGravedad.y);
            _proximaDecision = Mathf.Max(_proximaDecision, _reaccionHasta);
        }

        public OrdenesDelKart LeerOrdenes()
        {
            if (ajustes == null || _partida == null || _partida.Arbitro == null || _kart.EnCanio) return default;
            float dt = Time.fixedDeltaTime;

            if (Time.time >= _proximaDecision)
            {
                _proximaDecision = Time.time + ajustes.reaccion * Random.Range(0.8f, 1.2f);
                Decidir();
            }

            if (Destrabarse(dt, out var paraAtras)) return paraAtras;

            var ordenes = Manejar(dt);
            // La gravedad, solo cuando el botón está recargado (si no, se perdería el intento).
            if (_quiereGravedad && _kart.EnSuelo && _kart.GravedadLista && Time.time >= _reaccionHasta)
            {
                ordenes.Gravedad = true;
                _quiereGravedad = false;
            }
            // Llegó a la boca que buscaba: se mete.
            if (_boca >= 0 && _tuberia != null && _tuberia.BocaAlAlcance(_kart) == _boca) ordenes.Usar = true;
            return ordenes;
        }

        // ------------------------------------------------------------------ decisiones

        private void Decidir()
        {
            var arbitro = _partida.Arbitro;
            _quiereGravedad = false;
            _llego = false;
            _boca = -1;
            _mirando = null;

            if (arbitro.Duenio == Arbitro.Nadie)
            {
                Intencion = "Busca el pan";
                _objetivo = _partida.Comida.Centro;
                PensarGravedad(_objetivo, _partida.Comida.Arriba);
                PensarCanio(_objetivo, _partida.Comida.Arriba);
            }
            else if (arbitro.Duenio == _yo.Indice)
            {
                DecidirConLaComida();
            }
            else
            {
                Intencion = "Persigue";
                var duenio = _partida.Participantes[arbitro.Duenio].Kart;
                _mirando = duenio;
                _objetivo = duenio.transform.position + duenio.Adelante * (duenio.VelocidadAdelante * ajustes.anticipacion);
                PensarGravedad(duenio.transform.position, duenio.Arriba);
                PensarCanio(duenio.transform.position, duenio.Arriba);
            }
        }

        private void DecidirConLaComida()
        {
            var yo = _kart.transform.position;
            Kart cercano = null;
            float menor = float.MaxValue;
            foreach (var otro in _partida.Participantes)
            {
                if (otro == _yo) continue;
                float d = Vector3.Distance(otro.Kart.transform.position, yo);
                if (d >= menor) continue;
                menor = d;
                cercano = otro.Kart;
            }
            _mirando = cercano;

            if (cercano != null && menor < ajustes.distanciaParaHuir)
            {
                Intencion = "Escapa";
                var lejos = Vector3.ProjectOnPlane(yo - cercano.transform.position, _kart.Arriba);
                _objetivo = yo + lejos.normalized * 12f;

                // Si lo tiene encima en la misma superficie, se tira a la de enfrente o se mete en el caño.
                bool mismaSuperficie = Vector3.Dot(cercano.Arriba, _kart.Arriba) > 0.8f;
                if (ajustes.usaGravedad && mismaSuperficie && menor < ajustes.distanciaParaHuir * 0.5f) _quiereGravedad = true;
                BocaParaEscapar();
                return;
            }

            if (ajustes.seEsconde && cercano != null && Esconderse(cercano)) return;

            // Sin escondite a mano (por ejemplo, en una pared o en el techo): se aleja del más cercano.
            Intencion = "Se aleja";
            var alejarse = cercano != null ? Vector3.ProjectOnPlane(yo - cercano.transform.position, _kart.Arriba) : _kart.Adelante;
            _objetivo = yo + alejarse.normalized * 10f;
        }

        /// <summary>Va al lado del terrón o del frasco que queda tapado del perseguidor más cercano.</summary>
        private bool Esconderse(Kart perseguidor)
        {
            var cocina = _partida.Cocina;
            if (cocina.Escondites.Count == 0 || Vector3.Dot(_kart.Arriba, cocina.transform.up) < 0.8f) return false;

            var yo = _kart.transform.position;
            Vector3 mejor = yo;
            float menor = float.MaxValue;
            foreach (var escondite in cocina.Escondites)
            {
                var lejosDelPerseguidor = Vector3.ProjectOnPlane(escondite.Centro - perseguidor.transform.position, cocina.transform.up);
                if (lejosDelPerseguidor.sqrMagnitude < 1e-4f) continue;
                var lugar = escondite.Centro + lejosDelPerseguidor.normalized * (escondite.Radio + 2.2f);
                float d = Vector3.Distance(yo, lugar);
                if (d >= menor) continue;
                menor = d;
                mejor = lugar;
            }
            if (menor == float.MaxValue) return false;

            Intencion = "Se esconde";
            _objetivo = mejor;
            _llego = menor < 1.5f;
            return true;
        }

        /// <summary>Si el objetivo está en la superficie de enfrente, más o menos encima, conviene tirarse.</summary>
        private void PensarGravedad(Vector3 objetivo, Vector3 arribaDelObjetivo)
        {
            if (!ajustes.usaGravedad || Vector3.Dot(arribaDelObjetivo, _kart.Arriba) > -0.7f) return;
            float deCostado = Vector3.ProjectOnPlane(objetivo - _kart.transform.position, _kart.Arriba).magnitude;
            if (deCostado < ajustes.distanciaParaGravedad) _quiereGravedad = true;
        }

        /// <summary>Si hay una boca cerca en su superficie y el objetivo está cerca de la otra, va por el caño.</summary>
        private void PensarCanio(Vector3 objetivo, Vector3 arribaDelObjetivo)
        {
            if (!ajustes.usaCanio || _tuberia == null || _quiereGravedad) return;
            var yo = _kart.transform.position;
            for (int i = 0; i < 2; i++)
            {
                var (boca, normal) = _tuberia.Boca(i);
                var (otraBoca, otraNormal) = _tuberia.Boca(1 - i);
                if (Vector3.Dot(_kart.Arriba, normal) < 0.8f || Vector3.Dot(arribaDelObjetivo, otraNormal) < 0.5f) continue;
                float aBoca = Vector3.Distance(yo, boca);
                bool convieneMas = Vector3.Distance(otraBoca, objetivo) < ajustes.objetivoCercaDeLaOtraBoca && Vector3.Distance(yo, objetivo) > aBoca + 10f;
                if (aBoca < ajustes.distanciaABoca && convieneMas)
                {
                    Intencion += " (por el caño)";
                    _objetivo = boca;
                    _boca = i;
                    return;
                }
            }
        }

        private void BocaParaEscapar()
        {
            if (!ajustes.usaCanio || _tuberia == null) return;
            for (int i = 0; i < 2; i++)
            {
                var (boca, normal) = _tuberia.Boca(i);
                if (Vector3.Dot(_kart.Arriba, normal) < 0.8f || Vector3.Distance(_kart.transform.position, boca) > ajustes.distanciaABoca * 0.8f) continue;
                Intencion = "Escapa por el caño";
                _objetivo = boca;
                _boca = i;
                _quiereGravedad = false;
                return;
            }
        }

        // ------------------------------------------------------------------ manejo

        private OrdenesDelKart Manejar(float dt)
        {
            var yo = _kart.transform.position;
            var hacia = Vector3.ProjectOnPlane(_objetivo - yo, _kart.Arriba);
            float distancia = hacia.magnitude;
            float angulo = distancia > 1e-3f ? Vector3.SignedAngle(_kart.Adelante, hacia, _kart.Arriba) : 0f;

            float giro = Esquivar(Mathf.Clamp(angulo / ajustes.anguloGiroCompleto, -1f, 1f));
            float avance = Mathf.Abs(angulo) > 110f ? 0.45f : 1f;
            if (_llego) avance = 0f;
            else if (distancia < 3f) avance = Mathf.Min(avance, 0.4f + distancia / 5f);

            // Derrapa en las curvas cerradas yendo rápido; al enderezar, suelta (y sale el mini turbo).
            if (ajustes.derrapa && _derrape <= 0f && Mathf.Abs(angulo) > ajustes.anguloParaDerrapar && _kart.VelocidadAdelante > 11f) _derrape = 1.3f;
            bool derrapar = false;
            if (_derrape > 0f)
            {
                _derrape -= dt;
                derrapar = Mathf.Abs(angulo) > 15f;
                if (!derrapar) _derrape = 0f;
            }

            return new OrdenesDelKart { Avance = avance * ajustes.velocidad, Giro = giro, Derrapar = derrapar };
        }

        /// <summary>Si tiene el terrón o el frasco adelante, dobla para el lado que lo aleja.</summary>
        private float Esquivar(float giro)
        {
            if (!Physics.Raycast(_kart.transform.position, _kart.Adelante, out var golpe, ajustes.distanciaParaEsquivar, ~0, QueryTriggerInteraction.Ignore)) return giro;
            if (!_partida.Cocina.EsObstaculo(golpe.collider)) return giro;
            var rebote = Vector3.ProjectOnPlane(Vector3.Reflect(_kart.Adelante, golpe.normal), _kart.Arriba);
            float lado = Mathf.Sign(Vector3.SignedAngle(_kart.Adelante, rebote, _kart.Arriba));
            return Mathf.Clamp(giro + lado * 1.2f, -1f, 1f);
        }

        /// <summary>Si casi no avanza durante un segundo (y no está esperando la largada), retrocede un poco.</summary>
        private bool Destrabarse(float dt, out OrdenesDelKart ordenes)
        {
            ordenes = default;
            if (!_kart.Quieto && !_llego && Mathf.Abs(_kart.VelocidadAdelante) < 1f) _trabado += dt;
            else _trabado = 0f;
            if (_trabado > 1f)
            {
                _trabado = 0f;
                _marchaAtras = 0.7f;
                _ladoMarchaAtras = Random.value < 0.5f ? -1f : 1f;
            }
            if (_marchaAtras <= 0f) return false;

            _marchaAtras -= dt;
            ordenes = new OrdenesDelKart { Avance = -1f, Giro = _ladoMarchaAtras };
            return true;
        }
    }
}
