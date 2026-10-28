using NUnit.Framework;
using UnityEngine;

namespace BichiKart.Pruebas
{
    /// <summary>
    /// La distancia entre dos karts se mide como entre dos cápsulas: la distancia más corta entre las líneas
    /// del medio de cada uno.
    /// </summary>
    public sealed class PartidaPruebas
    {
        [Test]
        public void SegmentosParalelosAlLado()
        {
            float d = Partida.DistanciaEntreSegmentos(new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), new Vector3(2f, 0f, -1f), new Vector3(2f, 0f, 1f));
            Assert.That(d, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void SegmentosEnFilaSeMidenDePuntaAPunta()
        {
            float d = Partida.DistanciaEntreSegmentos(new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 5f));
            Assert.That(d, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void SegmentosCruzadosSeTocan()
        {
            float d = Partida.DistanciaEntreSegmentos(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f));
            Assert.That(d, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SegmentosCruzadosAUnaAltura()
        {
            float d = Partida.DistanciaEntreSegmentos(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1.5f, -1f), new Vector3(0f, 1.5f, 1f));
            Assert.That(d, Is.EqualTo(1.5f).Within(1e-4f));
        }
    }
}
