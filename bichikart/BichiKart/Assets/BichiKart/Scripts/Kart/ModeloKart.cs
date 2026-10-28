using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// El kart que se ve, con Blip manejando, hecho con formas simples. Mientras se juega, las ruedas giran
    /// con la velocidad, las de adelante doblan y la antena de Blip se bambolea al acelerar y frenar.
    /// Se arma desde código y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class ModeloKart : MonoBehaviour
    {
        private const float RadioRueda = 0.34f;

        [Tooltip("Color de la carrocería. Las ruedas son siempre negro azulado.")]
        [SerializeField] private Color colorCarroceria = new Color(1f, 0.624f, 0.11f);

        private Formas _formas;
        private Kart _kart;
        private readonly List<Transform> _ruedas = new List<Transform>();
        private Transform _antena;
        private Color _colorArmado;
        private float _vueltaRuedas;
        private float _velocidadAnterior;
        private float _inclinacionAntena;

        public Color ColorCarroceria
        {
            get => colorCarroceria;
            set
            {
                colorCarroceria = value;
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
            if (colorCarroceria != _colorArmado) Construir();
            if (Application.isPlaying) Animar(Time.deltaTime);
        }

        private void Animar(float dt)
        {
            if (dt <= 0f) return;
            float velocidad = _kart != null ? _kart.VelocidadAdelante : 0f;
            float giro = _kart != null ? _kart.UltimasOrdenes.Giro * 22f : 0f;

            _vueltaRuedas = Mathf.Repeat(_vueltaRuedas + velocidad / RadioRueda * Mathf.Rad2Deg * dt, 360f);
            foreach (var rueda in _ruedas)
            {
                bool delantera = rueda.localPosition.z > 0f;
                rueda.localRotation = Quaternion.Euler(0f, delantera ? giro : 0f, 0f) * Quaternion.Euler(_vueltaRuedas, 0f, 0f);
            }

            // La antena se va para atrás al acelerar y para adelante al frenar, y siempre tiembla un poco.
            float aceleracion = (velocidad - _velocidadAnterior) / dt;
            _velocidadAnterior = velocidad;
            _inclinacionAntena = Mathf.Lerp(_inclinacionAntena, Mathf.Clamp(-aceleracion * 1.2f, -35f, 35f), 1f - Mathf.Exp(-6f * dt));
            if (_antena != null)
            {
                float t = Time.time;
                float temblor = 4f + Mathf.Abs(velocidad) * 0.4f;
                _antena.localRotation = Quaternion.Euler(_inclinacionAntena + Mathf.Sin(t * 9f) * temblor, 0f, Mathf.Cos(t * 7f) * temblor);
            }
        }

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            Limpiar();
            _formas = new Formas();
            _colorArmado = colorCarroceria;
            ArmarKart();
            ArmarBlip();
        }

        private void ArmarKart()
        {
            var carroceria = _formas.Mat(colorCarroceria, 0.55f);
            var respaldo = _formas.Mat(Color.Lerp(colorCarroceria, Formas.Hex("#1d2340"), 0.25f), 0.4f);
            var goma = _formas.Mat(Formas.Hex("#1d2340"), 0.35f);
            var gris = _formas.Mat(Formas.Hex("#d5d9e2"), 0.5f);
            var metal = _formas.Mat(Formas.Hex("#8b93a3"), 0.7f);

            _formas.Caja("Chasis", transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.5f, 0.5f, 2.2f), 0.22f, carroceria);
            _formas.Caja("Trompa", transform, new Vector3(0f, 0.5f, 1.2f), new Vector3(1.1f, 0.35f, 0.5f), 0.15f, carroceria);
            _formas.Caja("Paragolpes", transform, new Vector3(0f, 0.42f, 1.5f), new Vector3(1.4f, 0.25f, 0.2f), 0.1f, gris);
            _formas.Caja("Respaldo", transform, new Vector3(0f, 1.05f, -0.75f), new Vector3(1f, 0.6f, 0.2f), 0.1f, respaldo);
            _formas.Caja("Alerón", transform, new Vector3(0f, 1.35f, -1.05f), new Vector3(1.7f, 0.1f, 0.4f), 0.05f, carroceria);
            _formas.Cilindro("Escape izquierdo", transform, new Vector3(-0.35f, 0.55f, -1.15f), new Vector3(0.18f, 0.12f, 0.18f), Quaternion.Euler(90f, 0f, 0f), metal);
            _formas.Cilindro("Escape derecho", transform, new Vector3(0.35f, 0.55f, -1.15f), new Vector3(0.18f, 0.12f, 0.18f), Quaternion.Euler(90f, 0f, 0f), metal);

            _ruedas.Clear();
            foreach (var x in new[] { -0.85f, 0.85f })
            foreach (var z in new[] { -0.7f, 0.75f })
            {
                var rueda = _formas.Nodo("Rueda", transform);
                rueda.localPosition = new Vector3(x, RadioRueda, z);
                _formas.Cilindro("Goma", rueda, Vector3.zero, new Vector3(0.68f, 0.15f, 0.68f), Quaternion.Euler(0f, 0f, 90f), goma);
                // Un rayo de la llanta, para que se note que la rueda gira.
                _formas.Caja("Rayo", rueda, new Vector3(Mathf.Sign(x) * 0.16f, 0f, 0f), new Vector3(0.04f, 0.46f, 0.1f), 0.015f, metal);
                _ruedas.Add(rueda);
            }
        }

        private void ArmarBlip()
        {
            // Blip: bolita celeste de un ojo, con orejitas y antena de resorte con bolita naranja.
            var blip = _formas.Nodo("Blip", transform);
            blip.localPosition = new Vector3(0f, 1.3f, -0.2f);
            var celeste = _formas.Mat(Formas.Hex("#8fd3ff"), 0.5f);
            _formas.Esfera("Cuerpo", blip, Vector3.zero, Vector3.one * 0.9f, celeste);
            _formas.Esfera("Oreja izquierda", blip, new Vector3(-0.45f, 0.1f, 0f), Vector3.one * 0.25f, celeste);
            _formas.Esfera("Oreja derecha", blip, new Vector3(0.45f, 0.1f, 0f), Vector3.one * 0.25f, celeste);
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
            _ruedas.Clear();
            _antena = null;
        }
    }
}
