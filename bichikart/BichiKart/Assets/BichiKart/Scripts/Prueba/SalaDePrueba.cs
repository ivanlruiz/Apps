using System.Collections.Generic;
using UnityEngine;

namespace BichiKart.Prueba
{
    /// <summary>
    /// Sala de prueba del proyecto base: un rincón de la cocina con formas simples (mesada, pared de
    /// azulejos con ventana al espacio, heladera gigante, alacena como techo), el kart de Blip y la
    /// rebanada de pan. Sirve para comprobar que el proyecto abre y que URP se ve bien.
    ///
    /// Todo se arma desde código al abrir la escena y no se guarda en ella. Todavía no se maneja:
    /// eso llega con el prototipo.
    /// </summary>
    [ExecuteAlways]
    public sealed class SalaDePrueba : MonoBehaviour
    {
        private readonly List<Object> _creados = new List<Object>();
        private readonly Dictionary<Color, Material> _materiales = new Dictionary<Color, Material>();
        private Shader _shader;
        private Transform _pan;
        private Transform _antena;
        private Vector3 _panInicio;

        private void OnEnable() => Construir();

        private void OnDisable() => Limpiar();

        private void Update()
        {
            if (!Application.isPlaying || _pan == null) return;
            float t = Time.time;
            _pan.localPosition = _panInicio + Vector3.up * Mathf.Sin(t * 2f) * 0.15f;
            _pan.localRotation = Quaternion.Euler(0f, t * 40f, Mathf.Sin(t * 1.5f) * 6f);
            if (_antena != null) _antena.localRotation = Quaternion.Euler(Mathf.Sin(t * 9f) * 12f, 0f, Mathf.Cos(t * 7f) * 10f);
        }

        // ------------------------------------------------------------------ armado

        private void Construir()
        {
            Limpiar();
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("Standard");

            ArmarSala();
            ArmarBocasDeCanos();
            ArmarPan();
            ArmarKartDeBlip();
            ArmarLuces();
        }

        private void ArmarSala()
        {
            var granito = Mat(Hex("#d9d2dc"), 0.45f, TexturaGranito());
            var azulejos = Mat(Hex("#f3e9df"), 0.3f, TexturaAzulejos());
            var madera = Mat(Hex("#b9773f"), 0.25f);
            var heladera = Mat(Hex("#f4f6fb"), 0.7f);

            // Mesada (piso), alacena (techo), pared del fondo y pared izquierda. El frente queda abierto para la cámara.
            Caja("Mesada", new Vector3(0f, -0.5f, 2f), new Vector3(26f, 1f, 19f), 0.4f, granito);
            Caja("Alacena", new Vector3(0f, 9.5f, 2f), new Vector3(26f, 1f, 19f), 0.4f, madera);
            Caja("Pared del fondo", new Vector3(0f, 4.5f, 11.5f), new Vector3(26f, 10f, 1f), 0.2f, azulejos);
            Caja("Pared de azulejos", new Vector3(-12.5f, 4.5f, 2f), new Vector3(1f, 10f, 19f), 0.2f, azulejos);

            // Costado de una heladera gigante, con su manija.
            Caja("Heladera", new Vector3(13.5f, 4.5f, 2f), new Vector3(3f, 10f, 19f), 0.8f, heladera);
            Caja("Manija de la heladera", new Vector3(11.85f, 5f, -2f), new Vector3(0.35f, 4f, 0.5f), 0.17f, Mat(Hex("#c8cfdc"), 0.8f));

            // Ventana al espacio con marco de madera.
            var ventana = Caja("Ventana al espacio", new Vector3(0f, 4.8f, 10.95f), new Vector3(9f, 5.2f, 0.1f), 0.05f, MaterialEspacio());
            ventana.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var marco = Mat(Hex("#9a5b2c"), 0.3f);
            Caja("Marco arriba", new Vector3(0f, 7.55f, 10.8f), new Vector3(9.8f, 0.4f, 0.5f), 0.15f, marco);
            Caja("Marco abajo", new Vector3(0f, 2.05f, 10.8f), new Vector3(9.8f, 0.4f, 0.5f), 0.15f, marco);
            Caja("Marco izquierdo", new Vector3(-4.7f, 4.8f, 10.8f), new Vector3(0.4f, 5.9f, 0.5f), 0.15f, marco);
            Caja("Marco derecho", new Vector3(4.7f, 4.8f, 10.8f), new Vector3(0.4f, 5.9f, 0.5f), 0.15f, marco);
            Caja("Marco del medio", new Vector3(0f, 4.8f, 10.8f), new Vector3(0.25f, 5.2f, 0.3f), 0.1f, marco);

            // Pileta: borde de acero y fondo más oscuro, con la canilla.
            var acero = Mat(Hex("#aeb6c4"), 0.85f);
            Caja("Pileta", new Vector3(-7f, 0.08f, 6.5f), new Vector3(6.5f, 0.16f, 4.5f), 0.08f, acero);
            Caja("Fondo de la pileta", new Vector3(-7f, 0.17f, 6.5f), new Vector3(5.6f, 0.02f, 3.6f), 0.01f, Mat(Hex("#6b7384"), 0.9f));
            Cilindro("Canilla", new Vector3(-7f, 1.6f, 9.6f), new Vector3(0.45f, 1.6f, 0.45f), Quaternion.identity, acero);
            Cilindro("Pico de la canilla", new Vector3(-7f, 3.05f, 8.9f), new Vector3(0.35f, 0.8f, 0.35f), Quaternion.Euler(90f, 0f, 0f), acero);
        }

        private void ArmarBocasDeCanos()
        {
            // Rejillas de caños reales con su lucecita: las bocas del mismo color están conectadas.
            Rejilla("Desagüe azul", new Vector3(5f, 0.03f, -1f), Quaternion.identity, Hex("#3e8ef7"));
            Rejilla("Rejilla azul del techo", new Vector3(-4f, 8.97f, 4f), Quaternion.Euler(180f, 0f, 0f), Hex("#3e8ef7"));
            Rejilla("Rejilla verde de la pared", new Vector3(-11.97f, 5f, 3f), Quaternion.Euler(0f, 0f, -90f), Hex("#30d158"));
            Rejilla("Rejilla verde de la heladera", new Vector3(11.97f, 3f, 6f), Quaternion.Euler(0f, 0f, 90f), Hex("#30d158"));
        }

        private void Rejilla(string nombre, Vector3 posicion, Quaternion rotacion, Color luz)
        {
            var raiz = Nodo(nombre, transform);
            raiz.localPosition = posicion;
            raiz.localRotation = rotacion;
            var metal = Mat(Hex("#8b93a3"), 0.7f);
            Cilindro("Aro", Vector3.zero, new Vector3(1.6f, 0.03f, 1.6f), Quaternion.identity, metal, raiz);
            Cilindro("Agujero", new Vector3(0f, 0.02f, 0f), new Vector3(1.3f, 0.02f, 1.3f), Quaternion.identity, Mat(Hex("#1d2340"), 0.2f), raiz);
            for (int i = -2; i <= 2; i++)
                Caja("Barrita", new Vector3(i * 0.24f, 0.05f, 0f), new Vector3(0.08f, 0.04f, 1.2f - Mathf.Abs(i) * 0.2f), 0.02f, metal, raiz);
            var foco = Esfera("Lucecita", new Vector3(1.05f, 0.08f, 0f), Vector3.one * 0.18f, MaterialEmisivo(luz, 4f), raiz);
            var l = foco.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = luz;
            l.intensity = 2f;
            l.range = 2.5f;
        }

        private void ArmarPan()
        {
            _pan = Nodo("Rebanada de pan", transform);
            _panInicio = new Vector3(0f, 2.6f, 3.5f);
            _pan.localPosition = _panInicio;
            var corteza = Mat(Hex("#c98a4b"), 0.2f);
            var miga = Mat(Hex("#f6dfae"), 0.1f);
            Caja("Corteza", new Vector3(0f, 0f, 0f), new Vector3(2.2f, 2f, 0.45f), 0.2f, corteza, _pan);
            Esfera("Copete izquierdo", new Vector3(-0.55f, 1f, 0f), new Vector3(1.25f, 0.95f, 0.45f), corteza, _pan);
            Esfera("Copete derecho", new Vector3(0.55f, 1f, 0f), new Vector3(1.25f, 0.95f, 0.45f), corteza, _pan);
            Caja("Miga", new Vector3(0f, -0.05f, -0.03f), new Vector3(1.9f, 1.75f, 0.42f), 0.15f, miga, _pan);
            Esfera("Miga izquierda", new Vector3(-0.5f, 0.85f, -0.03f), new Vector3(1.05f, 0.8f, 0.42f), miga, _pan);
            Esfera("Miga derecha", new Vector3(0.5f, 0.85f, -0.03f), new Vector3(1.05f, 0.8f, 0.42f), miga, _pan);
            var brillo = Nodo("Brillo", _pan).gameObject.AddComponent<Light>();
            brillo.type = LightType.Point;
            brillo.color = Hex("#ffe2a8");
            brillo.intensity = 3f;
            brillo.range = 4f;
        }

        private void ArmarKartDeBlip()
        {
            var kart = Nodo("Kart de Blip", transform);
            kart.localPosition = new Vector3(0f, 0f, -2.5f);

            var naranja = Mat(Hex("#ff9f1c"), 0.55f);
            var ruedas = Mat(Hex("#1d2340"), 0.35f);
            var gris = Mat(Hex("#d5d9e2"), 0.5f);

            Caja("Chasis", new Vector3(0f, 0.6f, 0f), new Vector3(1.5f, 0.5f, 2.2f), 0.22f, naranja, kart);
            Caja("Trompa", new Vector3(0f, 0.5f, 1.2f), new Vector3(1.1f, 0.35f, 0.5f), 0.15f, naranja, kart);
            Caja("Paragolpes", new Vector3(0f, 0.42f, 1.5f), new Vector3(1.4f, 0.25f, 0.2f), 0.1f, gris, kart);
            Caja("Respaldo", new Vector3(0f, 1.05f, -0.75f), new Vector3(1f, 0.6f, 0.2f), 0.1f, Mat(Hex("#c86f00"), 0.4f), kart);
            Caja("Alerón", new Vector3(0f, 1.35f, -1.05f), new Vector3(1.7f, 0.1f, 0.4f), 0.05f, naranja, kart);
            foreach (var x in new[] { -0.85f, 0.85f })
            foreach (var z in new[] { -0.7f, 0.75f })
                Cilindro("Rueda", new Vector3(x, 0.34f, z), new Vector3(0.68f, 0.15f, 0.68f), Quaternion.Euler(0f, 0f, 90f), ruedas, kart);

            // Blip: bolita celeste de un ojo, con orejitas y antena de resorte con bolita naranja.
            var blip = Nodo("Blip", kart);
            blip.localPosition = new Vector3(0f, 1.3f, -0.2f);
            var celeste = Mat(Hex("#8fd3ff"), 0.5f);
            Esfera("Cuerpo", Vector3.zero, Vector3.one * 0.9f, celeste, blip);
            Esfera("Oreja izquierda", new Vector3(-0.45f, 0.1f, 0f), Vector3.one * 0.25f, celeste, blip);
            Esfera("Oreja derecha", new Vector3(0.45f, 0.1f, 0f), Vector3.one * 0.25f, celeste, blip);
            Esfera("Ojo", new Vector3(0f, 0.08f, 0.33f), Vector3.one * 0.42f, Mat(Color.white, 0.6f), blip);
            Esfera("Pupila", new Vector3(0f, 0.08f, 0.52f), Vector3.one * 0.2f, Mat(Hex("#1d2340"), 0.8f), blip);
            _antena = Nodo("Antena", blip);
            _antena.localPosition = new Vector3(0f, 0.42f, 0f);
            Cilindro("Resorte", new Vector3(0f, 0.25f, 0f), new Vector3(0.06f, 0.25f, 0.06f), Quaternion.identity, Mat(Hex("#3b4a8f"), 0.4f), _antena);
            Esfera("Bolita", new Vector3(0f, 0.55f, 0f), Vector3.one * 0.22f, Mat(Hex("#ff8a1c"), 0.5f), _antena);
        }

        private void ArmarLuces()
        {
            // Luz cálida de la casa y luz fría que entra por la ventana.
            Luz("Luz cálida izquierda", new Vector3(-5f, 7.5f, 1f), Hex("#ffd59a"), 6f, 16f);
            Luz("Luz cálida derecha", new Vector3(6f, 7.5f, -1f), Hex("#ffd59a"), 5f, 16f);
            Luz("Luz del espacio", new Vector3(0f, 5f, 9.5f), Hex("#8f9dff"), 4f, 12f);
        }

        // ------------------------------------------------------------------ piezas

        private Transform Nodo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(padre, false);
            _creados.Add(go);
            return go.transform;
        }

        private Transform Caja(string nombre, Vector3 posicion, Vector3 tamano, float radio, Material material, Transform padre = null)
        {
            var t = Nodo(nombre, padre != null ? padre : transform);
            t.localPosition = posicion;
            var malla = CajaRedondeada.Crear(tamano, radio);
            malla.hideFlags = HideFlags.DontSave;
            _creados.Add(malla);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return t;
        }

        private Transform Primitiva(PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 escala, Quaternion rotacion, Material material, Transform padre)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.hideFlags = HideFlags.DontSave;
            var collider = go.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);
            go.transform.SetParent(padre != null ? padre : transform, false);
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            _creados.Add(go);
            return go.transform;
        }

        private Transform Esfera(string nombre, Vector3 posicion, Vector3 escala, Material material, Transform padre = null)
        {
            return Primitiva(PrimitiveType.Sphere, nombre, posicion, escala, Quaternion.identity, material, padre);
        }

        private Transform Cilindro(string nombre, Vector3 posicion, Vector3 escala, Quaternion rotacion, Material material, Transform padre = null)
        {
            return Primitiva(PrimitiveType.Cylinder, nombre, posicion, escala, rotacion, material, padre);
        }

        private void Luz(string nombre, Vector3 posicion, Color color, float intensidad, float alcance)
        {
            var t = Nodo(nombre, transform);
            t.localPosition = posicion;
            var l = t.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.Soft;
        }

        // ------------------------------------------------------------------ materiales y texturas

        private Material Mat(Color color, float suavidad, Texture2D textura = null)
        {
            if (textura == null && _materiales.TryGetValue(color, out var existente)) return existente;
            var m = new Material(_shader) { hideFlags = HideFlags.DontSave, color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            if (textura != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", textura);
                m.mainTexture = textura;
            }
            else
            {
                _materiales[color] = m;
            }
            _creados.Add(m);
            return m;
        }

        private Material MaterialEmisivo(Color color, float fuerza, Texture2D textura = null)
        {
            var m = new Material(_shader) { hideFlags = HideFlags.DontSave, color = textura != null ? Color.white : color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", textura != null ? Color.white : color);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * fuerza);
            if (textura != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", textura);
                m.mainTexture = textura;
                m.SetTexture("_EmissionMap", textura);
            }
            _creados.Add(m);
            return m;
        }

        private Material MaterialEspacio()
        {
            return MaterialEmisivo(Color.white, 1.2f, TexturaEspacio());
        }

        private Texture2D NuevaTextura(int ancho, int alto)
        {
            var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Repeat };
            _creados.Add(t);
            return t;
        }

        private Texture2D TexturaGranito()
        {
            var t = NuevaTextura(256, 256);
            var rng = new System.Random(7);
            var px = new Color32[256 * 256];
            for (int i = 0; i < px.Length; i++)
            {
                float r = (float)rng.NextDouble();
                byte b = (byte)(r < 0.04f ? 120 : r < 0.12f ? 200 : 245);
                px[i] = new Color32(b, b, (byte)Mathf.Min(255, b + 6), 255);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        private Texture2D TexturaAzulejos()
        {
            var t = NuevaTextura(128, 128);
            var px = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                bool junta = x % 64 < 3 || y % 64 < 3;
                px[y * 128 + x] = junta ? new Color32(205, 192, 182, 255) : new Color32(255, 255, 255, 255);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        private Texture2D TexturaEspacio()
        {
            const int w = 512, h = 300;
            var t = NuevaTextura(w, h);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var arriba = Hex("#120c33");
            var abajo = Hex("#3a2475");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = Color.Lerp(abajo, arriba, y / (float)h);

            var rng = new System.Random(3);
            for (int i = 0; i < 700; i++)
            {
                int x = rng.Next(w), y = rng.Next(h);
                float brillo = 0.4f + (float)rng.NextDouble() * 0.6f;
                px[y * w + x] = Color.Lerp(px[y * w + x], Color.white, brillo);
            }
            Planeta(px, w, h, 130, 190, 46, Hex("#ff9f6b"));
            Planeta(px, w, h, 400, 80, 26, Hex("#3e8ef7"));
            Planeta(px, w, h, 300, 230, 12, Hex("#79f0c3"));
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        private static void Planeta(Color[] px, int w, int h, int cx, int cy, int r, Color color)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > r) continue;
                float luz = Mathf.Clamp01(1.1f - ((x - cx + r * 0.4f) * 0.5f + (cy - y + r * 0.4f) * 0.5f) / (r * 2f));
                px[y * w + x] = Color.Lerp(color * 0.55f, color, luz);
            }
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ------------------------------------------------------------------ limpieza

        private void Limpiar()
        {
            for (int i = _creados.Count - 1; i >= 0; i--)
            {
                var o = _creados[i];
                if (o == null) continue;
                if (Application.isPlaying) Destroy(o);
                else DestroyImmediate(o);
            }
            _creados.Clear();
            _materiales.Clear();
            _pan = null;
            _antena = null;
        }
    }
}
