using System.Collections.Generic;
using UnityEngine;

namespace BichiKart.Prueba
{
    /// <summary>
    /// Arma una malla de caja con las esquinas y los bordes redondeados, para lograr el estilo
    /// "caricatura redondita" sin modelos externos.
    /// </summary>
    public static class CajaRedondeada
    {
        private static readonly Vector3[] Normales = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        private static readonly Vector3[] EjesU = { Vector3.up, Vector3.forward, Vector3.forward, Vector3.right, Vector3.right, Vector3.up };
        private static readonly Vector3[] EjesV = { Vector3.forward, Vector3.up, Vector3.right, Vector3.forward, Vector3.up, Vector3.right };

        /// <param name="tamano">Medidas totales de la caja.</param>
        /// <param name="radio">Radio de las esquinas (se limita a la mitad del lado más corto).</param>
        /// <param name="segmentos">Cuántas divisiones tiene cada esquina; más es más suave.</param>
        public static Mesh Crear(Vector3 tamano, float radio, int segmentos = 6)
        {
            radio = Mathf.Min(radio, Mathf.Min(tamano.x, Mathf.Min(tamano.y, tamano.z)) * 0.5f);
            Vector3 mitad = tamano * 0.5f;
            Vector3 interior = mitad - Vector3.one * radio;

            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangulos = new List<int>();

            for (int cara = 0; cara < 6; cara++)
            {
                Vector3 normal = Normales[cara], u = EjesU[cara], v = EjesV[cara];
                var muestrasU = Muestras(Componente(u, mitad), Componente(u, interior), segmentos);
                var muestrasV = Muestras(Componente(v, mitad), Componente(v, interior), segmentos);
                float altura = Componente(normal, mitad);
                int inicio = vertices.Count;

                for (int i = 0; i < muestrasU.Count; i++)
                {
                    for (int j = 0; j < muestrasV.Count; j++)
                    {
                        Vector3 punto = normal * altura + u * muestrasU[i] + v * muestrasV[j];
                        Vector3 nucleo = new Vector3(
                            Mathf.Clamp(punto.x, -interior.x, interior.x),
                            Mathf.Clamp(punto.y, -interior.y, interior.y),
                            Mathf.Clamp(punto.z, -interior.z, interior.z));
                        Vector3 direccion = punto - nucleo;
                        direccion = direccion.sqrMagnitude > 1e-10f ? direccion.normalized : normal;
                        vertices.Add(nucleo + direccion * radio);
                        normales.Add(direccion);
                        uvs.Add(new Vector2(i / (float)(muestrasU.Count - 1), j / (float)(muestrasV.Count - 1)));
                    }
                }

                int filas = muestrasV.Count;
                for (int i = 0; i < muestrasU.Count - 1; i++)
                {
                    for (int j = 0; j < filas - 1; j++)
                    {
                        int p = inicio + i * filas + j;
                        int q = p + filas;
                        triangulos.Add(p); triangulos.Add(q); triangulos.Add(p + 1);
                        triangulos.Add(q); triangulos.Add(q + 1); triangulos.Add(p + 1);
                    }
                }
            }

            var malla = new Mesh { name = "Caja redondeada" };
            if (vertices.Count > 65000) malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetUVs(0, uvs);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            malla.RecalculateTangents();
            return malla;
        }

        private static float Componente(Vector3 eje, Vector3 medidas)
        {
            return Mathf.Abs(eje.x) * medidas.x + Mathf.Abs(eje.y) * medidas.y + Mathf.Abs(eje.z) * medidas.z;
        }

        /// <summary>Posiciones a lo largo de un lado: varias en cada borde redondeado y pocas en el tramo recto.</summary>
        private static List<float> Muestras(float mitad, float interior, int segmentos)
        {
            var lista = new List<float>();
            float curva = mitad - interior;
            for (int k = 0; k <= segmentos; k++) lista.Add(-mitad + curva * k / segmentos);
            if (interior > 1e-5f)
            {
                for (int k = 0; k <= segmentos; k++) lista.Add(interior + curva * k / segmentos);
            }
            else
            {
                for (int k = 1; k <= segmentos; k++) lista.Add(curva * k / segmentos);
            }
            return lista;
        }
    }
}
