using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// La Cocina como sala cerrada: un rincón gigante donde el piso es la mesada de granito, el techo es la
    /// parte de abajo de la alacena, un costado es la heladera y las otras paredes son de azulejos, con la
    /// ventana al espacio. Los rincones son redondeados por dentro para pasar manejando del piso a las
    /// paredes. Se arma desde código con <see cref="AjustesDeSala"/> y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class Cocina : MonoBehaviour
    {
        [SerializeField] private AjustesDeSala ajustes;

        private Formas _formas;
        private int _huella;
        private readonly HashSet<Collider> _obstaculos = new HashSet<Collider>();
        private readonly List<Escondite> _escondites = new List<Escondite>();

        /// <summary>Un obstáculo detrás del cual esconderse: su centro sobre el piso y su radio.</summary>
        public readonly struct Escondite
        {
            public readonly Vector3 Centro;
            public readonly float Radio;

            public Escondite(Vector3 centro, float radio)
            {
                Centro = centro;
                Radio = radio;
            }
        }

        public IReadOnlyList<Escondite> Escondites => _escondites;

        /// <summary>Si choca contra esto, es un obstáculo (no una pared por la que se pueda subir).</summary>
        public bool EsObstaculo(Collider collider) => collider != null && _obstaculos.Contains(collider);

        public AjustesDeSala Ajustes
        {
            get => ajustes;
            set
            {
                ajustes = value;
                if (isActiveAndEnabled) Construir();
            }
        }

        public Vector3 Medidas => ajustes != null ? ajustes.medidas : Vector3.one;

        private void OnEnable() => Construir();

        private void OnDisable() => Limpiar();

        private void Update()
        {
            // Si cambian las medidas (también mientras se juega), se vuelve a armar.
            if (ajustes != null && ajustes.Huella() != _huella) Construir();
        }

        /// <summary>
        /// Dónde arranca cada kart: hacia los extremos de la sala, mirando al centro, con lugar atrás para
        /// la cámara.
        /// </summary>
        public Pose Salida(int jugador)
        {
            var m = Medidas;
            float radio = ajustes != null ? ajustes.radioRincones : 0f;
            float x = (m.x * 0.5f - radio) * 0.65f, z = (m.z * 0.5f - radio) * 0.65f;
            Vector3 lugar, mira;
            switch ((jugador % 4 + 4) % 4)
            {
                case 0: lugar = new Vector3(0f, 0f, -z); mira = Vector3.forward; break;
                case 1: lugar = new Vector3(0f, 0f, z); mira = Vector3.back; break;
                case 2: lugar = new Vector3(-x, 0f, 0f); mira = Vector3.right; break;
                default: lugar = new Vector3(x, 0f, 0f); mira = Vector3.left; break;
            }
            return new Pose(transform.TransformPoint(lugar), transform.rotation * Quaternion.LookRotation(mira));
        }

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            Limpiar();
            if (ajustes == null) return;
            _huella = ajustes.Huella();
            _formas = new Formas();

            int capa = LayerMask.NameToLayer("Sala");
            if (capa >= 0 && gameObject.layer != capa) gameObject.layer = capa;

            ArmarParedes();
            ArmarVentana();
            ArmarPileta();
            ArmarObstaculos();
            ArmarMigas();
            ArmarLuces();
        }

        private void ArmarParedes()
        {
            var m = ajustes.medidas;
            var paredes = _formas.Nodo("Paredes", transform);
            paredes.localPosition = new Vector3(0f, m.y * 0.5f, 0f);

            var granito = Texturas.Granito();
            var azulejos = Texturas.Azulejos();
            var madera = Texturas.Madera();
            float azulejo = ajustes.tamanoAzulejo * 2f; // la textura trae dos por dos azulejos

            Cara(paredes, CajaRedondeada.Abajo, "Mesada", _formas.Mat(Formas.Hex("#d9d2dc"), 0.45f, granito), 3f);
            Cara(paredes, CajaRedondeada.Arriba, "Alacena", _formas.Mat(Formas.Hex("#c98a4f"), 0.3f, madera), 6f);
            Cara(paredes, CajaRedondeada.Derecha, "Heladera", _formas.Mat(Formas.Hex("#f4f6fb"), 0.75f), 0f);
            Cara(paredes, CajaRedondeada.Izquierda, "Pared de la pileta", _formas.Mat(Formas.Hex("#f3e9df"), 0.35f, azulejos), azulejo);
            Cara(paredes, CajaRedondeada.Adelante, "Pared de la ventana", _formas.Mat(Formas.Hex("#f3e9df"), 0.35f, azulejos), azulejo);
            Cara(paredes, CajaRedondeada.Atras, "Pared del frente", _formas.Mat(Formas.Hex("#f3e9df"), 0.35f, azulejos), azulejo);
        }

        private void Cara(Transform paredes, int cara, string nombre, Material material, float tamanoTextura)
        {
            var malla = CajaRedondeada.CrearCara(ajustes.medidas, ajustes.radioRincones, cara, true);
            var t = _formas.Malla(nombre, paredes, malla, material);
            t.gameObject.AddComponent<MeshCollider>().sharedMesh = malla;
            if (tamanoTextura > 0f)
            {
                Formas.Repetir(material, new Vector2(
                    Largo(CajaRedondeada.EjeU(cara)) / tamanoTextura,
                    Largo(CajaRedondeada.EjeV(cara)) / tamanoTextura));
            }
        }

        private float Largo(Vector3 eje)
        {
            var m = ajustes.medidas;
            return Mathf.Abs(eje.x) * m.x + Mathf.Abs(eje.y) * m.y + Mathf.Abs(eje.z) * m.z;
        }

        private void ArmarVentana()
        {
            var m = ajustes.medidas;
            var ventana = _formas.Nodo("Ventana al espacio", transform);
            ventana.localPosition = new Vector3(0f, m.y * 0.56f, m.z * 0.5f);
            float ancho = m.x * 0.38f, alto = m.y * 0.36f;

            var vidrio = _formas.Caja("Vidrio", ventana, new Vector3(0f, 0f, -0.04f), new Vector3(ancho, alto, 0.05f), 0.02f,
                _formas.Emisivo(Color.white, 1.2f, Texturas.Espacio()));
            Formas.SinSombra(vidrio);

            var marco = _formas.Mat(Formas.Hex("#9a5b2c"), 0.3f);
            const float grosor = 0.4f, fondo = 0.16f;
            float z = -fondo * 0.5f - 0.02f;
            _formas.Caja("Marco arriba", ventana, new Vector3(0f, alto * 0.5f, z), new Vector3(ancho + grosor * 2f, grosor, fondo), 0.06f, marco);
            _formas.Caja("Marco abajo", ventana, new Vector3(0f, -alto * 0.5f, z), new Vector3(ancho + grosor * 2f, grosor, fondo), 0.06f, marco);
            _formas.Caja("Marco izquierdo", ventana, new Vector3(-ancho * 0.5f, 0f, z), new Vector3(grosor, alto, fondo), 0.06f, marco);
            _formas.Caja("Marco derecho", ventana, new Vector3(ancho * 0.5f, 0f, z), new Vector3(grosor, alto, fondo), 0.06f, marco);
            _formas.Caja("Marco del medio", ventana, new Vector3(0f, 0f, z), new Vector3(grosor * 0.6f, alto, fondo), 0.04f, marco);
        }

        private void ArmarPileta()
        {
            // Plana sobre la mesada, para que se pueda pasar por encima.
            var m = ajustes.medidas;
            var pileta = _formas.Nodo("Pileta", transform);
            pileta.localPosition = new Vector3(-m.x * 0.5f + ajustes.radioRincones + 4f, 0f, m.z * 0.2f);
            _formas.Caja("Borde de acero", pileta, new Vector3(0f, 0.03f, 0f), new Vector3(6.5f, 0.06f, 4.5f), 0.03f, _formas.Mat(Formas.Hex("#aeb6c4"), 0.85f));
            _formas.Caja("Fondo", pileta, new Vector3(0f, 0.065f, 0f), new Vector3(5.6f, 0.02f, 3.6f), 0.01f, _formas.Mat(Formas.Hex("#6b7384"), 0.9f));
        }

        private void ArmarObstaculos()
        {
            // Van en las diagonales, para dejar libres el centro y las cuatro salidas.
            var m = ajustes.medidas;

            // Terrón de azúcar: para esconderse detrás.
            var terron = _formas.Caja("Terrón de azúcar", transform, new Vector3(-m.x * 0.25f, 1.6f, -m.z * 0.22f),
                Vector3.one * 3.2f, 0.45f, _formas.Mat(Formas.Hex("#fbfaf4"), 0.2f));
            terron.localRotation = Quaternion.Euler(0f, 18f, 0f);
            _obstaculos.Add(terron.gameObject.AddComponent<BoxCollider>());
            terron.gameObject.AddComponent<Obstaculo>();
            // Arriba resbala: se puede subir (tirándose desde el techo), pero no quedarse para siempre.
            terron.gameObject.AddComponent<SuperficieResbalosa>();
            _escondites.Add(new Escondite(transform.TransformPoint(new Vector3(terron.localPosition.x, 0f, terron.localPosition.z)), 2.3f));

            // Frasco de mermelada con su etiqueta y su tapa.
            var frasco = _formas.Nodo("Frasco de mermelada", transform);
            frasco.localPosition = new Vector3(m.x * 0.25f, 0f, m.z * 0.24f);
            var vidrio = _formas.Cilindro("Vidrio", frasco, new Vector3(0f, 2.75f, 0f), new Vector3(4f, 2.75f, 4f), Quaternion.identity,
                _formas.Mat(Formas.Hex("#e2475e"), 0.85f));
            var colision = vidrio.gameObject.AddComponent<MeshCollider>();
            colision.sharedMesh = vidrio.GetComponent<MeshFilter>().sharedMesh;
            colision.convex = true;
            _obstaculos.Add(colision);
            vidrio.gameObject.AddComponent<Obstaculo>();
            _escondites.Add(new Escondite(transform.TransformPoint(frasco.localPosition), 2f));
            _formas.Cilindro("Etiqueta", frasco, new Vector3(0f, 2.6f, 0f), new Vector3(4.06f, 0.9f, 4.06f), Quaternion.identity,
                _formas.Mat(Formas.Hex("#fff4dc"), 0.2f));
            var tapa = _formas.Cilindro("Tapa", frasco, new Vector3(0f, 5.85f, 0f), new Vector3(4.3f, 0.35f, 4.3f), Quaternion.identity,
                _formas.Mat(Formas.Hex("#f2c14e"), 0.5f));
            // La tapa también choca (así el kart se apoya arriba de ella) y resbala.
            var choqueTapa = tapa.gameObject.AddComponent<MeshCollider>();
            choqueTapa.sharedMesh = tapa.GetComponent<MeshFilter>().sharedMesh;
            choqueTapa.convex = true;
            _obstaculos.Add(choqueTapa);
            tapa.gameObject.AddComponent<Obstaculo>();
            tapa.gameObject.AddComponent<SuperficieResbalosa>();
        }

        private void ArmarMigas()
        {
            // Migas gigantes de adorno, sin choque.
            var m = ajustes.medidas;
            var migas = _formas.Nodo("Migas", transform);
            var rng = new System.Random(11);
            var colores = new[] { _formas.Mat(Formas.Hex("#e3b778"), 0.15f), _formas.Mat(Formas.Hex("#c98a4b"), 0.15f) };
            for (int i = 0; i < 12; i++)
            {
                var tamano = new Vector3(0.5f + (float)rng.NextDouble() * 0.5f, 0.25f + (float)rng.NextDouble() * 0.2f, 0.4f + (float)rng.NextDouble() * 0.5f);
                var lugar = new Vector3(((float)rng.NextDouble() - 0.5f) * (m.x - ajustes.radioRincones * 2f), tamano.y * 0.5f,
                    ((float)rng.NextDouble() - 0.5f) * (m.z - ajustes.radioRincones * 2f));
                var miga = _formas.Caja("Miga", migas, lugar, tamano, 0.12f, colores[i % 2]);
                miga.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
        }

        private void ArmarLuces()
        {
            // Luz cálida de la casa (una sola con sombra) y luz fría que entra por la ventana.
            var m = ajustes.medidas;
            float diagonal = m.magnitude;
            _formas.Luz("Luz cálida", transform, new Vector3(0f, m.y - 2.5f, -m.z * 0.05f), Formas.Hex("#ffd59a"), 60f, diagonal, true);
            _formas.Luz("Luz cálida izquierda", transform, new Vector3(-m.x * 0.3f, m.y - 3f, m.z * 0.2f), Formas.Hex("#ffcf8f"), 25f, diagonal * 0.6f, false);
            _formas.Luz("Luz cálida derecha", transform, new Vector3(m.x * 0.3f, m.y - 3f, -m.z * 0.25f), Formas.Hex("#ffcf8f"), 25f, diagonal * 0.6f, false);
            _formas.Luz("Luz del espacio", transform, new Vector3(0f, m.y * 0.55f, m.z * 0.5f - 3f), Formas.Hex("#8f9dff"), 30f, diagonal * 0.5f, false);
        }

        private void Limpiar()
        {
            _formas?.Limpiar();
            _formas = null;
            _obstaculos.Clear();
            _escondites.Clear();
        }
    }
}
