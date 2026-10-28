using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Después de usar la gravedad, mientras el kart vuela hacia la superficie de enfrente, muestra un
    /// aro del color del bichito donde va a caer. Así se puede corregir en el aire y los demás ven dónde
    /// va a aterrizar. Se arma al jugar y no se guarda en la escena.
    /// </summary>
    [RequireComponent(typeof(Kart))]
    public sealed class MarcaDeCaida : MonoBehaviour
    {
        [Tooltip("Radio del aro, de afuera.")]
        [SerializeField] private float radio = 1.1f;

        private Kart _kart;
        private Formas _formas;
        private Transform _marca;

        private void Start()
        {
            _kart = GetComponent<Kart>();
            var participante = GetComponent<Participante>();
            var color = participante != null ? participante.Color : Color.white;

            _formas = new Formas();
            _marca = _formas.Nodo($"Marca de caída de {name}", null);
            var brillo = _formas.Emisivo(color, 1.2f);
            Formas.SinSombra(_formas.Malla("Aro", _marca, CrearAro(radio * 0.72f, radio, 40), brillo));
            Formas.SinSombra(_formas.Malla("Punto", _marca, CrearAro(0f, radio * 0.22f, 20), brillo));
            _marca.gameObject.SetActive(false);
        }

        private void OnDestroy() => _formas?.Limpiar();

        private void LateUpdate()
        {
            if (_marca == null) return;
            Vector3 punto = Vector3.zero, normal = Vector3.up;
            bool mostrar = _kart.CayendoPorGravedad && _kart.BuscarDondeCae(out punto, out normal);
            _marca.gameObject.SetActive(mostrar);
            if (!mostrar) return;

            // Apenas despegada de la superficie, para que no parpadee contra el piso. Late como un corazón.
            _marca.SetPositionAndRotation(punto + normal * 0.04f, Quaternion.FromToRotation(Vector3.up, normal));
            _marca.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(Time.time * 10f));
        }

        /// <summary>Un aro plano mirando hacia arriba (con radio de adentro 0, un círculo).</summary>
        private static Mesh CrearAro(float adentro, float afuera, int lados)
        {
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            for (int i = 0; i <= lados; i++)
            {
                float angulo = i * Mathf.PI * 2f / lados;
                var direccion = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                vertices.Add(direccion * adentro);
                vertices.Add(direccion * afuera);
            }
            for (int i = 0; i < lados; i++)
            {
                int a = i * 2, b = a + 1, c = a + 3, d = a + 2;
                triangulos.Add(a); triangulos.Add(d); triangulos.Add(b);
                triangulos.Add(b); triangulos.Add(d); triangulos.Add(c);
            }
            var malla = new Mesh { name = "Aro" };
            malla.SetVertices(vertices);
            var normales = new Vector3[vertices.Count];
            for (int i = 0; i < normales.Length; i++) normales[i] = Vector3.up;
            malla.normals = normales;
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            return malla;
        }
    }
}
