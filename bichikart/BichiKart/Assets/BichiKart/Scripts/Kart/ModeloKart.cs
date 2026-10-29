using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// El kart que se ve, con Blip manejando, hecho con formas simples. Mientras se juega, las ruedas giran
    /// con la velocidad, las de adelante doblan, la antena de Blip se bambolea al acelerar y frenar, el
    /// kart se cruza al derrapar (con chispas celestes que se ponen naranjas cuando el mini turbo está
    /// cargado) y los escapes tiran fuego con el turbo. Se puede desvanecer (<see cref="Alfa"/>), para cuando
    /// la cámara se le pega. Se arma desde código y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class ModeloKart : MonoBehaviour
    {
        private const float RadioRueda = 0.34f;
        private const float GiroAlDerrapar = 20f;

        private static readonly Color ChispaCargando = new Color(0.5f, 0.85f, 1f);
        private static readonly Color ChispaCargada = new Color(1f, 0.6f, 0.18f);

        [Tooltip("Color de la carrocería. Las ruedas son siempre negro azulado.")]
        [SerializeField] private Color colorCarroceria = new Color(1f, 0.624f, 0.11f);

        [Tooltip("Color del bichito. Por ahora todos tienen la forma de Blip; las formas de cada uno llegan con el arte.")]
        [SerializeField] private Color colorBichito = new Color(0.56f, 0.83f, 1f);

        private Formas _formas;
        private Kart _kart;
        private Transform _cuerpo;
        private readonly List<Transform> _ruedas = new List<Transform>();
        private readonly List<Transform> _chispas = new List<Transform>();
        private readonly List<Transform> _fuegos = new List<Transform>();
        private Material _materialChispas;
        private Transform _antena;
        private Color _colorArmado, _colorBichitoArmado;
        private float _vueltaRuedas;
        private float _velocidadAnterior;
        private float _inclinacionAntena;
        private float _giroCuerpo;
        private float _alfa = 1f;
        private readonly List<Material> _materiales = new List<Material>();

        /// <summary>
        /// Qué tan visible se ve el kart (con su bichito), de 0 a 1. Lo baja la cámara del jugador cuando se
        /// le acerca demasiado, para que el kart no le tape la vista.
        /// </summary>
        public float Alfa
        {
            get => _alfa;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, _alfa)) return;
                _alfa = value;
                foreach (var material in _materiales) Formas.PonerAlfa(material, _alfa);
            }
        }

        public Color ColorCarroceria
        {
            get => colorCarroceria;
            set
            {
                colorCarroceria = value;
                if (isActiveAndEnabled) Construir();
            }
        }

        public Color ColorBichito
        {
            get => colorBichito;
            set
            {
                colorBichito = value;
                if (isActiveAndEnabled) Construir();
            }
        }

        private void OnEnable()
        {
            _kart = GetComponentInParent<Kart>();
            Construir();
        }

        private void OnDisable() => Limpiar();

        private void Update()
        {
            if (colorCarroceria != _colorArmado || colorBichito != _colorBichitoArmado) Construir();
            if (Application.isPlaying) Animar(Time.deltaTime);
        }

        private void Animar(float dt)
        {
            if (dt <= 0f || _kart == null) return;
            float velocidad = _kart.VelocidadAdelante;
            float t = Time.time;

            _vueltaRuedas = Mathf.Repeat(_vueltaRuedas + velocidad / RadioRueda * Mathf.Rad2Deg * dt, 360f);
            float giroRuedas = _kart.UltimasOrdenes.Giro * 22f;
            foreach (var rueda in _ruedas)
            {
                bool delantera = rueda.localPosition.z > 0f;
                rueda.localRotation = Quaternion.Euler(0f, delantera ? giroRuedas : 0f, 0f) * Quaternion.Euler(_vueltaRuedas, 0f, 0f);
            }

            // Al derrapar, el kart se cruza hacia el lado del derrape.
            _giroCuerpo = Mathf.Lerp(_giroCuerpo, _kart.DireccionDerrape * GiroAlDerrapar, 1f - Mathf.Exp(-10f * dt));
            _cuerpo.localRotation = Quaternion.Euler(0f, _giroCuerpo, 0f);

            // La antena se va para atrás al acelerar y para adelante al frenar, y siempre tiembla un poco.
            float aceleracion = (velocidad - _velocidadAnterior) / dt;
            _velocidadAnterior = velocidad;
            _inclinacionAntena = Mathf.Lerp(_inclinacionAntena, Mathf.Clamp(-aceleracion * 1.2f, -35f, 35f), 1f - Mathf.Exp(-6f * dt));
            float temblor = 4f + Mathf.Abs(velocidad) * 0.4f;
            _antena.localRotation = Quaternion.Euler(_inclinacionAntena + Mathf.Sin(t * 9f) * temblor, 0f, Mathf.Cos(t * 7f) * temblor);

            // Chispas del derrape y fuego del turbo, que titilan.
            var colorChispa = _kart.CargaDerrape >= 1f ? ChispaCargada : ChispaCargando;
            _materialChispas.SetColor("_BaseColor", new Color(colorChispa.r, colorChispa.g, colorChispa.b, _alfa));
            _materialChispas.SetColor("_EmissionColor", colorChispa * 1.8f);
            for (int i = 0; i < _chispas.Count; i++)
            {
                _chispas[i].gameObject.SetActive(_kart.Derrapando);
                _chispas[i].localScale = Vector3.one * (0.14f + 0.08f * Mathf.PerlinNoise(t * 20f, i * 3.1f));
            }
            for (int i = 0; i < _fuegos.Count; i++)
            {
                _fuegos[i].gameObject.SetActive(_kart.EnTurbo);
                _fuegos[i].localScale = new Vector3(0.22f, 0.22f, 0.35f + 0.25f * Mathf.PerlinNoise(t * 25f, i * 5.3f));
            }
        }

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            Limpiar();
            _formas = new Formas();
            _colorArmado = colorCarroceria;
            _colorBichitoArmado = colorBichito;
            _cuerpo = _formas.Nodo("Cuerpo", transform);
            ArmarKart();
            ArmarEfectos();
            ArmarBlip();

            // Los materiales del kart, para poder desvanecerlo (si estaba desvanecido, sigue igual).
            _materiales.Clear();
            foreach (var renderer in _cuerpo.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
                if (material != null && !_materiales.Contains(material)) _materiales.Add(material);
            if (_alfa < 1f) foreach (var material in _materiales) Formas.PonerAlfa(material, _alfa);
        }

        private void ArmarKart()
        {
            var carroceria = _formas.Mat(colorCarroceria, 0.55f);
            var respaldo = _formas.Mat(Color.Lerp(colorCarroceria, Formas.Hex("#1d2340"), 0.25f), 0.4f);
            var goma = _formas.Mat(Formas.Hex("#1d2340"), 0.35f);
            var gris = _formas.Mat(Formas.Hex("#d5d9e2"), 0.5f);
            var metal = _formas.Mat(Formas.Hex("#8b93a3"), 0.7f);

            _formas.Caja("Chasis", _cuerpo, new Vector3(0f, 0.6f, 0f), new Vector3(1.5f, 0.5f, 2.2f), 0.22f, carroceria);
            _formas.Caja("Trompa", _cuerpo, new Vector3(0f, 0.5f, 1.2f), new Vector3(1.1f, 0.35f, 0.5f), 0.15f, carroceria);
            _formas.Caja("Paragolpes", _cuerpo, new Vector3(0f, 0.42f, 1.5f), new Vector3(1.4f, 0.25f, 0.2f), 0.1f, gris);
            _formas.Caja("Respaldo", _cuerpo, new Vector3(0f, 1.05f, -0.75f), new Vector3(1f, 0.6f, 0.2f), 0.1f, respaldo);
            _formas.Caja("Alerón", _cuerpo, new Vector3(0f, 1.35f, -1.05f), new Vector3(1.7f, 0.1f, 0.4f), 0.05f, carroceria);
            _formas.Cilindro("Escape izquierdo", _cuerpo, new Vector3(-0.35f, 0.55f, -1.15f), new Vector3(0.18f, 0.12f, 0.18f), Quaternion.Euler(90f, 0f, 0f), metal);
            _formas.Cilindro("Escape derecho", _cuerpo, new Vector3(0.35f, 0.55f, -1.15f), new Vector3(0.18f, 0.12f, 0.18f), Quaternion.Euler(90f, 0f, 0f), metal);

            _ruedas.Clear();
            foreach (var x in new[] { -0.85f, 0.85f })
            foreach (var z in new[] { -0.7f, 0.75f })
            {
                var rueda = _formas.Nodo("Rueda", _cuerpo);
                rueda.localPosition = new Vector3(x, RadioRueda, z);
                _formas.Cilindro("Goma", rueda, Vector3.zero, new Vector3(0.68f, 0.15f, 0.68f), Quaternion.Euler(0f, 0f, 90f), goma);
                // Un rayo de la llanta, para que se note que la rueda gira.
                _formas.Caja("Rayo", rueda, new Vector3(Mathf.Sign(x) * 0.16f, 0f, 0f), new Vector3(0.04f, 0.46f, 0.1f), 0.015f, metal);
                _ruedas.Add(rueda);
            }
        }

        private void ArmarEfectos()
        {
            _chispas.Clear();
            _fuegos.Clear();
            _materialChispas = _formas.Emisivo(ChispaCargando, 4f);
            foreach (var x in new[] { -0.85f, 0.85f })
            {
                var chispa = _formas.Esfera("Chispas", _cuerpo, new Vector3(x, 0.08f, -1.05f), Vector3.one * 0.18f, _materialChispas);
                Formas.SinSombra(chispa);
                chispa.gameObject.SetActive(false);
                _chispas.Add(chispa);
            }

            var fuego = _formas.Emisivo(Formas.Hex("#ff8a2a"), 5f);
            foreach (var x in new[] { -0.35f, 0.35f })
            {
                var llama = _formas.Esfera("Fuego del turbo", _cuerpo, new Vector3(x, 0.55f, -1.4f), Vector3.one * 0.22f, fuego);
                Formas.SinSombra(llama);
                llama.gameObject.SetActive(false);
                _fuegos.Add(llama);
            }
        }

        private void ArmarBlip()
        {
            // Blip: bolita celeste de un ojo, con orejitas y antena de resorte con bolita naranja.
            var blip = _formas.Nodo("Blip", _cuerpo);
            blip.localPosition = new Vector3(0f, 1.3f, -0.2f);
            var piel = _formas.Mat(colorBichito, 0.5f);
            _formas.Esfera("Cuerpo", blip, Vector3.zero, Vector3.one * 0.9f, piel);
            _formas.Esfera("Oreja izquierda", blip, new Vector3(-0.45f, 0.1f, 0f), Vector3.one * 0.25f, piel);
            _formas.Esfera("Oreja derecha", blip, new Vector3(0.45f, 0.1f, 0f), Vector3.one * 0.25f, piel);
            _formas.Esfera("Ojo", blip, new Vector3(0f, 0.08f, 0.33f), Vector3.one * 0.42f, _formas.Mat(Color.white, 0.6f));
            _formas.Esfera("Pupila", blip, new Vector3(0f, 0.08f, 0.52f), Vector3.one * 0.2f, _formas.Mat(Formas.Hex("#1d2340"), 0.8f));
            _antena = _formas.Nodo("Antena", blip);
            _antena.localPosition = new Vector3(0f, 0.42f, 0f);
            _formas.Cilindro("Resorte", _antena, new Vector3(0f, 0.25f, 0f), new Vector3(0.06f, 0.25f, 0.06f), Quaternion.identity, _formas.Mat(Formas.Hex("#3b4a8f"), 0.4f));
            _formas.Esfera("Bolita", _antena, new Vector3(0f, 0.55f, 0f), Vector3.one * 0.22f, _formas.Mat(Formas.Hex("#ff8a1c"), 0.5f));
        }

        private void Limpiar()
        {
            _formas?.Limpiar();
            _formas = null;
            _cuerpo = null;
            _ruedas.Clear();
            _chispas.Clear();
            _fuegos.Clear();
            _materiales.Clear();
            _materialChispas = null;
            _antena = null;
        }
    }
}
