using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Física de un kart, estilo arcade. Lo que choca es una esfera invisible con Rigidbody que nunca gira;
    /// hacia dónde mira el kart lo lleva este script, que además gira el <see cref="Chasis"/> (el kart que
    /// se ve, al que sigue la cámara). La gravedad la pone el juego, no Unity, para que más adelante pueda
    /// apuntar a las paredes y al techo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class Kart : MonoBehaviour
    {
        [SerializeField] private AjustesDelKart ajustes;

        [Tooltip("El kart que se ve. Lo gira este script y la cámara lo sigue.")]
        [SerializeField] private Transform chasis;

        [Tooltip("Capas sobre las que se puede manejar.")]
        [SerializeField] private LayerMask capaSuelo = ~0;

        private Rigidbody _rb;
        private Vector3 _adelante = Vector3.forward;
        private Vector3 _arriba = Vector3.up;
        private Vector3 _gravedad = Vector3.down;
        private Quaternion _rotacionChasis = Quaternion.identity;

        /// <summary>De dónde salen las órdenes. Si no se asigna, se busca en el mismo objeto.</summary>
        public IFuenteDeOrdenes Fuente { get; set; }

        public AjustesDelKart Ajustes => ajustes;
        public Transform Chasis => chasis;

        /// <summary>Hacia dónde mira el kart, sobre la superficie en la que está.</summary>
        public Vector3 Adelante => _adelante;

        /// <summary>El "arriba" del kart: la normal de la superficie en la que está apoyado.</summary>
        public Vector3 Arriba => _arriba;

        public bool EnSuelo { get; private set; }

        /// <summary>Velocidad hacia donde mira el kart (negativa en marcha atrás).</summary>
        public float VelocidadAdelante { get; private set; }

        public OrdenesDelKart UltimasOrdenes { get; private set; }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(AjustesDelKart nuevosAjustes, Transform nuevoChasis, LayerMask nuevaCapaSuelo)
        {
            ajustes = nuevosAjustes;
            chasis = nuevoChasis;
            capaSuelo = nuevaCapaSuelo;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var esfera = GetComponent<SphereCollider>();
            esfera.radius = ajustes.radioEsfera;
            esfera.center = Vector3.zero;
            esfera.sharedMaterial = new PhysicsMaterial("Kart sin roce")
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

            var centro = sobreElPiso + _arriba * ajustes.radioEsfera;
            transform.position = centro;
            if (_rb != null)
            {
                _rb.position = centro;
                _rb.linearVelocity = Vector3.zero;
            }
            if (chasis != null) chasis.SetPositionAndRotation(sobreElPiso, _rotacionChasis);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var ordenes = Fuente != null ? Fuente.LeerOrdenes() : default;
            UltimasOrdenes = ordenes;

            DetectarSuelo(dt);
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

        private void DetectarSuelo(float dt)
        {
            float radio = ajustes.radioEsfera;
            bool toca = Physics.SphereCast(_rb.position - _gravedad * 0.05f, radio * 0.9f, _gravedad, out var golpe,
                radio * 0.1f + 0.05f + ajustes.distanciaSuelo, capaSuelo, QueryTriggerInteraction.Ignore);
            EnSuelo = toca;

            // Por ahora la gravedad siempre tira hacia abajo; el kart solo se inclina con el piso.
            var arribaBuscado = toca ? golpe.normal : -_gravedad;
            _arriba = Vector3.Slerp(_arriba, arribaBuscado, 1f - Mathf.Exp(-ajustes.alineacion * dt)).normalized;
        }

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
            float control = EnSuelo ? 1f : ajustes.controlEnElAire;
            float factor = Mathf.Clamp01(Mathf.Abs(vAdelante) / ajustes.velocidadParaGiroCompleto);
            float sentido = vAdelante < -0.1f ? -1f : 1f;
            adelante = Quaternion.AngleAxis(ordenes.Giro * ajustes.giro * factor * sentido * control * dt, _arriba) * adelante;

            if (EnSuelo)
            {
                CalcularAvance(ordenes.Avance, vAdelante, out float objetivo, out float tasa);
                vAdelante = Mathf.MoveTowards(vAdelante, objetivo, tasa * dt);
                vLateral *= Mathf.Exp(-ajustes.agarre * dt);
            }

            // La velocidad hacia adelante gira con el kart: no se pierde al doblar.
            _adelante = adelante;
            VelocidadAdelante = vAdelante;
            _rb.linearVelocity = adelante * vAdelante + vLateral + vNormal;
        }

        private void CalcularAvance(float avance, float vAdelante, out float objetivo, out float tasa)
        {
            if (avance > 0.01f)
            {
                objetivo = ajustes.velocidadMaxima * avance;
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
