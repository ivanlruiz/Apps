using System;
using System.Collections.Generic;
using UnityEngine;

namespace BichiKart
{
    /// <summary>
    /// Une el <see cref="Arbitro"/> con la escena. En cada paso de física mira qué karts se tocan entre sí y
    /// quién toca la comida, se lo cuenta al árbitro, y aplica lo que pasó: la comida cambia de dueño, el
    /// robado sale despedido, los perseguidores rebotan, suenan el "plop" y la queja, y al empezar cada
    /// ronda todo vuelve a su lugar.
    /// </summary>
    public sealed class Partida : MonoBehaviour
    {
        [SerializeField] private ReglasDePartida reglas;
        [SerializeField] private Cocina cocina;
        [SerializeField] private Comida comida;
        [SerializeField] private List<Participante> participantes = new List<Participante>();

        [Header("Compu")]
        [Tooltip("Ajustes de la compu para cada nivel, del más fácil al más difícil.")]
        [SerializeField] private AjustesDeCompu[] niveles = new AjustesDeCompu[0];

        [Tooltip("Con qué nivel arranca (0 = el primero).")]
        [SerializeField] private int nivel = 1;

        [Header("Toques")]
        [Tooltip("Cada kart se mide como una cápsula: una línea a lo largo del kart con este grosor alrededor.")]
        [SerializeField] private float radioKart = 0.85f;

        [Tooltip("Mitad del largo de la línea del medio de cada kart.")]
        [SerializeField] private float medioLargoKart = 0.6f;

        [Tooltip("Distancia a la comida para agarrarla.")]
        [SerializeField] private float distanciaComida = 2.2f;

        private bool[,] _tocandose;
        private SonidosDelKart[] _sonidos;

        public Arbitro Arbitro { get; private set; }
        public ReglasDePartida Reglas => reglas;
        public Comida Comida => comida;
        public Cocina Cocina => cocina;
        public IReadOnlyList<Participante> Participantes => participantes;

        /// <summary>Pasó algo en la partida (para la interfaz y los sonidos).</summary>
        public event Action<EventoDePartida> AlOcurrir;

        /// <summary>Nombres de los niveles de la compu, en el mismo orden que sus ajustes.</summary>
        public static readonly string[] NombresDeNivel = { "Fácil", "Normal", "Difícil" };

        /// <summary>Nivel de la compu que se está usando (0 = el más fácil).</summary>
        public int Nivel => nivel;

        public int CantidadDeNiveles => niveles.Length;

        public string NombreDelNivel => nivel >= 0 && nivel < NombresDeNivel.Length ? NombresDeNivel[nivel] : $"Nivel {nivel + 1}";

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(ReglasDePartida nuevasReglas, Cocina nuevaCocina, Comida nuevaComida, List<Participante> nuevosParticipantes, AjustesDeCompu[] nuevosNiveles)
        {
            reglas = nuevasReglas;
            cocina = nuevaCocina;
            comida = nuevaComida;
            participantes = nuevosParticipantes;
            niveles = nuevosNiveles;
        }

        /// <summary>Cambia cómo juegan todas las compus (se puede hacer en cualquier momento).</summary>
        public void CambiarNivel(int nuevoNivel)
        {
            if (niveles.Length == 0) return;
            nivel = Mathf.Clamp(nuevoNivel, 0, niveles.Length - 1);
            foreach (var participante in participantes)
            {
                var compu = participante.GetComponent<FuenteCompu>();
                if (compu != null) compu.Configurar(niveles[nivel]);
            }
        }

        private void Start()
        {
            int n = participantes.Count;
            _tocandose = new bool[n, n];
            _sonidos = new SonidosDelKart[n];
            for (int i = 0; i < n; i++)
            {
                participantes[i].Indice = i;
                _sonidos[i] = participantes[i].GetComponent<SonidosDelKart>();
            }
            Arbitro = new Arbitro(reglas, n);
            CambiarNivel(nivel);
            UbicarTodo();
            ActualizarKarts();
        }

        private void FixedUpdate()
        {
            if (Arbitro == null) return;
            DetectarToques();
            Arbitro.Avanzar(Time.fixedDeltaTime);
            foreach (var evento in Arbitro.Eventos)
            {
                Aplicar(evento);
                AlOcurrir?.Invoke(evento);
            }
            Arbitro.LimpiarEventos();
            ActualizarKarts();
        }

        // ------------------------------------------------------------------ toques

        private void DetectarToques()
        {
            int n = participantes.Count;
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                bool toca = DistanciaEntreKarts(participantes[i].Kart, participantes[j].Kart) < radioKart * 2f;
                if (toca && !_tocandose[i, j])
                {
                    // Solo cuenta cuando empiezan a tocarse: si siguen pegados, no se roban de nuevo.
                    Arbitro.ReportarToqueEntreKarts(i, j);
                    if (Arbitro.SonPerseguidores(i, j)) Rebotar(participantes[i].Kart, participantes[j].Kart);
                }
                _tocandose[i, j] = toca;
            }

            if (comida.Duenio != null) return;
            for (int i = 0; i < n; i++)
            {
                var kart = participantes[i].Kart;
                var punto = kart.Chasis.position + kart.Arriba * 1.2f;
                if (Vector3.Distance(punto, comida.Centro) < distanciaComida) Arbitro.ReportarToqueComida(i);
            }
        }

        private float DistanciaEntreKarts(Kart a, Kart b)
        {
            Vector3 centroA = a.Chasis.position + a.Arriba * 0.65f, ejeA = a.Adelante * medioLargoKart;
            Vector3 centroB = b.Chasis.position + b.Arriba * 0.65f, ejeB = b.Adelante * medioLargoKart;
            return DistanciaEntreSegmentos(centroA - ejeA, centroA + ejeA, centroB - ejeB, centroB + ejeB);
        }

        /// <summary>Distancia más corta entre los segmentos p1–q1 y p2–q2.</summary>
        public static float DistanciaEntreSegmentos(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s, t;
            if (a <= 1e-6f && e <= 1e-6f) return r.magnitude;
            if (a <= 1e-6f)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 1e-6f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denominador = a * e - b * b;
                    s = denominador > 1e-6f ? Mathf.Clamp01((b * f - c * e) / denominador) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }
            return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        private void Rebotar(Kart a, Kart b)
        {
            var posA = a.transform.position;
            a.Despedir(b.transform.position, reglas.empujeRebote, reglas.saltoRebote, 0.15f);
            b.Despedir(posA, reglas.empujeRebote, reglas.saltoRebote, 0.15f);
        }

        // ------------------------------------------------------------------ eventos

        private void Aplicar(EventoDePartida evento)
        {
            switch (evento.Tipo)
            {
                case TipoDeEvento.Agarro:
                    comida.Seguir(participantes[evento.Kart].Kart);
                    if (_sonidos[evento.Kart] != null) _sonidos[evento.Kart].Agarrar();
                    break;

                case TipoDeEvento.Robo:
                {
                    var ladron = participantes[evento.Kart].Kart;
                    var robado = participantes[evento.Otro].Kart;
                    comida.Seguir(ladron);
                    robado.Despedir(ladron.transform.position, reglas.empujeRobado, reglas.saltoRobado, reglas.aturdidoRobado);
                    if (_sonidos[evento.Kart] != null) _sonidos[evento.Kart].Agarrar();
                    if (_sonidos[evento.Otro] != null) _sonidos[evento.Otro].Quejarse();
                    break;
                }

                case TipoDeEvento.Cayo:
                {
                    var kart = participantes[evento.Kart].Kart;
                    comida.Soltar(kart.Chasis.position, kart.Arriba);
                    if (_sonidos[evento.Kart] != null) _sonidos[evento.Kart].Quejarse();
                    break;
                }

                case TipoDeEvento.NuevaRonda:
                    UbicarTodo();
                    break;
            }
        }

        /// <summary>Cada kart a su salida y la comida flotando en el medio de la sala.</summary>
        private void UbicarTodo()
        {
            for (int i = 0; i < participantes.Count; i++)
            {
                var salida = cocina.Salida(i);
                participantes[i].Kart.Ubicar(salida.position, salida.rotation * Vector3.forward);
            }
            comida.Soltar(cocina.transform.position, cocina.transform.up);
            System.Array.Clear(_tocandose, 0, _tocandose.Length);
        }

        private void ActualizarKarts()
        {
            bool quietos = Arbitro.Estado != EstadoDePartida.Jugando;
            for (int i = 0; i < participantes.Count; i++)
            {
                var kart = participantes[i].Kart;
                kart.Quieto = quietos;
                kart.MultiplicadorVelocidad = Arbitro.MultiplicadorVelocidad(i);
                kart.LlevaComida = Arbitro.Duenio == i;
            }
        }
    }
}
