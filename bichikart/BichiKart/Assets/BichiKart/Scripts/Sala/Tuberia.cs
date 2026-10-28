using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace BichiKart
{
    /// <summary>
    /// Un caño real de la casa, metálico y gris, que une un desagüe del piso con una rejilla del techo en la
    /// esquina de enfrente. Va por afuera de la sala: baja por debajo del piso, sube por detrás de la
    /// heladera y cruza por arriba del techo. Las dos bocas tienen la misma lucecita de color. Arriba de cada
    /// boca aparece un cartel-globito con la F cuando el jugador se acerca. Sobre la boca, con el botón de
    /// usar (F) el kart se mete y viaja solo por adentro, como en un tobogán, hasta salir por la otra; con
    /// doblar puede girar alrededor del caño. Adentro también se puede robar: si otro kart te alcanza, te toca.
    /// Se arma desde código con las medidas de la Cocina y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class Tuberia : MonoBehaviour
    {
        [SerializeField] private Cocina cocina;

        [Tooltip("Color de las lucecitas de las dos bocas (las dos puntas del mismo caño tienen el mismo).")]
        [SerializeField] private Color colorLuz = new Color(0.24f, 0.56f, 0.97f);

        [Tooltip("Velocidad a la que se viaja por adentro.")]
        [SerializeField] private float velocidad = 24f;

        [Tooltip("Velocidad con la que se sale por la otra boca.")]
        [SerializeField] private float velocidadDeSalida = 14f;

        [Tooltip("Radio del caño por dentro.")]
        [SerializeField] private float radioCanio = 1.8f;

        [Tooltip("Qué tan cerca de una boca hay que estar para meterse con el botón de usar.")]
        [SerializeField] private float radioBoca = 2.6f;

        [Tooltip("Desde qué distancia se ve el cartel arriba de la boca (más chico hasta que se puede usar).")]
        [SerializeField] private float distanciaCartel = 8f;

        [Tooltip("Grados por segundo que se gira alrededor del caño doblando a fondo.")]
        [SerializeField] private float giroAdentro = 200f;

        [Tooltip("Segundos sin poder volver a meterse después de salir (para no entrar de nuevo por la boca de salida).")]
        [SerializeField] private float esperaParaVolver = 1.2f;

        private Formas _formas;
        private Spline _camino;
        private float _largo;
        private Vector3 _medidasArmadas;
        private Vector3 _bocaPiso, _normalPiso, _bocaTecho, _normalTecho;
        private Kart[] _karts = new Kart[0];
        private readonly List<Viaje> _viajes = new List<Viaje>();
        private readonly Dictionary<Kart, float> _puedeVolverDesde = new Dictionary<Kart, float>();
        private readonly Transform[] _carteles = new Transform[2];
        private readonly float[] _tamanoCartel = new float[2];
        private Kart _jugador;
        private Camera _camara;

        private const float AlturaCartel = 2.8f;

        private sealed class Viaje
        {
            public Kart Kart;
            public float T;
            public int Sentido;
            public Vector3 Arriba;
            public float Giro;
        }

        /// <summary>Las dos bocas (0: la del piso, 1: la del techo): dónde están y hacia dónde miran.</summary>
        public (Vector3 lugar, Vector3 normal) Boca(int numero)
        {
            return numero == 0
                ? (transform.TransformPoint(_bocaPiso), transform.TransformDirection(_normalPiso))
                : (transform.TransformPoint(_bocaTecho), transform.TransformDirection(_normalTecho));
        }

        /// <summary>Qué tan cerca de una boca hay que estar para meterse.</summary>
        public float RadioBoca => radioBoca;

        /// <summary>Por qué boca se puede meter este kart ahora mismo apretando usar (-1 si por ninguna).</summary>
        public int BocaAlAlcance(Kart kart) => BocaCerca(kart, radioBoca);

        private int BocaCerca(Kart kart, float distancia)
        {
            if (kart == null || kart.EnCanio || !kart.EnSuelo) return -1;
            if (_puedeVolverDesde.TryGetValue(kart, out float desde) && Time.time < desde) return -1;
            if (EstaSobre(kart, _bocaPiso, _normalPiso, distancia)) return 0;
            if (EstaSobre(kart, _bocaTecho, _normalTecho, distancia)) return 1;
            return -1;
        }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(Cocina nuevaCocina)
        {
            cocina = nuevaCocina;
            if (isActiveAndEnabled) Construir();
        }

        private void OnEnable() => Construir();

        private void OnDisable()
        {
            _formas?.Limpiar();
            _formas = null;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            _karts = FindObjectsByType<Kart>(FindObjectsSortMode.None);
            var teclado = FindFirstObjectByType<FuenteTeclado>();
            _jugador = teclado != null ? teclado.GetComponent<Kart>() : null;
        }

        private void Update()
        {
            // Si cambian las medidas de la sala, el caño se vuelve a armar.
            if (cocina != null && cocina.Medidas != _medidasArmadas) Construir();
            if (Application.isPlaying) ActualizarCarteles();
        }

        /// <summary>
        /// El cartel de cada boca aparece chiquito cuando el jugador anda cerca, en la misma superficie, y
        /// crece y salta cuando ya se puede meter. Siempre mira a la cámara.
        /// </summary>
        private void ActualizarCarteles()
        {
            if (_camara == null) _camara = Camera.main;
            int alAlcance = BocaAlAlcance(_jugador);
            int cerca = BocaCerca(_jugador, distanciaCartel);
            for (int i = 0; i < 2; i++)
            {
                var cartel = _carteles[i];
                if (cartel == null) continue;
                float buscado = alAlcance == i ? 1f : cerca == i ? 0.6f : 0f;
                _tamanoCartel[i] = Mathf.Lerp(_tamanoCartel[i], buscado, 1f - Mathf.Exp(-12f * Time.deltaTime));
                bool visible = _tamanoCartel[i] > 0.02f;
                cartel.gameObject.SetActive(visible);
                if (!visible) continue;

                float salto = alAlcance == i ? Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.35f : Mathf.Sin(Time.time * 3f) * 0.12f;
                float latido = alAlcance == i ? 1f + 0.06f * Mathf.Sin(Time.time * 12f) : 1f;
                cartel.localPosition = Vector3.up * (AlturaCartel + salto);
                cartel.localScale = Vector3.one * (_tamanoCartel[i] * latido);
                if (_camara != null) cartel.rotation = Quaternion.LookRotation(_camara.transform.forward, _camara.transform.up);
            }
        }

        // ------------------------------------------------------------------ viajes

        private void FixedUpdate()
        {
            if (!Application.isPlaying || _camino == null) return;
            float dt = Time.fixedDeltaTime;
            BuscarQuienEntra();

            for (int i = _viajes.Count - 1; i >= 0; i--)
            {
                var viaje = _viajes[i];
                // Si la partida lo volvió a poner en su salida (ronda nueva), el viaje se corta.
                if (!viaje.Kart.EnCanio)
                {
                    _viajes.RemoveAt(i);
                    continue;
                }
                viaje.T += viaje.Sentido * velocidad * dt / _largo;
                if (viaje.T >= 1f || viaje.T <= 0f)
                {
                    Salir(viaje);
                    _viajes.RemoveAt(i);
                    continue;
                }
                Avanzar(viaje, dt);
            }
        }

        /// <summary>Se mete el que está sobre una boca y aprieta usar.</summary>
        private void BuscarQuienEntra()
        {
            foreach (var kart in _karts)
            {
                if (kart == null || !kart.UltimasOrdenes.Usar) continue;
                int boca = BocaAlAlcance(kart);
                if (boca == 0) Entrar(kart, 1);
                else if (boca == 1) Entrar(kart, -1);
            }
        }

        private bool EstaSobre(Kart kart, Vector3 boca, Vector3 normal, float distancia)
        {
            return Vector3.Dot(kart.Arriba, transform.TransformDirection(normal)) > 0.8f
                && Vector3.Distance(kart.Chasis.position, transform.TransformPoint(boca)) < distancia;
        }

        /// <param name="sentido">1 si entra por el piso, -1 si entra por el techo.</param>
        private void Entrar(Kart kart, int sentido)
        {
            var viaje = new Viaje { Kart = kart, T = sentido > 0 ? 0f : 1f, Sentido = sentido };
            // Se tira de cabeza: lo que era "adelante" pasa a ser su "arriba" adentro del caño.
            var tangente = Tangente(viaje.T) * sentido;
            viaje.Arriba = Vector3.ProjectOnPlane(kart.Adelante, tangente);
            if (viaje.Arriba.sqrMagnitude < 1e-4f) viaje.Arriba = Perpendicular(tangente);
            viaje.Arriba.Normalize();

            kart.EntrarAlCanio();
            _viajes.Add(viaje);
            kart.GetComponent<SonidosDelKart>()?.EntrarAlCanio();
            Avanzar(viaje, 0f);
        }

        private void Avanzar(Viaje viaje, float dt)
        {
            var centro = transform.TransformPoint((Vector3)_camino.EvaluatePosition(viaje.T));
            var tangente = Tangente(viaje.T) * viaje.Sentido;
            // "Arriba" sin retorcerse: se acomoda de a poco a la curva del caño.
            viaje.Arriba = Vector3.ProjectOnPlane(viaje.Arriba, tangente).normalized;
            viaje.Giro += viaje.Kart.UltimasOrdenes.Giro * giroAdentro * dt;
            var arriba = Quaternion.AngleAxis(viaje.Giro, tangente) * viaje.Arriba;
            viaje.Kart.MoverEnCanio(centro, tangente, arriba, velocidad);
        }

        private void Salir(Viaje viaje)
        {
            bool porElTecho = viaje.Sentido > 0;
            var boca = transform.TransformPoint(porElTecho ? _bocaTecho : _bocaPiso);
            var normal = transform.TransformDirection(porElTecho ? _normalTecho : _normalPiso);

            // Sale apoyado en la superficie de la boca, avanzando hacia donde tenía su "arriba" en el caño
            // (o hacia el centro de la sala, si no se puede).
            var adelante = Vector3.ProjectOnPlane(Quaternion.AngleAxis(viaje.Giro, normal) * viaje.Arriba, normal);
            if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.ProjectOnPlane(cocina.transform.position - boca, normal);
            if (adelante.sqrMagnitude < 1e-4f) adelante = Perpendicular(normal);

            viaje.Kart.SalirDelCanio(boca, normal, adelante.normalized, velocidadDeSalida);
            _puedeVolverDesde[viaje.Kart] = Time.time + esperaParaVolver;
            viaje.Kart.GetComponent<SonidosDelKart>()?.SalirDelCanio();
        }

        private Vector3 Tangente(float t) => transform.TransformDirection(((Vector3)_camino.EvaluateTangent(Mathf.Clamp01(t))).normalized);

        private static Vector3 Perpendicular(Vector3 v) => Vector3.Cross(v, Mathf.Abs(Vector3.Dot(v, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right).normalized;

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            _formas?.Limpiar();
            _formas = null;
            _camino = null;
            if (cocina == null || cocina.Ajustes == null) return;
            _formas = new Formas();
            var m = cocina.Medidas;
            _medidasArmadas = m;

            // Bocas en esquinas opuestas: un desagüe en el piso y una rejilla en el techo.
            _bocaPiso = new Vector3(m.x * 0.3f, 0f, -m.z * 0.31f);
            _normalPiso = Vector3.up;
            _bocaTecho = new Vector3(-m.x * 0.3f, m.y, m.z * 0.31f);
            _normalTecho = Vector3.down;

            // Por afuera: baja, va por debajo del piso hasta pasar la heladera, sube por detrás, cruza por
            // arriba del techo y baja a la rejilla.
            const float afuera = 3f;
            var puntos = new List<float3>
            {
                _bocaPiso,
                _bocaPiso + Vector3.down * 2.5f,
                new Vector3(m.x * 0.5f + afuera * 0.4f, -2.5f, _bocaPiso.z),
                new Vector3(m.x * 0.5f + afuera, 1f, _bocaPiso.z),
                new Vector3(m.x * 0.5f + afuera, m.y - 1f, _bocaPiso.z),
                new Vector3(m.x * 0.5f + afuera * 0.4f, m.y + 2.5f, _bocaPiso.z),
                _bocaTecho + Vector3.up * 2.5f,
                _bocaTecho,
            };
            _camino = new Spline();
            foreach (var punto in puntos) _camino.Add(punto, TangentMode.AutoSmooth);
            _largo = _camino.GetLength();

            ArmarCanio();
            _carteles[0] = ArmarBoca("Desagüe", _bocaPiso, Quaternion.identity);
            _carteles[1] = ArmarBoca("Rejilla del techo", _bocaTecho, Quaternion.Euler(180f, 0f, 0f));
        }

        private void ArmarCanio()
        {
            var metal = _formas.Mat(Formas.Hex("#9aa2b1"), 0.75f);
            // Por dentro brilla un poco, con anillos, para que se vea y se note la velocidad.
            var adentro = _formas.Emisivo(Color.Lerp(Formas.Hex("#7e8aa3"), colorLuz, 0.35f), 0.6f, Texturas.Anillos());
            _formas.Malla("Caño por dentro", transform, CrearTubo(radioCanio, true), adentro);
            _formas.Malla("Caño por fuera", transform, CrearTubo(radioCanio + 0.25f, false), metal);
        }

        /// <summary>Un tubo a lo largo del camino. Por dentro, las caras miran hacia el centro del caño.</summary>
        private Mesh CrearTubo(float radio, bool porDentro)
        {
            int anillos = Mathf.Max(8, Mathf.CeilToInt(_largo));
            const int lados = 16;
            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangulos = new List<int>();

            Vector3 derecha = Vector3.zero;
            for (int i = 0; i <= anillos; i++)
            {
                float t = i / (float)anillos;
                Vector3 centro = (Vector3)_camino.EvaluatePosition(t);
                Vector3 tangente = ((Vector3)_camino.EvaluateTangent(t)).normalized;
                // La orientación de cada anillo sigue a la del anterior, para que el tubo no se retuerza.
                derecha = i == 0 ? Perpendicular(tangente) : Vector3.ProjectOnPlane(derecha, tangente).normalized;
                Vector3 arriba = Vector3.Cross(tangente, derecha);
                for (int j = 0; j <= lados; j++)
                {
                    float angulo = j * Mathf.PI * 2f / lados;
                    Vector3 direccion = Mathf.Cos(angulo) * derecha + Mathf.Sin(angulo) * arriba;
                    vertices.Add(centro + direccion * radio);
                    normales.Add(porDentro ? -direccion : direccion);
                    uvs.Add(new Vector2(j / (float)lados, t * _largo / 4f));
                }
            }

            int fila = lados + 1;
            for (int i = 0; i < anillos; i++)
            for (int j = 0; j < lados; j++)
            {
                int a = i * fila + j, b = a + fila, c = b + 1, d = a + 1;
                if (porDentro)
                {
                    triangulos.Add(a); triangulos.Add(b); triangulos.Add(d);
                    triangulos.Add(b); triangulos.Add(c); triangulos.Add(d);
                }
                else
                {
                    triangulos.Add(a); triangulos.Add(d); triangulos.Add(b);
                    triangulos.Add(d); triangulos.Add(c); triangulos.Add(b);
                }
            }

            var malla = new Mesh { name = porDentro ? "Caño por dentro" : "Caño por fuera" };
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetUVs(0, uvs);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            return malla;
        }

        /// <summary>
        /// Rejilla redonda de metal con su agujero, sus barritas y la lucecita de color al costado. Devuelve
        /// el cartel que flota arriba.
        /// </summary>
        private Transform ArmarBoca(string nombre, Vector3 lugar, Quaternion giro)
        {
            var boca = _formas.Nodo(nombre, transform);
            boca.localPosition = lugar;
            boca.localRotation = giro;
            var metal = _formas.Mat(Formas.Hex("#8b93a3"), 0.7f);
            _formas.Cilindro("Aro", boca, new Vector3(0f, 0.02f, 0f), new Vector3(3.4f, 0.04f, 3.4f), Quaternion.identity, metal);
            _formas.Cilindro("Agujero", boca, new Vector3(0f, 0.045f, 0f), new Vector3(2.8f, 0.01f, 2.8f), Quaternion.identity, _formas.Mat(Formas.Hex("#1d2340"), 0.2f));
            for (int i = -3; i <= 3; i++)
                _formas.Caja("Barrita", boca, new Vector3(i * 0.36f, 0.07f, 0f), new Vector3(0.12f, 0.06f, 2.7f - Mathf.Abs(i) * 0.3f), 0.03f, metal);
            var foco = _formas.Esfera("Lucecita", boca, new Vector3(2.15f, 0.15f, 0f), Vector3.one * 0.35f, _formas.Emisivo(colorLuz, 4f));
            Formas.SinSombra(foco);
            _formas.Luz("Luz de la boca", boca, new Vector3(2.15f, 0.6f, 0f), colorLuz, 6f, 5f, false);
            return ArmarCartel(boca);
        }

        /// <summary>
        /// Globito inflado con el borde del color de la lucecita y una tecla F en el medio, con la colita
        /// apuntando a la boca. La cara de adelante es la -Z (la que mira a la cámara).
        /// </summary>
        private Transform ArmarCartel(Transform boca)
        {
            var cartel = _formas.Nodo("Cartel", boca);
            cartel.localPosition = Vector3.up * AlturaCartel;
            var borde = _formas.Emisivo(colorLuz, 1.2f);
            var tecla = _formas.Mat(Formas.Hex("#1d2340"), 0.6f);
            var letra = _formas.Emisivo(Color.white, 1.6f);

            Formas.SinSombra(_formas.Caja("Globo", cartel, Vector3.zero, new Vector3(1.6f, 1.6f, 0.5f), 0.25f, borde));
            var colita = _formas.Caja("Colita", cartel, new Vector3(0f, -0.85f, 0f), new Vector3(0.5f, 0.5f, 0.4f), 0.1f, borde);
            colita.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Formas.SinSombra(colita);
            Formas.SinSombra(_formas.Caja("Tecla", cartel, Vector3.zero, new Vector3(1.3f, 1.3f, 0.56f), 0.2f, tecla));

            // La F, con tres barritas.
            Formas.SinSombra(_formas.Caja("F palo", cartel, new Vector3(-0.18f, 0f, -0.3f), new Vector3(0.14f, 0.76f, 0.06f), 0.025f, letra));
            Formas.SinSombra(_formas.Caja("F arriba", cartel, new Vector3(0f, 0.31f, -0.3f), new Vector3(0.5f, 0.14f, 0.06f), 0.025f, letra));
            Formas.SinSombra(_formas.Caja("F medio", cartel, new Vector3(-0.06f, 0.02f, -0.3f), new Vector3(0.38f, 0.14f, 0.06f), 0.025f, letra));

            // En el editor se ve siempre (para acomodarlo); jugando, aparece cuando el jugador se acerca.
            cartel.gameObject.SetActive(!Application.isPlaying);
            return cartel;
        }
    }
}
