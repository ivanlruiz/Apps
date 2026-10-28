using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BichiKart
{
    /// <summary>
    /// Arma objetos con formas simples (cajas redondeadas, esferas y cilindros) y sus materiales, para el
    /// prototipo. Todo lo que crea queda anotado para borrarlo con <see cref="Limpiar"/> y no se guarda en
    /// la escena: lo vuelve a armar el componente que lo usa cada vez que se activa.
    /// </summary>
    public sealed class Formas
    {
        /// <summary>
        /// Materiales de muestra guardados en Resources. Se copian en vez de buscar el shader por nombre,
        /// porque en el juego compilado solo vienen los shaders que usa algún material guardado.
        /// </summary>
        private const string RutaBase = "Formas/Base", RutaEmisivo = "Formas/Emisivo";

        private readonly List<Object> _creados = new List<Object>();
        private readonly Dictionary<(Color, float), Material> _materiales = new Dictionary<(Color, float), Material>();
        private readonly Material _base, _emisivo;

        public Formas()
        {
            _base = Resources.Load<Material>(RutaBase);
            _emisivo = Resources.Load<Material>(RutaEmisivo);
            if (_base == null)
            {
                _base = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
                _creados.Add(_base);
            }
            if (_emisivo == null) _emisivo = _base;
        }

        // ------------------------------------------------------------------ objetos

        public Transform Nodo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(padre, false);
            if (padre != null) go.layer = padre.gameObject.layer;
            _creados.Add(go);
            return go.transform;
        }

        public Transform Malla(string nombre, Transform padre, Mesh malla, Material material)
        {
            var t = Nodo(nombre, padre);
            malla.hideFlags = HideFlags.DontSave;
            _creados.Add(malla);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return t;
        }

        public Transform Caja(string nombre, Transform padre, Vector3 posicion, Vector3 tamano, float radio, Material material)
        {
            var t = Malla(nombre, padre, CajaRedondeada.Crear(tamano, radio), material);
            t.localPosition = posicion;
            return t;
        }

        public Transform Esfera(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
        {
            return Primitiva(PrimitiveType.Sphere, nombre, padre, posicion, escala, Quaternion.identity, material);
        }

        public Transform Cilindro(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Quaternion rotacion, Material material)
        {
            return Primitiva(PrimitiveType.Cylinder, nombre, padre, posicion, escala, rotacion, material);
        }

        public Light Luz(string nombre, Transform padre, Vector3 posicion, Color color, float intensidad, float alcance, bool sombras)
        {
            var t = Nodo(nombre, padre);
            t.localPosition = posicion;
            var luz = t.gameObject.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            luz.shadows = sombras ? LightShadows.Soft : LightShadows.None;
            return luz;
        }

        private Transform Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala, Quaternion rotacion, Material material)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.hideFlags = HideFlags.DontSave;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            go.transform.SetParent(padre, false);
            if (padre != null) go.layer = padre.gameObject.layer;
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            _creados.Add(go);
            return go.transform;
        }

        // ------------------------------------------------------------------ materiales y texturas

        /// <summary>Material de plástico liso. Sin textura, los del mismo color y brillo se comparten.</summary>
        public Material Mat(Color color, float suavidad, Texture2D textura = null)
        {
            if (textura == null && _materiales.TryGetValue((color, suavidad), out var existente)) return existente;
            var m = new Material(_base) { hideFlags = HideFlags.DontSave, color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            if (textura != null)
            {
                Registrar(textura);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", textura);
                m.mainTexture = textura;
            }
            else
            {
                _materiales[(color, suavidad)] = m;
            }
            _creados.Add(m);
            return m;
        }

        /// <summary>Material que brilla con luz propia (lucecitas, la ventana al espacio).</summary>
        public Material Emisivo(Color color, float fuerza, Texture2D textura = null)
        {
            var m = new Material(_emisivo) { hideFlags = HideFlags.DontSave, color = textura != null ? Color.white : color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", textura != null ? Color.white : color);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * fuerza);
            if (textura != null)
            {
                Registrar(textura);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", textura);
                m.mainTexture = textura;
                m.SetTexture("_EmissionMap", textura);
            }
            _creados.Add(m);
            return m;
        }

        /// <summary>Repite la textura de un material (por ejemplo, para que los azulejos tengan su tamaño real).</summary>
        public static void Repetir(Material material, Vector2 veces)
        {
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", veces);
            material.mainTextureScale = veces;
        }

        private void Registrar(Texture2D textura)
        {
            textura.hideFlags = HideFlags.DontSave;
            if (!_creados.Contains(textura)) _creados.Add(textura);
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// <summary>Que no proyecte sombras (para cosas que brillan o que están pegadas a una pared).</summary>
        public static void SinSombra(Transform t)
        {
            var r = t.GetComponent<Renderer>();
            if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ limpieza

        public void Limpiar()
        {
            for (int i = _creados.Count - 1; i >= 0; i--)
            {
                var o = _creados[i];
                if (o == null) continue;
                if (Application.isPlaying) Object.Destroy(o);
                else Object.DestroyImmediate(o);
            }
            _creados.Clear();
            _materiales.Clear();
        }
    }
}
