using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    public enum EstadoDePartida
    {
        CuentaRegresiva,
        Jugando,
        FinDeRonda,
        FinDePartida,
    }

    public enum TipoDeEvento
    {
        /// <summary>Número de la cuenta regresiva (3, 2, 1).</summary>
        Cuenta,
        Arranca,
        Agarro,
        /// <summary><see cref="EventoDePartida.Kart"/> le robó a <see cref="EventoDePartida.Otro"/>.</summary>
        Robo,
        /// <summary>A <see cref="EventoDePartida.Kart"/> se le cayó la comida.</summary>
        Cayo,
        MuerteSubita,
        GanoRonda,
        GanoPartida,
        /// <summary>Empieza otra ronda (o una partida nueva si <see cref="EventoDePartida.Numero"/> es 1).</summary>
        NuevaRonda,
    }

    public readonly struct EventoDePartida
    {
        public readonly TipoDeEvento Tipo;
        public readonly int Kart;
        public readonly int Otro;
        public readonly int Numero;

        public EventoDePartida(TipoDeEvento tipo, int kart = Arbitro.Nadie, int otro = Arbitro.Nadie, int numero = 0)
        {
            Tipo = tipo;
            Kart = kart;
            Otro = otro;
            Numero = numero;
        }

        public override string ToString() => $"{Tipo} kart={Kart} otro={Otro} n={Numero}";
    }

    /// <summary>
    /// Las reglas de la comida, en un solo lugar: agarrar, aguantar, robar con un toque, la protección, la
    /// caída por doble toque, las rondas y la muerte súbita. No sabe nada de la escena: la escena le cuenta
    /// los toques y lo hace avanzar a paso fijo, y él anota lo que pasó en <see cref="Eventos"/>. Así, más
    /// adelante, el mismo árbitro puede correr en el que hace de servidor de una partida online.
    /// </summary>
    public sealed class Arbitro
    {
        public const int Nadie = -1;

        private readonly ReglasDePartida _reglas;
        private readonly int[] _victorias;
        private readonly List<EventoDePartida> _eventos = new List<EventoDePartida>();
        private readonly List<int> _toquesComida = new List<int>();
        private readonly List<(int, int)> _toquesKarts = new List<(int, int)>();
        private readonly List<int> _quienesTocaron = new List<int>();
        private float _ventana = -1f;
        private int _ultimaCuenta;
        private int _seLeCayo = Nadie;
        private float _esperaTrasCaida;

        public Arbitro(ReglasDePartida reglas, int cantidadDeKarts)
        {
            _reglas = reglas;
            CantidadDeKarts = cantidadDeKarts;
            _victorias = new int[cantidadDeKarts];
            Ronda = 1;
            EmpezarRonda();
        }

        public int CantidadDeKarts { get; }
        public EstadoDePartida Estado { get; private set; }
        public int Ronda { get; private set; }

        /// <summary>Quién tiene la comida (<see cref="Nadie"/> si está suelta).</summary>
        public int Duenio { get; private set; } = Nadie;

        /// <summary>Segundos seguidos que el dueño lleva aguantando la comida.</summary>
        public float TiempoAguantado { get; private set; }

        /// <summary>Segundos que lleva la ronda jugándose.</summary>
        public float TiempoDeRonda { get; private set; }

        /// <summary>Segundos que quedan de la cuenta regresiva o de la pausa entre rondas.</summary>
        public float TiempoDeEspera { get; private set; }

        /// <summary>Segundos que le quedan de protección al último que robó.</summary>
        public float Proteccion { get; private set; }

        public bool MuerteSubita { get; private set; }
        public int GanadorDeRonda { get; private set; } = Nadie;
        public int GanadorDePartida { get; private set; } = Nadie;

        public float TiempoParaGanar => MuerteSubita ? _reglas.tiempoMuerteSubita : _reglas.TiempoParaGanar(CantidadDeKarts);

        /// <summary>De 0 a 1: cuánto le falta al dueño para ganar la ronda.</summary>
        public float Progreso => Duenio == Nadie ? 0f : Mathf.Clamp01(TiempoAguantado / TiempoParaGanar);

        public IReadOnlyList<EventoDePartida> Eventos => _eventos;

        public int Victorias(int kart) => _victorias[kart];

        /// <summary>Si alguien tiene la comida, cuánto más rápido o más lento va este kart.</summary>
        public float MultiplicadorVelocidad(int kart)
        {
            if (Duenio == Nadie || Estado != EstadoDePartida.Jugando) return 1f;
            return kart == Duenio ? _reglas.velocidadDuenio : _reglas.velocidadPerseguidores;
        }

        /// <summary>Los dos son perseguidores (ninguno tiene la comida): rebotan al chocarse.</summary>
        public bool SonPerseguidores(int a, int b) => a != Duenio && b != Duenio;

        // ------------------------------------------------------------------ lo que cuenta la escena

        /// <summary>Un kart toca la comida (solo cuenta si está suelta).</summary>
        public void ReportarToqueComida(int kart) => _toquesComida.Add(kart);

        /// <summary>Dos karts empiezan a tocarse.</summary>
        public void ReportarToqueEntreKarts(int a, int b) => _toquesKarts.Add((a, b));

        /// <summary>La escena ya aplicó los eventos: se borran.</summary>
        public void LimpiarEventos() => _eventos.Clear();

        // ------------------------------------------------------------------ paso

        public void Avanzar(float dt)
        {
            switch (Estado)
            {
                case EstadoDePartida.CuentaRegresiva:
                    AvanzarCuenta(dt);
                    break;
                case EstadoDePartida.Jugando:
                    AvanzarJuego(dt);
                    break;
                case EstadoDePartida.FinDeRonda:
                case EstadoDePartida.FinDePartida:
                    AvanzarPausa(dt);
                    break;
            }
            _toquesComida.Clear();
            _toquesKarts.Clear();
        }

        private void AvanzarCuenta(float dt)
        {
            TiempoDeEspera -= dt;
            int cuenta = Mathf.CeilToInt(TiempoDeEspera);
            if (cuenta != _ultimaCuenta && cuenta >= 1)
            {
                _ultimaCuenta = cuenta;
                _eventos.Add(new EventoDePartida(TipoDeEvento.Cuenta, numero: cuenta));
            }
            if (TiempoDeEspera > 0f) return;

            Estado = EstadoDePartida.Jugando;
            _eventos.Add(new EventoDePartida(TipoDeEvento.Arranca, numero: Ronda));
        }

        private void AvanzarJuego(float dt)
        {
            TiempoDeRonda += dt;
            Proteccion = Mathf.Max(0f, Proteccion - dt);
            _esperaTrasCaida = Mathf.Max(0f, _esperaTrasCaida - dt);

            if (!MuerteSubita && TiempoDeRonda >= _reglas.muerteSubitaTras)
            {
                // El próximo que aguante la comida unos pocos segundos gana: el aguante vuelve a cero.
                MuerteSubita = true;
                TiempoAguantado = 0f;
                _eventos.Add(new EventoDePartida(TipoDeEvento.MuerteSubita));
            }

            if (Duenio == Nadie) ResolverAgarre();
            else ResolverRobo(dt);

            if (Duenio == Nadie) return;
            TiempoAguantado += dt;
            if (TiempoAguantado >= TiempoParaGanar) GanarRonda(Duenio);
        }

        private void ResolverAgarre()
        {
            if (_toquesComida.Count == 0) return;

            // Si la tocan varios a la vez, se la lleva el de número más bajo (así siempre da lo mismo). Al que
            // se le acaba de caer no la puede volver a agarrar enseguida.
            int quien = int.MaxValue;
            foreach (int kart in _toquesComida)
            {
                if (kart == _seLeCayo && _esperaTrasCaida > 0f) continue;
                quien = Mathf.Min(quien, kart);
            }
            if (quien == int.MaxValue) return;
            CambiarDuenio(quien);
            _eventos.Add(new EventoDePartida(TipoDeEvento.Agarro, quien));
        }

        private void ResolverRobo(float dt)
        {
            // Con un toque alcanza para robar, salvo que el dueño esté protegido. Si otro kart también lo
            // toca dentro de la ventana, la comida se cae en vez de ser robada.
            if (Proteccion <= 0f)
            {
                foreach (var (a, b) in _toquesKarts)
                {
                    int ladron = a == Duenio ? b : b == Duenio ? a : Nadie;
                    if (ladron == Nadie || ladron == Duenio || _quienesTocaron.Contains(ladron)) continue;
                    _quienesTocaron.Add(ladron);
                    if (_ventana < 0f) _ventana = _reglas.ventanaDobleToque;
                }
            }

            if (_ventana < 0f) return;
            _ventana -= dt;

            if (_quienesTocaron.Count >= 2)
            {
                int caido = Duenio;
                Soltar();
                _seLeCayo = caido;
                _esperaTrasCaida = _reglas.esperaTrasCaida;
                _eventos.Add(new EventoDePartida(TipoDeEvento.Cayo, caido));
            }
            else if (_ventana <= 0f)
            {
                int robado = Duenio, ladron = _quienesTocaron[0];
                CambiarDuenio(ladron);
                Proteccion = _reglas.proteccionTrasRobo;
                _eventos.Add(new EventoDePartida(TipoDeEvento.Robo, ladron, robado));
            }
        }

        private void GanarRonda(int ganador)
        {
            GanadorDeRonda = ganador;
            _victorias[ganador]++;
            _eventos.Add(new EventoDePartida(TipoDeEvento.GanoRonda, ganador, numero: _victorias[ganador]));
            Soltar();

            if (_victorias[ganador] >= _reglas.rondasParaGanar)
            {
                GanadorDePartida = ganador;
                Estado = EstadoDePartida.FinDePartida;
                TiempoDeEspera = _reglas.pausaFinDePartida;
                _eventos.Add(new EventoDePartida(TipoDeEvento.GanoPartida, ganador));
            }
            else
            {
                Estado = EstadoDePartida.FinDeRonda;
                TiempoDeEspera = _reglas.pausaEntreRondas;
            }
        }

        private void AvanzarPausa(float dt)
        {
            TiempoDeEspera -= dt;
            if (TiempoDeEspera > 0f) return;

            if (Estado == EstadoDePartida.FinDePartida)
            {
                // Partida nueva desde cero.
                for (int i = 0; i < _victorias.Length; i++) _victorias[i] = 0;
                GanadorDePartida = Nadie;
                Ronda = 1;
            }
            else
            {
                Ronda++;
            }
            EmpezarRonda();
            _eventos.Add(new EventoDePartida(TipoDeEvento.NuevaRonda, numero: Ronda));
        }

        private void EmpezarRonda()
        {
            Estado = EstadoDePartida.CuentaRegresiva;
            TiempoDeEspera = _reglas.cuentaRegresiva;
            _ultimaCuenta = 0;
            TiempoDeRonda = 0f;
            MuerteSubita = false;
            GanadorDeRonda = Nadie;
            Proteccion = 0f;
            _seLeCayo = Nadie;
            _esperaTrasCaida = 0f;
            Soltar();
        }

        private void CambiarDuenio(int kart)
        {
            Duenio = kart;
            TiempoAguantado = 0f;
            _quienesTocaron.Clear();
            _ventana = -1f;
        }

        private void Soltar()
        {
            Duenio = Nadie;
            TiempoAguantado = 0f;
            _quienesTocaron.Clear();
            _ventana = -1f;
        }
    }
}
