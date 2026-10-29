using System;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Física de un kart, estilo arcade. Lo que choca es una esfera invisible con Rigidbody que nunca gira;
    /// hacia dónde mira el kart lo lleva este script, que además gira el <see cref="Chasis"/> (el kart que
    /// se ve, al que sigue la cámara). La gravedad la pone el juego, no Unity: con las ruedas magnéticas
    /// apunta a la superficie que el kart tiene abajo, y el botón de gravedad la da vuelta.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Kart : MonoBehaviour
    {
        /// <summary>Después de saltar o de usar la gravedad, un ratito no se considera apoyado.</summary>
        private const float TiempoDeDespegue = 0.15f;

        [SerializeField] private AjustesDelKart ajustes;

        [Tooltip("El kart que se ve. Lo gira este script y la cámara lo sigue.")]
        [SerializeField] private Transform chasis;

        [Tooltip("Capas sobre las que se puede manejar.")]
        [SerializeField] private LayerMask capaSuelo = ~0;

        private Rigidbody _rb;
        private SphereCollider _esfera;
        private Vector3 _adelante = Vector3.forward;
        private Vector3 _arriba = Vector3.up;
        private Vector3 _gravedad = Vector3.down;
        private Quaternion _rotacionChasis = Quaternion.identity;
        private float _cargaGravedad = 1f;
        private float _despegue;
        private float _cargaDerrape;
        private float _turbo;
        private float _aturdido;
        private SuperficieResbalosa _resbalosa;

        /// <summary>De dónde salen las órdenes. Si no se asigna, se busca en el mismo objeto.</summary>
        public IFuenteDeOrdenes Fuente { get; set; }

        /// <summary>Se usó el botón de gravedad y el kart despegó hacia la superficie de enfrente.</summary>
        public event Action AlCambiarGravedad;

        public event Action AlSaltar;

        public event Action AlTocarBocina;

        /// <summary>Se soltó un derrape con la carga completa y arrancó el mini turbo.</summary>
        public event Action AlArrancarTurbo;

        public AjustesDelKart Ajustes => ajustes;
        public Transform Chasis => chasis;

        /// <summary>Hacia dónde mira el kart, sobre la superficie en la que está.</summary>
        public Vector3 Adelante => _adelante;

        /// <summary>El "arriba" del kart: la normal de la superficie en la que está apoyado.</summary>
        public Vector3 Arriba => _arriba;

        public bool EnSuelo { get; private set; }

        /// <summary>Sobre qué está apoyado (null en el aire): el piso, una pared, el terrón, el frasco...</summary>
        public Collider Suelo { get; private set; }

        /// <summary>Velocidad hacia donde mira el kart (negativa en marcha atrás).</summary>
        public float VelocidadAdelante { get; private set; }

        public OrdenesDelKart UltimasOrdenes { get; private set; }

        public bool Derrapando { get; private set; }

        /// <summary>-1 si derrapa hacia la izquierda y 1 si hacia la derecha (0 sin derrapar).</summary>
        public int DireccionDerrape { get; private set; }

        /// <summary>De 0 a 1: qué tan cargado está el mini turbo del derrape.</summary>
        public float CargaDerrape => ajustes != null && ajustes.cargaMiniTurbo > 0f ? Mathf.Clamp01(_cargaDerrape / ajustes.cargaMiniTurbo) : 0f;

        public bool EnTurbo => _turbo > 0f;

        /// <summary>Ignora las órdenes (por ejemplo, durante la cuenta regresiva).</summary>
        public bool Quieto { get; set; }

        /// <summary>Cuánto más rápido o más lento va: lo pone la partida según quién tiene la comida.</summary>
        public float MultiplicadorVelocidad { get; set; } = 1f;

        /// <summary>Lleva la comida: lo pone la partida. La gravedad se recarga más lento y el mini turbo empuja menos.</summary>
        public bool LlevaComida { get; set; }

        /// <summary>De 0 a 1: cuánto se recargó el botón de gravedad. Adentro de un caño no se recarga.</summary>
        public float CargaGravedad => _cargaGravedad;

        public bool GravedadLista => _cargaGravedad >= 1f;

        /// <summary>Vuela hacia la superficie de enfrente después de usar la gravedad, hasta apoyarse.</summary>
        public bool CayendoPorGravedad { get; private set; }

        /// <summary>Hacia dónde tira la gravedad del kart.</summary>
        public Vector3 Abajo => _gravedad;

        /// <summary>Viaja por adentro de un caño: lo mueve la tubería, no la física.</summary>
        public bool EnCanio { get; private set; }

        /// <summary>Entra a un caño: deja de chocar y de tener gravedad hasta salir.</summary>
        public void EntrarAlCanio()
        {
            EnCanio = true;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
            EnSuelo = false;
            Suelo = null;
            _resbalosa = null;
            CayendoPorGravedad = false;
            Derrapando = false;
            DireccionDerrape = 0;
            _cargaDerrape = 0f;
        }

        /// <summary>La tubería lo pone en un lugar del caño, mirando hacia donde avanza.</summary>
        public void MoverEnCanio(Vector3 centro, Vector3 adelante, Vector3 arriba, float velocidad)
        {
            _rb.MovePosition(centro);
            _adelante = adelante;
            _arriba = arriba;
            _rotacionChasis = Quaternion.LookRotation(adelante, arriba);
            VelocidadAdelante = velocidad;
        }

        /// <summary>Sale del caño apoyado en una superficie, avanzando con una velocidad.</summary>
        public void SalirDelCanio(Vector3 sobreLaSuperficie, Vector3 arriba, Vector3 adelante, float velocidad)
        {
            EnCanio = false;
            _gravedad = -arriba;
            _arriba = arriba;
            _adelante = Vector3.ProjectOnPlane(adelante, arriba).normalized;
            _rotacionChasis = Quaternion.LookRotation(_adelante, _arriba);
            var centro = sobreLaSuperficie + arriba * ajustes.radioEsfera;
            _rb.isKinematic = false;
            _rb.detectCollisions = true;
            _rb.position = centro;
            transform.position = centro;
            _rb.linearVelocity = _adelante * velocidad;
        }

        /// <summary>
        /// Empuja el kart lejos de un punto (hacia atrás y un poco hacia arriba, para que se note) y lo deja
        /// unos segundos sin poder manejar. Adentro de un caño no se lo puede empujar.
        /// </summary>
        public void Despedir(Vector3 desde, float empuje, float salto, float aturdido)
        {
            if (EnCanio) return;
            var lejos = Vector3.ProjectOnPlane(_rb.position - desde, _arriba);
            if (lejos.sqrMagnitude < 1e-4f) lejos = -_adelante;
            _rb.linearVelocity = lejos.normalized * empuje + _arriba * salto + Vector3.ProjectOnPlane(_rb.linearVelocity, _arriba) * 0.3f;
            EnSuelo = false;
            _despegue = TiempoDeDespegue;
            _aturdido = Mathf.Max(_aturdido, aturdido);
            Derrapando = false;
            DireccionDerrape = 0;
            _cargaDerrape = 0f;
        }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(AjustesDelKart nuevosAjustes, Transform nuevoChasis, LayerMask nuevaCapaSuelo)
        {
            ajustes = nuevosAjustes;
            chasis = nuevoChasis;
            capaSuelo = nuevaCapaSuelo;
        }

        private void Awake()
        {
            if (ajustes == null)
            {
                Debug.LogError($"[Kart] A \"{name}\" le faltan los ajustes del kart.", this);
                enabled = false;
                return;
            }

            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _esfera = GetComponent<SphereCollider>();
            _esfera.radius = ajustes.radioEsfera;
            _esfera.center = Vector3.zero;
            _esfera.sharedMaterial = new PhysicsMaterial("Kart sin roce")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            if (Fuente == null) Fuente = GetComponent<IFuenteDeOrdenes>();

            var mira = chasis != null ? chasis.forward : transform.forward;
            _adelante = Vector3.ProjectOnPlane(mira, _arriba).normalized;
            _rotacionChasis = Quaternion.LookRotation(_adelante, _arriba);
        }

        /// <summary>Pone el kart quieto en un lugar del piso, mirando hacia un lado.</summary>
        public void Ubicar(Vector3 sobreElPiso, Vector3 mirando)
        {
            _gravedad = Vector3.down;
            _arriba = Vector3.up;
            _adelante = Vector3.ProjectOnPlane(mirando, _arriba).normalized;
            _rotacionChasis = Quaternion.LookRotation(_adelante, _arriba);
            _despegue = _turbo = _cargaDerrape = _aturdido = 0f;
            _cargaGravedad = 1f;
            Derrapando = false;
            DireccionDerrape = 0;
            EnCanio = false;
            CayendoPorGravedad = false;
            Suelo = null;
            _resbalosa = null;

            var centro = sobreElPiso + _arriba * ajustes.radioEsfera;
            transform.position = centro;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.detectCollisions = true;
                _rb.position = centro;
                _rb.linearVelocity = Vector3.zero;
            }
            if (chasis != null) chasis.SetPositionAndRotation(sobreElPiso, _rotacionChasis);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var ordenes = Fuente != null ? Fuente.LeerOrdenes() : default;
            _aturdido = Mathf.Max(0f, _aturdido - dt);
            if (Quieto || _aturdido > 0f) ordenes = default;
            UltimasOrdenes = ordenes;
            if (EnCanio) return;

            // El radio se puede cambiar mientras se juega.
            if (_esfera.radius != ajustes.radioEsfera) _esfera.radius = ajustes.radioEsfera;

            // La gravedad se recarga según quién lleva la comida en cada momento (adentro del caño, no).
            float espera = LlevaComida ? ajustes.esperaGravedadConComida : ajustes.esperaGravedad;
            _cargaGravedad = espera > 0f ? Mathf.Min(1f, _cargaGravedad + dt / espera) : 1f;

            DetectarSuelo(dt);
            if (ordenes.Gravedad) CambiarGravedad();
            if (ordenes.Saltar) Saltar();
            if (ordenes.Bocina) AlTocarBocina?.Invoke();
            ActualizarDerrape(ordenes, dt);
            Manejar(ordenes, dt);
            _rb.AddForce(_gravedad * ajustes.gravedad, ForceMode.Acceleration);
            _rotacionChasis = Quaternion.LookRotation(_adelante, _arriba);
        }

        private void Update()
        {
            // El chasis acompaña a la esfera (que ya viene interpolada) y gira suave hacia donde mira.
            if (chasis == null) return;
            chasis.position = transform.position - _arriba * ajustes.radioEsfera;
            chasis.rotation = Quaternion.Slerp(chasis.rotation, _rotacionChasis, 1f - Mathf.Exp(-ajustes.suavizadoChasis * Time.deltaTime));
        }

        // ------------------------------------------------------------------ suelo y gravedad

        private void DetectarSuelo(float dt)
        {
            float radio = ajustes.radioEsfera;
            bool toca = Physics.SphereCast(_rb.position - _gravedad * 0.05f, radio * 0.9f, _gravedad, out var golpe,
                radio * 0.1f + 0.05f + ajustes.distanciaSuelo, capaSuelo, QueryTriggerInteraction.Ignore);
            if (_despegue > 0f)
            {
                _despegue -= dt;
                toca = false;
            }
            // Arriba del terrón o del frasco las ruedas magnéticas no tuercen la gravedad, y en sus bordes y
            // costados no se apoya: de ahí se cae al piso (no queda colgado del costado).
            bool obstaculo = toca && EsObstaculo(golpe.collider);
            if (obstaculo && Vector3.Dot(golpe.normal, -_gravedad) < 0.8f) toca = false;
            EnSuelo = toca;
            if (toca) CayendoPorGravedad = false;
            var suelo = toca ? golpe.collider : null;
            if (suelo != Suelo)
            {
                Suelo = suelo;
                _resbalosa = suelo != null ? suelo.GetComponent<SuperficieResbalosa>() : null;
            }
            float suavizado = 1f - Mathf.Exp(-ajustes.alineacion * dt);

            // Ruedas magnéticas: apoyado, la gravedad tira hacia la superficie que tiene abajo. Así, al pasar
            // por un rincón redondeado, el kart sube por la pared y sigue hasta el techo.
            if (toca && ajustes.ruedasMagneticas && !obstaculo)
                _gravedad = Vector3.Slerp(_gravedad, -golpe.normal, suavizado).normalized;

            var arribaBuscado = toca ? golpe.normal : -_gravedad;

            // Si hay que darse vuelta del todo (después del botón de gravedad), el kart gira como un tonel,
            // alrededor de hacia dónde mira, y no hacia un costado cualquiera.
            if (Vector3.Dot(_arriba, arribaBuscado) < -0.95f) _arriba = Quaternion.AngleAxis(12f, _adelante) * _arriba;
            _arriba = Vector3.Slerp(_arriba, arribaBuscado, suavizado).normalized;
        }

        private Collider _ultimoRevisado;
        private bool _ultimoEsObstaculo;

        private bool EsObstaculo(Collider collider)
        {
            if (collider != _ultimoRevisado)
            {
                _ultimoRevisado = collider;
                _ultimoEsObstaculo = collider != null && collider.GetComponent<Obstaculo>() != null;
            }
            return _ultimoEsObstaculo;
        }

        /// <summary>
        /// Botón de gravedad: tira el kart hacia la superficie de enfrente (piso ↔ techo, pared ↔ pared).
        /// Solo funciona apoyado y con el botón recargado (ver <see cref="CargaGravedad"/>).
        /// </summary>
        private void CambiarGravedad()
        {
            if (!EnSuelo || !GravedadLista) return;
            if (!Physics.Raycast(_rb.position, _arriba, out _, ajustes.alcanceGravedad, capaSuelo, QueryTriggerInteraction.Ignore)) return;

            _cargaGravedad = 0f;
            _gravedad = _arriba;

            // Despega de golpe: se saca la velocidad hacia la superficie de abajo y se empuja hacia la de enfrente.
            Despegar(ajustes.impulsoGravedad);
            CayendoPorGravedad = true;
            AlCambiarGravedad?.Invoke();
        }

        /// <summary>
        /// Dónde va a caer el kart si sigue volando como ahora: cuánto le falta para llegar a la superficie
        /// (con la velocidad hacia ella y la gravedad) y cuánto se corre de costado mientras tanto.
        /// </summary>
        public bool BuscarDondeCae(out Vector3 punto, out Vector3 normal)
        {
            punto = normal = Vector3.zero;
            if (_rb == null || EnCanio) return false;
            var v = _rb.linearVelocity;
            float hacia = Vector3.Dot(v, _gravedad);
            return Caida(_rb.position, _gravedad, hacia, v - _gravedad * hacia, out punto, out normal, out _);
        }

        /// <summary>
        /// Dónde caería si usara la gravedad ahora mismo, y sobre qué (la compu lo usa para tirarse justo
        /// encima de alguien).
        /// </summary>
        public bool PredecirCaida(out Vector3 punto, out Collider suelo)
        {
            punto = Vector3.zero;
            suelo = null;
            if (_rb == null || EnCanio || !EnSuelo) return false;
            var deCostado = Vector3.ProjectOnPlane(_rb.linearVelocity, _arriba);
            return Caida(_rb.position, _arriba, ajustes.impulsoGravedad, deCostado, out punto, out _, out suelo);
        }

        /// <summary>
        /// Cayendo hacia <paramref name="direccion"/> con una velocidad hacia allá y otra de costado: cuánto
        /// tarda en llegar (con la gravedad) y dónde toca. Se calcula varias veces, porque al correrse de
        /// costado puede caer sobre otra cosa (por ejemplo, arriba del frasco en vez del piso).
        /// </summary>
        private bool Caida(Vector3 posicion, Vector3 direccion, float hacia, Vector3 deCostado, out Vector3 punto, out Vector3 normal, out Collider suelo)
        {
            punto = normal = Vector3.zero;
            suelo = null;
            if (!Physics.Raycast(posicion, direccion, out var golpe, ajustes.alcanceGravedad, capaSuelo, QueryTriggerInteraction.Ignore)) return false;

            float g = Mathf.Max(ajustes.gravedad, 0.01f);
            for (int vuelta = 0; vuelta < 3; vuelta++)
            {
                float distancia = Mathf.Max(0f, Vector3.Dot(golpe.point - posicion, direccion) - ajustes.radioEsfera);
                float tiempo = (-hacia + Mathf.Sqrt(Mathf.Max(0f, hacia * hacia + 2f * g * distancia))) / g;
                var desde = posicion + deCostado * tiempo - direccion * 0.5f;
                if (!Physics.Raycast(desde, direccion, out var caida, ajustes.alcanceGravedad, capaSuelo, QueryTriggerInteraction.Ignore)) break;
                golpe = caida;
            }
            punto = golpe.point;
            normal = golpe.normal;
            suelo = golpe.collider;
            return true;
        }

        private void Saltar()
        {
            if (!EnSuelo) return;
            Despegar(ajustes.impulsoSalto);
            AlSaltar?.Invoke();
        }

        /// <summary>Cambia la velocidad hacia arriba del kart por la pedida y lo deja en el aire.</summary>
        private void Despegar(float velocidadHaciaArriba)
        {
            var v = _rb.linearVelocity;
            _rb.linearVelocity = v - Vector3.Project(v, _arriba) + _arriba * velocidadHaciaArriba;
            EnSuelo = false;
            _despegue = TiempoDeDespegue;
        }

        // ------------------------------------------------------------------ derrape

        private void ActualizarDerrape(OrdenesDelKart ordenes, float dt)
        {
            _turbo = Mathf.Max(0f, _turbo - dt);

            if (!Derrapando)
            {
                // Se empieza a derrapar apretando el derrape mientras se dobla, con algo de velocidad.
                if (ordenes.Derrapar && EnSuelo && Mathf.Abs(ordenes.Giro) > 0.3f && VelocidadAdelante > ajustes.velocidadMinimaDerrape)
                {
                    Derrapando = true;
                    DireccionDerrape = ordenes.Giro > 0f ? 1 : -1;
                    _cargaDerrape = 0f;
                }
                return;
            }

            if (ordenes.Derrapar && EnSuelo && VelocidadAdelante > ajustes.velocidadMinimaDerrape * 0.6f)
            {
                _cargaDerrape += dt;
                return;
            }

            // Si se suelta con la carga completa, sale el mini turbo. Si se frenó o se despegó, se pierde.
            if (!ordenes.Derrapar && _cargaDerrape >= ajustes.cargaMiniTurbo)
            {
                _turbo = ajustes.duracionTurbo;
                AlArrancarTurbo?.Invoke();
            }
            Derrapando = false;
            DireccionDerrape = 0;
            _cargaDerrape = 0f;
        }

        // ------------------------------------------------------------------ manejo

        private void Manejar(OrdenesDelKart ordenes, float dt)
        {
            Vector3 v = _rb.linearVelocity;
            Vector3 vNormal = Vector3.Project(v, _arriba);
            Vector3 vPlano = v - vNormal;

            Vector3 adelante = Vector3.ProjectOnPlane(_adelante, _arriba);
            if (adelante.sqrMagnitude < 1e-6f) adelante = Vector3.ProjectOnPlane(_rotacionChasis * Vector3.forward, _arriba);
            adelante.Normalize();

            float vAdelante = Vector3.Dot(vPlano, adelante);
            Vector3 vLateral = vPlano - adelante * vAdelante;

            // Doblar: más rápido cuanto más rápido vas, al revés en marcha atrás y poco en el aire.
            // Derrapando se dobla siempre hacia el lado del derrape: más cerrado doblando hacia ese lado y
            // más abierto doblando hacia el otro.
            float giro = ordenes.Giro;
            if (Derrapando)
            {
                float hacia = (ordenes.Giro * DireccionDerrape + 1f) * 0.5f;
                giro = DireccionDerrape * Mathf.Lerp(ajustes.giroDerrape.x, ajustes.giroDerrape.y, hacia);
            }
            float control = EnSuelo ? 1f : ajustes.controlEnElAire;
            float factor = Mathf.Clamp01(Mathf.Abs(vAdelante) / ajustes.velocidadParaGiroCompleto);
            float sentido = vAdelante < -0.1f ? -1f : 1f;
            adelante = Quaternion.AngleAxis(giro * ajustes.giro * factor * sentido * control * dt, _arriba) * adelante;

            // Sobre una superficie resbalosa se agarra menos (y sin acelerar casi no frena).
            var resbalosa = EnSuelo && _resbalosa != null && _resbalosa.isActiveAndEnabled ? _resbalosa : null;
            float agarreDelSuelo = resbalosa != null ? resbalosa.agarre : 1f;

            if (EnSuelo)
            {
                CalcularAvance(ordenes.Avance, vAdelante, out float objetivo, out float tasa);
                if (objetivo == 0f && Mathf.Abs(ordenes.Avance) <= 0.01f) tasa *= agarreDelSuelo;
                vAdelante = Mathf.MoveTowards(vAdelante, objetivo, tasa * dt);

                // Sin derrapar, el deslizamiento de costado se frena rápido. Derrapando, el kart se desliza un
                // poco hacia afuera de la curva, sin perder velocidad.
                Vector3 lateralBuscado = Vector3.zero;
                if (Derrapando)
                {
                    Vector3 derecha = Vector3.Cross(_arriba, adelante);
                    lateralBuscado = derecha * (-DireccionDerrape * Mathf.Abs(vAdelante) * ajustes.deslizamientoDerrape);
                }
                float agarre = (Derrapando ? ajustes.agarreDerrape : ajustes.agarre) * agarreDelSuelo;
                vLateral = Vector3.Lerp(lateralBuscado, vLateral, Mathf.Exp(-agarre * dt));
            }

            // La velocidad hacia adelante gira con el kart: no se pierde al doblar.
            _adelante = adelante;
            VelocidadAdelante = vAdelante;
            _rb.linearVelocity = adelante * vAdelante + vLateral + vNormal;

            // Y lo resbaloso lo empuja hacia el borde.
            if (resbalosa != null) _rb.linearVelocity += resbalosa.Empuje(_rb.position, _arriba) * dt;
        }

        private void CalcularAvance(float avance, float vAdelante, out float objetivo, out float tasa)
        {
            float maxima = ajustes.velocidadMaxima * MultiplicadorVelocidad;
            if (EnTurbo && avance > -0.01f)
            {
                // El que lleva la comida recibe solo una parte del empujón.
                float parte = LlevaComida ? ajustes.turboConComida : 1f;
                objetivo = maxima * (1f + (ajustes.velocidadTurbo - 1f) * parte);
                tasa = ajustes.aceleracionTurbo;
            }
            else if (avance > 0.01f)
            {
                objetivo = maxima * avance;
                tasa = vAdelante < 0f ? ajustes.frenado : ajustes.aceleracion;
            }
            else if (avance < -0.01f)
            {
                // Primero frena; ya casi quieto, arranca la marcha atrás.
                bool frenando = vAdelante > 0.5f;
                objetivo = frenando ? 0f : ajustes.velocidadMarchaAtras * avance;
                tasa = frenando ? ajustes.frenado : ajustes.aceleracion;
            }
            else
            {
                objetivo = 0f;
                tasa = ajustes.rozamiento;
            }
        }
    }
}
