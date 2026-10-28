using NUnit.Framework;
using UnityEngine;

namespace BichiKart.Pruebas
{
    /// <summary>
    /// Comprueba que las cajas redondeadas salgan cerradas hacia afuera y con las medidas pedidas, y que
    /// las caras sueltas de las salas miren hacia adentro.
    /// </summary>
    public sealed class CajaRedondeadaPruebas
    {
        private static readonly Vector3[] Tamanos = { new Vector3(2f, 1f, 3f), new Vector3(26f, 1f, 19f), new Vector3(0.4f, 0.4f, 0.4f) };
        private static readonly int[] Caras = { 0, 1, 2, 3, 4, 5 };

        [Test]
        public void LasMedidasSonLasPedidas([ValueSource(nameof(Tamanos))] Vector3 tamano)
        {
            var malla = CajaRedondeada.Crear(tamano, 0.3f);
            Assert.That(Vector3.Distance(malla.bounds.size, tamano), Is.LessThan(1e-3f));
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void ElRadioSeLimitaALaMitadDelLadoMasCorto()
        {
            var malla = CajaRedondeada.Crear(Vector3.one, 5f);
            Assert.That(Vector3.Distance(malla.bounds.size, Vector3.one), Is.LessThan(1e-3f));
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void LasNormalesMiranHaciaAfuera([ValueSource(nameof(Tamanos))] Vector3 tamano)
        {
            var malla = CajaRedondeada.Crear(tamano, 0.3f);
            ComprobarNormales(malla, 1f);
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void LosTriangulosMiranHaciaAfuera([ValueSource(nameof(Tamanos))] Vector3 tamano)
        {
            var malla = CajaRedondeada.Crear(tamano, 0.3f);
            ComprobarTriangulos(malla, 1f);
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void LasCarasDeUnaSalaMiranHaciaAdentro([ValueSource(nameof(Caras))] int cara)
        {
            var malla = CajaRedondeada.CrearCara(new Vector3(36f, 14f, 28f), 3.5f, cara, true);
            ComprobarNormales(malla, -1f);
            ComprobarTriangulos(malla, -1f);
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void LasSeisCarasJuntasSonLaCajaEntera()
        {
            var tamano = new Vector3(36f, 14f, 28f);
            var caja = CajaRedondeada.Crear(tamano, 3.5f);
            int vertices = 0;
            var limites = new Bounds();
            for (int cara = 0; cara < 6; cara++)
            {
                var malla = CajaRedondeada.CrearCara(tamano, 3.5f, cara, true);
                vertices += malla.vertexCount;
                if (cara == 0) limites = malla.bounds;
                else limites.Encapsulate(malla.bounds);
                Object.DestroyImmediate(malla);
            }
            Assert.That(vertices, Is.EqualTo(caja.vertexCount));
            Assert.That(Vector3.Distance(limites.size, tamano), Is.LessThan(1e-3f));
            Object.DestroyImmediate(caja);
        }

        [Test]
        public void LaTexturaSigueLaPosicionSobreLaCara([ValueSource(nameof(Caras))] int cara)
        {
            // En la parte plana de la cara, la coordenada de textura tiene que avanzar pareja con la posición.
            var tamano = new Vector3(40f, 14f, 32f);
            var malla = CajaRedondeada.CrearCara(tamano, 3.5f, cara, true);
            Vector3 u = CajaRedondeada.EjeU(cara), v = CajaRedondeada.EjeV(cara);
            Vector3 haciaAfuera = Vector3.Cross(u, v);
            float mitadU = Mathf.Abs(Vector3.Dot(u, tamano)) * 0.5f, mitadV = Mathf.Abs(Vector3.Dot(v, tamano)) * 0.5f;
            var vertices = malla.vertices;
            var normales = malla.normals;
            var uvs = malla.uv;
            int planos = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Vector3.Dot(normales[i], haciaAfuera) > -0.9999f) continue;
                planos++;
                Assert.That(uvs[i].x, Is.EqualTo((Vector3.Dot(vertices[i], u) + mitadU) / (2f * mitadU)).Within(1e-4f), $"U del vértice {i}");
                Assert.That(uvs[i].y, Is.EqualTo((Vector3.Dot(vertices[i], v) + mitadV) / (2f * mitadV)).Within(1e-4f), $"V del vértice {i}");
            }
            Assert.That(planos, Is.GreaterThan(0));
            Object.DestroyImmediate(malla);
        }

        /// <param name="sentido">1 si las normales tienen que mirar hacia afuera, -1 si hacia adentro.</param>
        private static void ComprobarNormales(Mesh malla, float sentido)
        {
            var vertices = malla.vertices;
            var normales = malla.normals;
            for (int i = 0; i < vertices.Length; i++)
                Assert.That(sentido * Vector3.Dot(normales[i], vertices[i]), Is.GreaterThan(0f), $"Vértice {i}");
        }

        /// <param name="sentido">1 si los triángulos tienen que mirar hacia afuera, -1 si hacia adentro.</param>
        private static void ComprobarTriangulos(Mesh malla, float sentido)
        {
            var vertices = malla.vertices;
            var triangulos = malla.triangles;
            for (int i = 0; i < triangulos.Length; i += 3)
            {
                Vector3 a = vertices[triangulos[i]], b = vertices[triangulos[i + 1]], c = vertices[triangulos[i + 2]];
                Vector3 cara = Vector3.Cross(b - a, c - a);
                if (cara.sqrMagnitude < 1e-12f) continue;
                Assert.That(sentido * Vector3.Dot(cara, (a + b + c) / 3f), Is.GreaterThan(0f), $"Triángulo {i / 3}");
            }
        }
    }
}
