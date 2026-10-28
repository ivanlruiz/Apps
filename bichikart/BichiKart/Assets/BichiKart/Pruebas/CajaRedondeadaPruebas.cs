using BichiKart.Prueba;
using NUnit.Framework;
using UnityEngine;

namespace BichiKart.Pruebas
{
    /// <summary>
    /// Comprueba que las cajas redondeadas salgan cerradas hacia afuera y con las medidas pedidas.
    /// </summary>
    public sealed class CajaRedondeadaPruebas
    {
        private static readonly Vector3[] Tamanos = { new Vector3(2f, 1f, 3f), new Vector3(26f, 1f, 19f), new Vector3(0.4f, 0.4f, 0.4f) };

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
            var vertices = malla.vertices;
            var normales = malla.normals;
            for (int i = 0; i < vertices.Length; i++)
                Assert.That(Vector3.Dot(normales[i], vertices[i]), Is.GreaterThan(0f), $"Vértice {i}");
            Object.DestroyImmediate(malla);
        }

        [Test]
        public void LosTriangulosMiranHaciaAfuera([ValueSource(nameof(Tamanos))] Vector3 tamano)
        {
            var malla = CajaRedondeada.Crear(tamano, 0.3f);
            var vertices = malla.vertices;
            var triangulos = malla.triangles;
            for (int i = 0; i < triangulos.Length; i += 3)
            {
                Vector3 a = vertices[triangulos[i]], b = vertices[triangulos[i + 1]], c = vertices[triangulos[i + 2]];
                Vector3 cara = Vector3.Cross(b - a, c - a);
                if (cara.sqrMagnitude < 1e-12f) continue;
                Assert.That(Vector3.Dot(cara, (a + b + c) / 3f), Is.GreaterThan(0f), $"Triángulo {i / 3}");
            }
            Object.DestroyImmediate(malla);
        }
    }
}
