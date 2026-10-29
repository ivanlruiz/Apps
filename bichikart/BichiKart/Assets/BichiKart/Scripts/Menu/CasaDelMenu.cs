using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// La casa del menú principal, flotando en el espacio: una casita de dos pisos abierta de frente (como
    /// una casa de muñecas) donde cada cuarto es un modo. Abajo, la cocina (Historia), el living (Partida
    /// rápida) y el garaje con el kart de Blip (Garaje); arriba, el cuarto de los trofeos (Copas); en el
    /// techo, la antena (Online). Alrededor hay estrellas, planetas, nubes e islitas. Todo es de formas
    /// simples, se arma desde código y no se guarda en la escena.
    /// </summary>
    [ExecuteAlways]
    public sealed class CasaDelMenu : MonoBehaviour
    {
        public enum Cuarto { Historia, PartidaRapida, Copas, Online, Garaje }

        private const float AltoPlantaBaja = 3.2f, ProfundidadPlantaBaja = 8f, MitadAncho = 8.5f, Tabique = 3.2f;
        private const float PisoAlto = 3.55f, TechoAlto = 6.6f, MitadAnchoAlto = 4.2f, MitadProfundidadAlto = 3.6f, Cumbrera = 9.4f;

        private Formas _formas;
        private readonly List<Transform> _nubes = new List<Transform>();
        private readonly List<Vector3> _nubesBase = new List<Vector3>();

        /// <summary>Lo que se mira de cada cuarto (para la flecha del cartel y el viaje de la cámara).</summary>
        public Vector3 Centro(Cuarto cuarto)
        {
            switch (cuarto)
            {
                case Cuarto.Historia: return transform.TransformPoint(-5.85f, 1.3f, 1f);
                case Cuarto.PartidaRapida: return transform.TransformPoint(0f, 1.2f, 1f);
                case Cuarto.Copas: return transform.TransformPoint(0f, 5.1f, 1.5f);
                case Cuarto.Online: return transform.TransformPoint(2.6f, 10.6f, 0.8f);
                default: return transform.TransformPoint(5.85f, 1f, 0.6f);
            }
        }

        /// <summary>Dónde va el cartel de cada cuarto, al costado para no taparlo.</summary>
        public Vector3 Cartel(Cuarto cuarto)
        {
            switch (cuarto)
            {
                case Cuarto.Historia: return transform.TransformPoint(-11.5f, 3.2f, -3f);
                case Cuarto.PartidaRapida: return transform.TransformPoint(-3.2f, -1.6f, -5.5f);
                case Cuarto.Copas: return transform.TransformPoint(-7.2f, 7.4f, -3f);
                case Cuarto.Online: return transform.TransformPoint(8.4f, 10.6f, -1f);
                default: return transform.TransformPoint(11.8f, 1.6f, -3f);
            }
        }

        private void OnEnable() => Construir();

        private void OnDisable()
        {
            _formas?.Limpiar();
            _formas = null;
            _nubes.Clear();
            _nubesBase.Clear();
        }

        private void Update()
        {
            // Las nubes suben y bajan despacito.
            float t = Application.isPlaying ? Time.time : 0f;
            for (int i = 0; i < _nubes.Count; i++)
            {
                if (_nubes[i] == null) continue;
                _nubes[i].localPosition = _nubesBase[i] + Vector3.up * (Mathf.Sin(t * 0.5f + i * 1.7f) * 0.25f);
            }
        }

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            OnDisable();
            _formas = new Formas();
            var casa = _formas.Nodo("Casa", transform);
            ArmarBase(casa);
            ArmarPlantaBaja(casa);
            ArmarCocina(casa);
            ArmarLiving(casa);
            ArmarGaraje(casa);
            ArmarPisoDeArriba(casa);
            ArmarTechos(casa);
            ArmarEspacio();
        }

        private Material Pintura(string hex, float brillo = 0.35f) => _formas.Mat(Formas.Hex(hex), brillo);

        private Material Luz(string hex, float fuerza) => _formas.Emisivo(Formas.Hex(hex), fuerza);

        private Transform Caja(string nombre, Transform padre, Vector3 lugar, Vector3 tamano, Material material, float radio = 0.08f)
        {
            float maximo = Mathf.Min(tamano.x, Mathf.Min(tamano.y, tamano.z)) * 0.45f;
            return _formas.Caja(nombre, padre, lugar, tamano, Mathf.Min(radio, maximo), material);
        }

        private void ArmarBase(Transform casa)
        {
            Caja("Base", casa, new Vector3(0f, -0.45f, 0f), new Vector3(19.5f, 0.9f, 10.5f), Pintura("#9a9cc4"), 0.4f);
            Caja("Borde de la base", casa, new Vector3(0f, -1.05f, 0f), new Vector3(18.5f, 0.5f, 9.6f), Pintura("#7c7fac"), 0.25f);
        }

        /// <summary>Paredes de atrás y de los costados, tabiques, columnas del frente y el techo de la planta baja.</summary>
        private void ArmarPlantaBaja(Transform casa)
        {
            var amarillo = Pintura("#f6c453");
            var crema = Pintura("#fff3df");
            float z = ProfundidadPlantaBaja * 0.5f, alto = AltoPlantaBaja;

            Caja("Pared de la cocina", casa, new Vector3(-5.85f, alto * 0.5f, z), new Vector3(5.3f, alto, 0.3f), Pintura("#cdebd6"));
            Caja("Pared del living", casa, new Vector3(0f, alto * 0.5f, z), new Vector3(6.4f, alto, 0.3f), Pintura("#f7d2bd"));
            Caja("Pared del garaje", casa, new Vector3(5.85f, alto * 0.5f, z), new Vector3(5.3f, alto, 0.3f), Pintura("#b7bed2"));
            Caja("Costado izquierdo", casa, new Vector3(-MitadAncho, alto * 0.5f, 0f), new Vector3(0.35f, alto, ProfundidadPlantaBaja + 0.3f), amarillo);
            Caja("Costado derecho", casa, new Vector3(MitadAncho, alto * 0.5f, 0f), new Vector3(0.35f, alto, ProfundidadPlantaBaja + 0.3f), amarillo);
            Caja("Tabique izquierdo", casa, new Vector3(-Tabique, alto * 0.5f, 0.3f), new Vector3(0.22f, alto, ProfundidadPlantaBaja - 0.6f), crema);
            Caja("Tabique derecho", casa, new Vector3(Tabique, alto * 0.5f, 0.3f), new Vector3(0.22f, alto, ProfundidadPlantaBaja - 0.6f), crema);
            foreach (float x in new[] { -MitadAncho, -Tabique, Tabique, MitadAncho })
                Caja("Columna", casa, new Vector3(x, alto * 0.5f, -z), new Vector3(0.45f, alto, 0.45f), amarillo, 0.12f);

            Caja("Techo de la planta baja", casa, new Vector3(0f, alto + 0.18f, 0f), new Vector3(MitadAncho * 2f + 0.5f, 0.36f, ProfundidadPlantaBaja + 0.5f), crema);
            Caja("Viga del frente", casa, new Vector3(0f, alto + 0.18f, -z - 0.05f), new Vector3(MitadAncho * 2f + 0.6f, 0.42f, 0.3f), amarillo, 0.12f);
        }

        private void ArmarCocina(Transform casa)
        {
            var piso = _formas.Mat(Formas.Hex("#eef3ff"), 0.5f, Texturas.Azulejos());
            Formas.Repetir(piso, new Vector2(3f, 4f));
            Caja("Piso de la cocina", casa, new Vector3(-5.85f, 0.03f, 0f), new Vector3(5.2f, 0.06f, 7.9f), piso, 0.02f);

            var blanco = Pintura("#f4f1ea");
            var madera = Pintura("#b07a45");
            Caja("Mesada", casa, new Vector3(-5.6f, 0.5f, 3.3f), new Vector3(3.6f, 1f, 1f), blanco);
            Caja("Tapa de la mesada", casa, new Vector3(-5.6f, 1.06f, 3.3f), new Vector3(3.7f, 0.12f, 1.08f), Pintura("#d9cfd6", 0.6f), 0.04f);
            Caja("Hornalla", casa, new Vector3(-4.7f, 1.14f, 3.3f), new Vector3(1f, 0.05f, 0.75f), Pintura("#33364a", 0.6f), 0.02f);
            Caja("Alacena", casa, new Vector3(-5.6f, 2.45f, 3.55f), new Vector3(3.2f, 0.8f, 0.55f), madera);
            Caja("Heladera", casa, new Vector3(-7.75f, 1.2f, 3.15f), new Vector3(1.1f, 2.4f, 1.1f), Pintura("#e7f0ff", 0.7f), 0.15f);
            _formas.Cilindro("Mesa", casa, new Vector3(-5.6f, 0.85f, 0.6f), new Vector3(1.6f, 0.05f, 1.6f), Quaternion.identity, madera);
            _formas.Cilindro("Pata de la mesa", casa, new Vector3(-5.6f, 0.42f, 0.6f), new Vector3(0.18f, 0.42f, 0.18f), Quaternion.identity, madera);
            // Una rebanada de pan arriba de la mesa, como en el juego.
            Caja("Rebanada de pan", casa, new Vector3(-5.5f, 0.97f, 0.55f), new Vector3(0.6f, 0.14f, 0.55f), Pintura("#e7b064"), 0.06f);
            Caja("Silla", casa, new Vector3(-6.9f, 0.5f, 0.6f), new Vector3(0.6f, 0.08f, 0.6f), Pintura("#7fc4a0"), 0.03f);
            Caja("Silla", casa, new Vector3(-4.3f, 0.5f, 0.6f), new Vector3(0.6f, 0.08f, 0.6f), Pintura("#7fc4a0"), 0.03f);
            _formas.Luz("Luz de la cocina", casa, new Vector3(-5.8f, 2.8f, 0.5f), Formas.Hex("#ffe2b0"), 3f, 7f, false);
        }

        private void ArmarLiving(Transform casa)
        {
            var piso = _formas.Mat(Formas.Hex("#d9a36d"), 0.4f, Texturas.Madera());
            Formas.Repetir(piso, new Vector2(2f, 2f));
            Caja("Piso del living", casa, new Vector3(0f, 0.03f, 0f), new Vector3(6.3f, 0.06f, 7.9f), piso, 0.02f);
            _formas.Cilindro("Alfombra", casa, new Vector3(0.4f, 0.07f, 1.3f), new Vector3(4.2f, 0.01f, 2.8f), Quaternion.identity, Pintura("#f29bb3"));

            var naranja = Pintura("#f08a4b");
            Caja("Sillón", casa, new Vector3(0.7f, 0.42f, 2.75f), new Vector3(3.2f, 0.55f, 1.2f), naranja, 0.2f);
            Caja("Respaldo", casa, new Vector3(0.7f, 1f, 3.35f), new Vector3(3.2f, 0.95f, 0.4f), naranja, 0.18f);
            Caja("Brazo", casa, new Vector3(-0.85f, 0.75f, 2.75f), new Vector3(0.4f, 0.75f, 1.2f), naranja, 0.15f);
            Caja("Brazo", casa, new Vector3(2.25f, 0.75f, 2.75f), new Vector3(0.4f, 0.75f, 1.2f), naranja, 0.15f);
            Caja("Almohadón", casa, new Vector3(0.1f, 0.9f, 2.95f), new Vector3(0.7f, 0.6f, 0.25f), Pintura("#ffd166"), 0.12f);
            Caja("Mesita", casa, new Vector3(0.7f, 0.32f, 1.1f), new Vector3(1.5f, 0.12f, 0.8f), Pintura("#b07a45"), 0.05f);

            // La tele, en un mueble a la izquierda, mirando al sillón.
            var tele = _formas.Nodo("Tele", casa);
            tele.localPosition = new Vector3(-2.2f, 0f, 1.3f);
            tele.localRotation = Quaternion.Euler(0f, 55f, 0f);
            Caja("Mueble", tele, new Vector3(0f, 0.35f, 0f), new Vector3(1.6f, 0.7f, 0.6f), Pintura("#7a5a8c"));
            Caja("Pantalla", tele, new Vector3(0f, 1.25f, 0f), new Vector3(1.4f, 0.85f, 0.12f), Pintura("#2a2d40", 0.7f), 0.05f);
            Caja("Imagen", tele, new Vector3(0f, 1.25f, 0.07f), new Vector3(1.25f, 0.7f, 0.02f), Luz("#7fc8ff", 1.6f), 0.01f);

            Caja("Cuadro", casa, new Vector3(-0.9f, 2.1f, 3.82f), new Vector3(1.3f, 0.9f, 0.06f), Luz("#ffb86b", 0.6f), 0.03f);
            _formas.Cilindro("Pie de la lámpara", casa, new Vector3(2.75f, 0.9f, 3f), new Vector3(0.08f, 0.9f, 0.08f), Quaternion.identity, Pintura("#3b3f58"));
            Formas.SinSombra(_formas.Esfera("Lámpara", casa, new Vector3(2.75f, 1.95f, 3f), new Vector3(0.55f, 0.45f, 0.55f), Luz("#ffe4a8", 2f)));
            _formas.Luz("Luz del living", casa, new Vector3(1f, 2.6f, 1f), Formas.Hex("#ffdcb0"), 3.5f, 8f, false);
        }

        private void ArmarGaraje(Transform casa)
        {
            Caja("Piso del garaje", casa, new Vector3(5.85f, 0.03f, 0f), new Vector3(5.2f, 0.06f, 7.9f), Pintura("#8f95a8", 0.25f), 0.02f);
            // El portón levantado arriba de la entrada.
            Caja("Portón", casa, new Vector3(5.85f, 2.85f, -3.75f), new Vector3(5f, 0.55f, 0.25f), Pintura("#d6dbe8", 0.5f), 0.08f);
            Caja("Raya del portón", casa, new Vector3(5.85f, 2.85f, -3.9f), new Vector3(5f, 0.08f, 0.05f), Pintura("#9aa2b8"), 0.02f);

            // El kart de Blip estacionado.
            var kart = _formas.Nodo("Kart del garaje", casa);
            kart.localPosition = new Vector3(5.7f, 0.06f, 0.7f);
            kart.localRotation = Quaternion.Euler(0f, 205f, 0f);
            kart.gameObject.AddComponent<ModeloKart>();

            var negro = Pintura("#2b2d3a", 0.3f);
            for (int i = 0; i < 3; i++)
                _formas.Cilindro("Cubierta", casa, new Vector3(7.75f, 0.15f + i * 0.3f, 3.1f), new Vector3(0.8f, 0.14f, 0.8f), Quaternion.identity, negro);
            Caja("Estante", casa, new Vector3(5f, 1.8f, 3.65f), new Vector3(2.6f, 0.1f, 0.5f), Pintura("#b07a45"), 0.03f);
            Caja("Caja de herramientas", casa, new Vector3(4.4f, 2.07f, 3.65f), new Vector3(0.8f, 0.45f, 0.4f), Pintura("#e2483d"), 0.06f);
            Caja("Lata", casa, new Vector3(5.6f, 2.05f, 3.65f), new Vector3(0.35f, 0.4f, 0.35f), Pintura("#4fb3e8"), 0.08f);
            _formas.Luz("Luz del garaje", casa, new Vector3(5.85f, 2.8f, 0.8f), Formas.Hex("#dfe9ff"), 3f, 7f, false);
        }

        /// <summary>El cuarto de los trofeos, con la ventana grande al frente y la ventanita redonda arriba.</summary>
        private void ArmarPisoDeArriba(Transform casa)
        {
            var amarillo = Pintura("#f6c453");
            var blanco = Pintura("#fff6e8", 0.5f);
            float alto = TechoAlto - PisoAlto, medio = (PisoAlto + TechoAlto) * 0.5f, frente = -MitadProfundidadAlto + 0.15f;

            Caja("Pared de atrás", casa, new Vector3(0f, medio, MitadProfundidadAlto - 0.15f), new Vector3(MitadAnchoAlto * 2f, alto, 0.3f), amarillo);
            Caja("Pared por dentro", casa, new Vector3(0f, medio, MitadProfundidadAlto - 0.32f), new Vector3(MitadAnchoAlto * 2f - 0.6f, alto - 0.1f, 0.04f), Pintura("#d8c6f2"), 0.01f);
            Caja("Costado izquierdo", casa, new Vector3(-MitadAnchoAlto + 0.15f, medio, 0f), new Vector3(0.3f, alto, MitadProfundidadAlto * 2f), amarillo);
            Caja("Costado derecho", casa, new Vector3(MitadAnchoAlto - 0.15f, medio, 0f), new Vector3(0.3f, alto, MitadProfundidadAlto * 2f), amarillo);

            // Frente con la ventana: x de -2,4 a 2,4 y alto de 4,1 a 6,1.
            Caja("Frente izquierdo", casa, new Vector3(-3.3f, medio, frente), new Vector3(1.8f, alto, 0.3f), amarillo);
            Caja("Frente derecho", casa, new Vector3(3.3f, medio, frente), new Vector3(1.8f, alto, 0.3f), amarillo);
            Caja("Bajo la ventana", casa, new Vector3(0f, (PisoAlto + 4.1f) * 0.5f, frente), new Vector3(4.8f, 4.1f - PisoAlto, 0.3f), amarillo);
            Caja("Sobre la ventana", casa, new Vector3(0f, (6.1f + TechoAlto) * 0.5f, frente), new Vector3(4.8f, TechoAlto - 6.1f, 0.3f), amarillo);
            Caja("Marco de abajo", casa, new Vector3(0f, 4.1f, frente - 0.1f), new Vector3(5.1f, 0.18f, 0.25f), blanco, 0.05f);
            Caja("Marco de arriba", casa, new Vector3(0f, 6.1f, frente - 0.1f), new Vector3(5.1f, 0.18f, 0.25f), blanco, 0.05f);
            foreach (float x in new[] { -2.45f, -0.8f, 0.8f, 2.45f })
                Caja("Marco", casa, new Vector3(x, 5.1f, frente - 0.1f), new Vector3(0.16f, 2.1f, 0.22f), blanco, 0.05f);

            // Los trofeos en un estante.
            var dorado = _formas.Mat(Formas.Hex("#ffc93c"), 0.85f);
            Caja("Estante", casa, new Vector3(0f, 4.35f, 1.2f), new Vector3(5.6f, 0.18f, 1f), Pintura("#b07a45"), 0.05f);
            for (int i = -1; i <= 1; i++)
            {
                float grande = i == 0 ? 1.25f : 1f;
                var trofeo = _formas.Nodo("Trofeo", casa);
                trofeo.localPosition = new Vector3(i * 1.7f, 4.45f, 1.2f);
                trofeo.localScale = Vector3.one * grande;
                Caja("Base", trofeo, new Vector3(0f, 0.12f, 0f), new Vector3(0.55f, 0.24f, 0.55f), Pintura("#6b4a2f"), 0.05f);
                _formas.Cilindro("Pie", trofeo, new Vector3(0f, 0.42f, 0f), new Vector3(0.16f, 0.18f, 0.16f), Quaternion.identity, dorado);
                _formas.Esfera("Copa", trofeo, new Vector3(0f, 0.85f, 0f), new Vector3(0.75f, 0.65f, 0.75f), dorado);
                _formas.Esfera("Asa", trofeo, new Vector3(-0.42f, 0.9f, 0f), new Vector3(0.22f, 0.3f, 0.12f), dorado);
                _formas.Esfera("Asa", trofeo, new Vector3(0.42f, 0.9f, 0f), new Vector3(0.22f, 0.3f, 0.12f), dorado);
            }
            _formas.Luz("Luz de los trofeos", casa, new Vector3(0f, 6f, 0f), Formas.Hex("#ffe7b8"), 3f, 7f, false);
        }

        private void ArmarTechos(Transform casa)
        {
            var amarillo = Pintura("#f6c453");
            var rojo = Pintura("#e05a3f", 0.4f);
            var oscuro = Pintura("#b8432f", 0.4f);

            // El techo de dos aguas del piso de arriba, con el frente triangular (y su ventanita redonda).
            Prisma("Frente del techo", casa, new Vector2(-MitadAnchoAlto, TechoAlto), new Vector2(MitadAnchoAlto, TechoAlto), new Vector2(0f, Cumbrera), MitadProfundidadAlto * 2f, amarillo);
            // Cada agua va de la cumbrera hasta pasar un poco la pared, apoyada sobre la línea del techo.
            const float alero = 0.6f, grosor = 0.32f;
            float pendiente = (Cumbrera - TechoAlto) / MitadAnchoAlto;
            float angulo = Mathf.Atan(pendiente) * Mathf.Rad2Deg;
            float medio = (MitadAnchoAlto + alero) * 0.5f;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var agua = Caja("Agua del techo", casa,
                    new Vector3(lado * medio, Cumbrera - medio * pendiente + grosor * 0.5f / Mathf.Cos(angulo * Mathf.Deg2Rad), 0f),
                    new Vector3((MitadAnchoAlto + alero) / Mathf.Cos(angulo * Mathf.Deg2Rad), grosor, MitadProfundidadAlto * 2f + 0.8f), rojo, 0.12f);
                agua.localRotation = Quaternion.Euler(0f, 0f, -lado * angulo);
            }
            Caja("Cumbrera", casa, new Vector3(0f, Cumbrera + 0.2f, 0f), new Vector3(0.45f, 0.35f, MitadProfundidadAlto * 2f + 0.9f), oscuro, 0.15f);
            _formas.Cilindro("Marco de la ventanita", casa, new Vector3(0f, 7.6f, -MitadProfundidadAlto - 0.02f), new Vector3(1.3f, 0.08f, 1.3f), Quaternion.Euler(90f, 0f, 0f), Pintura("#fff6e8", 0.5f));
            Formas.SinSombra(_formas.Cilindro("Ventanita", casa, new Vector3(0f, 7.6f, -MitadProfundidadAlto - 0.08f), new Vector3(1f, 0.06f, 1f), Quaternion.Euler(90f, 0f, 0f), Luz("#ffd68a", 1.4f)));

            // Los techos de una sola agua de la cocina y del garaje: altos contra el piso de arriba y bajos afuera.
            const float alto = 5.2f, bajo = 3.5f;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float adentro = lado * MitadAnchoAlto, afuera = lado * MitadAncho;
                Prisma("Costado del techo", casa, new Vector2(afuera, bajo), new Vector2(adentro, bajo), new Vector2(adentro, alto), ProfundidadPlantaBaja, amarillo);
                float ancho = MitadAncho - MitadAnchoAlto;
                float caida = (alto - bajo) / ancho;
                float inclinacion = Mathf.Atan(caida) * Mathf.Rad2Deg;
                float centro = (adentro + afuera) * 0.5f + lado * 0.2f;
                float y = alto - Mathf.Abs(centro - adentro) * caida + grosor * 0.5f / Mathf.Cos(inclinacion * Mathf.Deg2Rad);
                var agua = Caja("Techo del costado", casa, new Vector3(centro, y, 0f),
                    new Vector3((ancho + 0.8f) / Mathf.Cos(inclinacion * Mathf.Deg2Rad), grosor, ProfundidadPlantaBaja + 0.8f), rojo, 0.12f);
                agua.localRotation = Quaternion.Euler(0f, 0f, -lado * inclinacion);
            }

            // La antena del techo, con su lucecita.
            var gris = Pintura("#c9cede", 0.6f);
            _formas.Cilindro("Palo de la antena", casa, new Vector3(2.6f, 9.2f, 0.8f), new Vector3(0.14f, 1.1f, 0.14f), Quaternion.identity, gris);
            _formas.Esfera("Plato", casa, new Vector3(2.6f, 10.5f, 0.8f), new Vector3(1.6f, 0.45f, 1.6f), gris).localRotation = Quaternion.Euler(-35f, 20f, 0f);
            Formas.SinSombra(_formas.Esfera("Luz de la antena", casa, new Vector3(2.6f, 11.1f, 0.6f), Vector3.one * 0.3f, Luz("#ff5a5a", 3f)));
        }

        // ------------------------------------------------------------------ el espacio

        private void ArmarEspacio()
        {
            var espacio = _formas.Nodo("Espacio", transform);
            var rng = new System.Random(11);
            float Azar(float desde, float hasta) => desde + (float)rng.NextDouble() * (hasta - desde);

            // El cielo: una esfera enorme vista desde adentro, con la textura del espacio de la ventana.
            var cielo = _formas.Emisivo(UnityEngine.Color.white, 0.9f, Texturas.Espacio());
            Formas.Repetir(cielo, new Vector2(3f, 1.5f));
            var esfera = _formas.Malla("Cielo", espacio, EsferaPorDentro(), cielo);
            esfera.localScale = Vector3.one * 400f;
            Formas.SinSombra(esfera);

            // Estrellas sueltas, para que brillen nítidas.
            var estrella = Luz("#ffffff", 3f);
            var estrellaAmarilla = Luz("#fff1a8", 3f);
            for (int i = 0; i < 160; i++)
            {
                var direccion = new Vector3(Azar(-1f, 1f), Azar(-0.6f, 1f), Azar(-0.2f, 1f)).normalized;
                var punto = _formas.Esfera("Estrella", espacio, direccion * Azar(120f, 170f), Vector3.one * Azar(0.3f, 0.9f), i % 4 == 0 ? estrellaAmarilla : estrella);
                Formas.SinSombra(punto);
            }

            // Planetas.
            Planeta(espacio, new Vector3(34f, 18f, 48f), 16f, "#3fc6a8");
            Planeta(espacio, new Vector3(-38f, -8f, 42f), 10f, "#9b6bff");
            Planeta(espacio, new Vector3(-16f, 20f, 36f), 3.2f, "#ff9f5a");
            Planeta(espacio, new Vector3(20f, -16f, 30f), 5f, "#ff7eb6");

            // Nubes debajo de la casa y alrededor.
            Nube(espacio, new Vector3(-6f, -2.2f, 0f), 3f, rng);
            Nube(espacio, new Vector3(4f, -2.4f, 1f), 3.4f, rng);
            Nube(espacio, new Vector3(-13f, 1.5f, 5f), 2.2f, rng);
            Nube(espacio, new Vector3(13.5f, -0.5f, 4f), 2.4f, rng);
            Nube(espacio, new Vector3(1f, -3.5f, -4f), 2.6f, rng);

            // Islitas con pasto flotando.
            Islita(espacio, new Vector3(-15f, -4f, -2f), 2.4f, "#ff6fa0");
            Islita(espacio, new Vector3(16f, 6.5f, 6f), 1.6f, "#ffd23f");
        }

        private void Planeta(Transform padre, Vector3 lugar, float tamano, string color)
        {
            Formas.SinSombra(_formas.Esfera("Planeta", padre, lugar, Vector3.one * tamano, _formas.Mat(Formas.Hex(color), 0.2f)));
        }

        private void Nube(Transform padre, Vector3 lugar, float tamano, System.Random rng)
        {
            var nube = _formas.Nodo("Nube", padre);
            nube.localPosition = lugar;
            var blanco = _formas.Mat(Formas.Hex("#ece4ff"), 0.15f);
            for (int i = 0; i < 6; i++)
            {
                var desvio = new Vector3((float)rng.NextDouble() - 0.5f, ((float)rng.NextDouble() - 0.5f) * 0.4f, (float)rng.NextDouble() - 0.5f) * tamano * 1.4f;
                float bola = tamano * (0.55f + (float)rng.NextDouble() * 0.5f);
                Formas.SinSombra(_formas.Esfera("Bola", nube, desvio, new Vector3(bola, bola * 0.7f, bola), blanco));
            }
            _nubes.Add(nube);
            _nubesBase.Add(lugar);
        }

        private void Islita(Transform padre, Vector3 lugar, float tamano, string colorFlor)
        {
            var isla = _formas.Nodo("Islita", padre);
            isla.localPosition = lugar;
            _formas.Esfera("Roca", isla, new Vector3(0f, -tamano * 0.35f, 0f), new Vector3(tamano * 1.3f, tamano, tamano * 1.2f), _formas.Mat(Formas.Hex("#8a86a3"), 0.2f));
            _formas.Esfera("Pasto", isla, new Vector3(0f, tamano * 0.1f, 0f), new Vector3(tamano * 1.25f, tamano * 0.35f, tamano * 1.15f), _formas.Mat(Formas.Hex("#6cc96a"), 0.2f));
            _formas.Cilindro("Tallo", isla, new Vector3(0.2f, tamano * 0.45f, 0f), new Vector3(0.08f, tamano * 0.25f, 0.08f), Quaternion.identity, _formas.Mat(Formas.Hex("#3f9c4a"), 0.2f));
            _formas.Esfera("Flor", isla, new Vector3(0.2f, tamano * 0.72f, 0f), Vector3.one * tamano * 0.25f, _formas.Mat(Formas.Hex(colorFlor), 0.3f));
        }

        // ------------------------------------------------------------------ mallas

        /// <summary>
        /// Prisma con un triángulo (en X e Y) estirado a lo largo de Z, centrado en Z = 0. Sirve para los
        /// frentes triangulares de los techos. Cada cara es plana y mira hacia afuera.
        /// </summary>
        private void Prisma(string nombre, Transform padre, Vector2 a, Vector2 b, Vector2 c, float largo, Material material)
        {
            float z = largo * 0.5f;
            Vector3 A(Vector2 p, float lado) => new Vector3(p.x, p.y, lado * z);
            var centro = new Vector3((a.x + b.x + c.x) / 3f, (a.y + b.y + c.y) / 3f, 0f);
            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var triangulos = new List<int>();

            void Triangulo(Vector3 p, Vector3 q, Vector3 r)
            {
                var normal = Vector3.Cross(q - p, r - p).normalized;
                if (Vector3.Dot(normal, (p + q + r) / 3f - centro) < 0f)
                {
                    (q, r) = (r, q);
                    normal = -normal;
                }
                int i = vertices.Count;
                vertices.Add(p); vertices.Add(q); vertices.Add(r);
                normales.Add(normal); normales.Add(normal); normales.Add(normal);
                triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
            }

            Triangulo(A(a, -1f), A(b, -1f), A(c, -1f));
            Triangulo(A(a, 1f), A(b, 1f), A(c, 1f));
            var esquinas = new[] { a, b, c };
            for (int i = 0; i < 3; i++)
            {
                var p = esquinas[i];
                var q = esquinas[(i + 1) % 3];
                Triangulo(A(p, -1f), A(q, -1f), A(q, 1f));
                Triangulo(A(p, -1f), A(q, 1f), A(p, 1f));
            }

            var malla = new Mesh { name = nombre };
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            _formas.Malla(nombre, padre, malla, material);
        }

        /// <summary>Una esfera de radio 0,5 con las caras para adentro, para el cielo.</summary>
        private static Mesh EsferaPorDentro()
        {
            var molde = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var original = molde.GetComponent<MeshFilter>().sharedMesh;
            var malla = new Mesh { name = "Cielo", vertices = original.vertices, uv = original.uv };
            var normales = original.normals;
            for (int i = 0; i < normales.Length; i++) normales[i] = -normales[i];
            malla.normals = normales;
            var triangulos = original.triangles;
            for (int i = 0; i < triangulos.Length; i += 3) (triangulos[i + 1], triangulos[i + 2]) = (triangulos[i + 2], triangulos[i + 1]);
            malla.triangles = triangulos;
            malla.RecalculateBounds();
            if (Application.isPlaying) Destroy(molde);
            else DestroyImmediate(molde);
            return malla;
        }
    }
}
