using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BichiKart.Pruebas
{
    /// <summary>
    /// Las reglas de la comida: agarrar, aguantar, robar, la protección, el doble toque, las rondas y la
    /// muerte súbita.
    /// </summary>
    public sealed class ArbitroPruebas
    {
        private const float Paso = 0.02f;

        private ReglasDePartida _reglas;
        private Arbitro _arbitro;
        private List<EventoDePartida> _eventos;

        [SetUp]
        public void Preparar()
        {
            _reglas = ScriptableObject.CreateInstance<ReglasDePartida>();
            _reglas.tiempoCuatroJugadores = 20f;
            _reglas.rondasParaGanar = 2;
            _reglas.proteccionTrasRobo = 1f;
            _reglas.ventanaDobleToque = 0.1f;
            _reglas.muerteSubitaTras = 180f;
            _reglas.tiempoMuerteSubita = 5f;
            _reglas.cuentaRegresiva = 3f;
            _reglas.pausaEntreRondas = 3f;
            _reglas.pausaFinDePartida = 6f;
            _arbitro = new Arbitro(_reglas, 4);
            _eventos = new List<EventoDePartida>();
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(_reglas);

        [Test]
        public void EmpiezaConCuentaRegresivaYDespuesArranca()
        {
            Assert.That(_arbitro.Estado, Is.EqualTo(EstadoDePartida.CuentaRegresiva));
            Avanzar(3.1f);
            var cuentas = _eventos.Where(e => e.Tipo == TipoDeEvento.Cuenta).Select(e => e.Numero);
            Assert.That(cuentas, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.Arranca));
            Assert.That(_arbitro.Estado, Is.EqualTo(EstadoDePartida.Jugando));
        }

        [Test]
        public void DuranteLaCuentaRegresivaNoSePuedeAgarrar()
        {
            _arbitro.ReportarToqueComida(0);
            Avanzar(Paso);
            Assert.That(_arbitro.Duenio, Is.EqualTo(Arbitro.Nadie));
        }

        [Test]
        public void ElQueTocaLaComidaSueltaLaAgarra()
        {
            Arrancar();
            _arbitro.ReportarToqueComida(2);
            Avanzar(Paso);
            Assert.That(_arbitro.Duenio, Is.EqualTo(2));
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.Agarro && e.Kart == 2));
        }

        [Test]
        public void AguantarElTiempoGanaLaRonda()
        {
            Arrancar();
            Agarrar(0);
            Avanzar(20.1f);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.GanoRonda && e.Kart == 0));
            Assert.That(_arbitro.Victorias(0), Is.EqualTo(1));
            Assert.That(_arbitro.Estado, Is.EqualTo(EstadoDePartida.FinDeRonda));
        }

        [Test]
        public void UnToqueRobaLaComidaYElTiempoVuelveACero()
        {
            Arrancar();
            Agarrar(0);
            Avanzar(5f);
            _arbitro.ReportarToqueEntreKarts(1, 0);
            Avanzar(0.12f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(1));
            Assert.That(_arbitro.TiempoAguantado, Is.LessThan(0.1f));
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.Robo && e.Kart == 1 && e.Otro == 0));
        }

        [Test]
        public void ElLadronQuedaProtegidoUnSegundito()
        {
            Arrancar();
            Agarrar(0);
            _arbitro.ReportarToqueEntreKarts(1, 0);
            Avanzar(0.12f);

            _arbitro.ReportarToqueEntreKarts(0, 1);
            Avanzar(0.3f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(1), "Protegido, no se la pueden robar");

            Avanzar(1f);
            _arbitro.ReportarToqueEntreKarts(0, 1);
            Avanzar(0.12f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(0), "Sin protección, sí");
        }

        [Test]
        public void SiDosLoTocanALaVezLaComidaSeCae()
        {
            Arrancar();
            Agarrar(0);
            _arbitro.ReportarToqueEntreKarts(1, 0);
            _arbitro.ReportarToqueEntreKarts(0, 2);
            Avanzar(0.12f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(Arbitro.Nadie));
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.Cayo && e.Kart == 0));
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.Robo), Is.False);
        }

        [Test]
        public void DosToquesSeguidosDentroDeLaVentanaTambienLaTiran()
        {
            Arrancar();
            Agarrar(0);
            _arbitro.ReportarToqueEntreKarts(1, 0);
            Avanzar(0.04f);
            _arbitro.ReportarToqueEntreKarts(0, 2);
            Avanzar(0.1f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(Arbitro.Nadie));
        }

        [Test]
        public void LosPerseguidoresNoSeRobanEntreEllos()
        {
            Arrancar();
            Agarrar(0);
            _arbitro.ReportarToqueEntreKarts(1, 2);
            Avanzar(0.2f);
            Assert.That(_arbitro.Duenio, Is.EqualTo(0));
            Assert.That(_arbitro.SonPerseguidores(1, 2));
            Assert.That(_arbitro.SonPerseguidores(0, 2), Is.False);
        }

        [Test]
        public void ElDuenioVaMasLentoYLosDemasMasRapido()
        {
            Arrancar();
            Assert.That(_arbitro.MultiplicadorVelocidad(1), Is.EqualTo(1f));
            Agarrar(0);
            Assert.That(_arbitro.MultiplicadorVelocidad(0), Is.EqualTo(_reglas.velocidadDuenio));
            Assert.That(_arbitro.MultiplicadorVelocidad(1), Is.EqualTo(_reglas.velocidadPerseguidores));
        }

        [Test]
        public void SeJuegaAlMejorDeTresYDespuesEmpiezaOtraPartida()
        {
            Arrancar();
            Agarrar(0);
            Avanzar(20.1f);
            Avanzar(3.1f);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.NuevaRonda && e.Numero == 2));
            Assert.That(_arbitro.Ronda, Is.EqualTo(2));

            Arrancar();
            Agarrar(0);
            Avanzar(20.1f);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.GanoPartida && e.Kart == 0));
            Assert.That(_arbitro.Estado, Is.EqualTo(EstadoDePartida.FinDePartida));

            _eventos.Clear();
            Avanzar(6.1f);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.NuevaRonda && e.Numero == 1));
            Assert.That(_arbitro.Victorias(0), Is.EqualTo(0));
        }

        [Test]
        public void ALosTresMinutosEmpiezaLaMuerteSubita()
        {
            Arrancar();
            Agarrar(0);
            // Le roban cada tanto para que nadie gane.
            for (int i = 0; i < 18; i++)
            {
                Avanzar(9.9f);
                _arbitro.ReportarToqueEntreKarts(i % 2 == 0 ? 1 : 0, _arbitro.Duenio);
                Avanzar(0.12f);
            }
            Assert.That(_arbitro.MuerteSubita, Is.True);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.MuerteSubita));
            Assert.That(_arbitro.TiempoParaGanar, Is.EqualTo(5f));

            int duenio = _arbitro.Duenio;
            Avanzar(5.1f);
            Assert.That(_eventos.Any(e => e.Tipo == TipoDeEvento.GanoRonda && e.Kart == duenio));
        }

        // ------------------------------------------------------------------ ayudas

        private void Avanzar(float segundos)
        {
            for (float t = 0f; t < segundos - 1e-4f; t += Paso)
            {
                _arbitro.Avanzar(Paso);
                _eventos.AddRange(_arbitro.Eventos);
                _arbitro.LimpiarEventos();
            }
        }

        private void Arrancar()
        {
            while (_arbitro.Estado != EstadoDePartida.Jugando) Avanzar(Paso);
        }

        private void Agarrar(int kart)
        {
            _arbitro.ReportarToqueComida(kart);
            Avanzar(Paso);
            Assert.That(_arbitro.Duenio, Is.EqualTo(kart));
        }
    }
}
