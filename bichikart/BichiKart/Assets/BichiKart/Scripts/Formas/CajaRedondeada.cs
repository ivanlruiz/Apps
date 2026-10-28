using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Arma una malla de caja con las esquinas y los bordes redondeados, para lograr el estilo
    /// "caricatura redondita" sin modelos externos. También arma cada cara por separado y mirando hacia
    /// adentro, para las salas cerradas.
    /// </summary>
    public static class CajaRedondeada
    {
        /// <summary>Índices de las caras, en el orden en que se arman.</summary>
        public const int Derecha = 0, Izquierda = 1, Arriba = 2, Abajo = 3, Adelante = 4, Atras = 5;

        private static readonly Vector3[] Normales = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        private static readonly Vector3[] EjesU = { Vector3.up, Vector3.forward, Vector3.forward, Vector3.right, Vector3.right, Vector3.up };
        private static readonly Vector3[] EjesV = { Vector3.forward, Vector3.up, Vector3.right, Vector3.forward, Vector3.up, Vector3.right };

        /// <param name="tamano">Medidas totales de la caja.</param>
        /// <param name="radio">Radio de las esquinas (se limita a la mitad del lado más corto).</param>
        /// <param name="segmentos">Cuántas divisiones tiene cada esquina; más es más suave.</param>
        public static Mesh Crear(Vector3 tamano, float radio, int segmentos = 6)
        {
            var datos = new Datos();
            for (int cara = 0; cara < 6; cara++) AgregarCara(datos, tamano, radio, segmentos, cara, false);
            return datos.Armar("Caja redondeada");
        }

        /// <summary>
        /// Arma una sola cara de la caja, con la mitad de cada borde redondeado que la toca. Las seis caras
        /// juntas forman la caja entera. Hacia adentro, las normales y los triángulos miran al centro.
        /// </summary>
        public static Mesh CrearCara(Vector3 tamano, float radio, int cara, bool haciaAdentro, int segmentos = 6)
        {
            var datos = new Datos();
            AgregarCara(datos, tamano, radio, segmentos, cara, haciaAdentro);
            return datos.Armar("Cara redondeada");
        }

        /// <summary>Eje del ancho de la textura de una cara (el de su coordenada U).</summary>
        public static Vector3 EjeU(int cara) => EjesU[cara];

        /// <summary>Eje del alto de la textura de una cara (el de su coordenada V).</summary>
        public static Vector3 EjeV(int cara) => EjesV[cara];

        private static void AgregarCara(Datos datos, Vector3 tamano, float radio, int segmentos, int cara, bool haciaAdentro)
        {
            radio = Mathf.Min(radio, Mathf.Min(tamano.x, Mathf.Min(tamano.y, tamano.z)) * 0.5f);
            Vector3 mitad = tamano * 0.5f;
            Vector3 interior = mitad - Vector3.one * radio;

            Vector3 normal = Normales[cara], u = EjesU[cara], v = EjesV[cara];
            float mitadU = Componente(u, mitad), mitadV = Componente(v, mitad);
            var muestrasU = Muestras(mitadU, Componente(u, interior), segmentos);
            var muestrasV = Muestras(mitadV, Componente(v, interior), segmentos);
            float altura = Componente(normal, mitad);
            int inicio = datos.Vertices.Count;

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
                    datos.Vertices.Add(nucleo + direccion * radio);
                    datos.Normales.Add(haciaAdentro ? -direccion : direccion);
                    // La textura sigue la posición sobre la cara, así no se amontona en los bordes redondeados.
                    datos.Uvs.Add(new Vector2((muestrasU[i] + mitadU) / (2f * mitadU), (muestrasV[j] + mitadV) / (2f * mitadV)));
                }
            }

            int filas = muestrasV.Count;
            for (int i = 0; i < muestrasU.Count - 1; i++)
            {
                for (int j = 0; j < filas - 1; j++)
                {
                    int p = inicio + i * filas + j;
                    int q = p + filas;
                    if (haciaAdentro)
                    {
                        datos.Triangulos.Add(p); datos.Triangulos.Add(p + 1); datos.Triangulos.Add(q);
                        datos.Triangulos.Add(q); datos.Triangulos.Add(p + 1); datos.Triangulos.Add(q + 1);
                    }
                    else
                    {
                        datos.Triangulos.Add(p); datos.Triangulos.Add(q); datos.Triangulos.Add(p + 1);
                        datos.Triangulos.Add(q); datos.Triangulos.Add(q + 1); datos.Triangulos.Add(p + 1);
                    }
                }
            }
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

        private sealed class Datos
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normales = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangulos = new List<int>();

            public Mesh Armar(string nombre)
            {
                var malla = new Mesh { name = nombre };
                if (Vertices.Count > 65000) malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                malla.SetVertices(Vertices);
                malla.SetNormals(Normales);
                malla.SetUVs(0, Uvs);
                malla.SetTriangles(Triangulos, 0);
                malla.RecalculateBounds();
                malla.RecalculateTangents();
                return malla;
            }
        }
    }
}
