using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>Una rosca (como un volante), acostada: el agujero mira hacia arriba (Y).</summary>
        public Transform Rosca(string nombre, Transform padre, Vector3 posicion, float radio, float grosor, Quaternion rotacion, Material material)
        {
            const int vueltas = 32, lados = 12;
            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var triangulos = new List<int>();
            for (int i = 0; i <= vueltas; i++)
            {
                float a = i * Mathf.PI * 2f / vueltas;
                var afuera = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int j = 0; j <= lados; j++)
                {
                    float b = j * Mathf.PI * 2f / lados;
                    var normal = afuera * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    vertices.Add(afuera * radio + normal * grosor);
                    normales.Add(normal);
                }
            }
            int fila = lados + 1;
            for (int i = 0; i < vueltas; i++)
            for (int j = 0; j < lados; j++)
            {
                int p = i * fila + j, q = p + fila;
                triangulos.Add(p); triangulos.Add(p + 1); triangulos.Add(q);
                triangulos.Add(q); triangulos.Add(p + 1); triangulos.Add(q + 1);
            }
            var malla = new Mesh { name = nombre };
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            var t = Malla(nombre, padre, malla, material);
            t.localPosition = posicion;
            t.localRotation = rotacion;
            return t;
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

        /// <summary>
        /// Copia un modelo hecho en Blender (por ejemplo, un bichito de Resources) y le cambia los materiales
        /// por los de acá. El nombre de cada material de Blender trae "#color|aspereza|brillo|laca"; así el
        /// modelo queda igual que lo demás (y se puede desvanecer como el resto del kart).
        /// </summary>
        public Transform Modelo(string nombre, Transform padre, GameObject molde)
        {
            var copia = Object.Instantiate(molde, padre, false);
            copia.name = nombre;
            foreach (var t in copia.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.hideFlags = HideFlags.DontSave;
                if (padre != null) t.gameObject.layer = padre.gameObject.layer;
            }
            foreach (var renderer in copia.GetComponentsInChildren<Renderer>(true))
            {
                var originales = renderer.sharedMaterials;
                var nuevos = new Material[originales.Length];
                for (int i = 0; i < originales.Length; i++) nuevos[i] = SegunNombre(originales[i]);
                renderer.sharedMaterials = nuevos;
            }
            _creados.Add(copia);
            return copia.transform;
        }

        /// <summary>
        /// "#6fbcff|0.40|0.0|0.0": color, aspereza (lo contrario de la suavidad), brillo propio y laca. Al
        /// exportar a FBX, Blender cambia las "|" por "_", así que valen las dos.
        /// </summary>
        private Material SegunNombre(Material original)
        {
            if (original == null) return Mat(Color.white, 0.5f);
            var partes = original.name.Split('|', '_');
            float Numero(int i, float siFalta) =>
                partes.Length > i && float.TryParse(partes[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : siFalta;

            var color = Hex(partes[0].Trim());
            float brillo = Numero(2, 0f);
            if (brillo > 0f) return Emisivo(color, brillo);
            return Mat(color, Mathf.Max(1f - Numero(1, 0.5f), Numero(3, 0f) * 0.85f));
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

        /// <summary>
        /// Qué tan visible es un material, de 0 (invisible) a 1 (opaco). Por debajo de 1 lo pasa a
        /// transparente (shader Lit de URP); en 1 lo vuelve a opaco, que se dibuja mejor. En Resources hay
        /// materiales transparentes de muestra para que el juego compilado traiga esa versión del shader.
        /// </summary>
        public static void PonerAlfa(Material material, float alfa)
        {
            bool transparente = alfa < 0.999f;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", transparente ? 1f : 0f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", transparente ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
                material.SetFloat("_DstBlend", transparente ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", transparente ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
                material.SetFloat("_ZWrite", transparente ? 0f : 1f);
                material.SetOverrideTag("RenderType", transparente ? "Transparent" : "Opaque");
                material.renderQueue = transparente ? (int)RenderQueue.Transparent : -1;
                if (transparente) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            var propiedad = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            var color = material.GetColor(propiedad);
            color.a = Mathf.Clamp01(alfa);
            material.SetColor(propiedad, color);
        }

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
