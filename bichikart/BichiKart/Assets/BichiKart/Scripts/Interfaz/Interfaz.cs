using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// Lo que se ve en pantalla durante la partida, con paneles "inflados":
    /// arriba, la barra con la carita del dueño avanzando hacia la meta; en el medio, la cuenta regresiva y
    /// los avisos; una flecha que apunta al dueño (o a la comida suelta); el minimapa holográfico abajo a la
    /// derecha, visto desde arriba; y el marcador de rondas. Se arma desde código al empezar.
    /// </summary>
    public sealed class Interfaz : MonoBehaviour
    {
        private const float DuracionAviso = 1.6f;
        private const float AnchoBarra = 860f;
        private const float MargenFlecha = 70f;

        /// <summary>Arriba la flecha deja lugar para la barra de tiempo.</summary>
        private const float MargenFlechaArriba = 170f;

        private static readonly Color Panel = new Color(0.11f, 0.14f, 0.25f, 0.8f);
        private static readonly Color Holograma = new Color(0.24f, 0.76f, 0.94f, 0.22f);
        private static readonly Color BordeHolograma = new Color(0.55f, 0.9f, 1f, 0.9f);
        private static readonly Color ColorDelPan = new Color(1f, 0.82f, 0.25f);

        [SerializeField] private Partida partida;
        [SerializeField] private Camera camara;

        [Tooltip("Número del jugador que mira esta pantalla (a él no se le muestra la flecha).")]
        [SerializeField] private int jugador;

        private Font _letra;
        private RectTransform _lienzo;
        private Text _aviso, _aguante, _marcador;
        private float _avisoHasta;

        private RectTransform _relleno, _carita;
        private Image _rellenoColor, _caritaColor;
        private Text _caritaLetra;

        private RectTransform _flecha;
        private Image _flechaColor;

        private RectTransform _mapa;
        private Vector2 _medidasMapa;
        private readonly List<IconoDelMapa> _iconos = new List<IconoDelMapa>();
        private RectTransform _iconoPan, _rumbo;
        private Image _iconoPanColor;

        private sealed class IconoDelMapa
        {
            public RectTransform Lugar;
            public Image Relleno, Anillo;
        }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(Partida nuevaPartida, Camera nuevaCamara)
        {
            partida = nuevaPartida;
            camara = nuevaCamara;
        }

        private void Start()
        {
            _letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ArmarLienzo();
            ArmarBarra();
            ArmarFlecha();
            ArmarMinimapa();
            _aviso = Texto("Aviso", transform, 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(1400f, 300f), Vector2.zero);
            _marcador = Texto("Marcador", transform, 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(380f, 260f), new Vector2(40f, -30f));
            _marcador.rectTransform.pivot = new Vector2(0f, 1f);
            partida.AlOcurrir += Avisar;
        }

        private void OnDestroy()
        {
            if (partida != null) partida.AlOcurrir -= Avisar;
        }

        private void Update()
        {
            var arbitro = partida.Arbitro;
            if (arbitro == null) return;

            ActualizarBarra(arbitro);
            ActualizarFlecha(arbitro);
            ActualizarMinimapa(arbitro);
            ActualizarMarcador(arbitro);

            var color = arbitro.Duenio == Arbitro.Nadie ? Color.white : partida.Participantes[arbitro.Duenio].Color;
            partida.Comida.MostrarProgreso(arbitro.Progreso, color);
            if (Time.time > _avisoHasta) _aviso.text = "";
        }

        // ------------------------------------------------------------------ barra de tiempo

        private void ArmarBarra()
        {
            var fondo = Imagen("Barra", transform, Dibujos.Redondeado, Panel, new Vector2(0.5f, 1f), new Vector2(AnchoBarra + 20f, 52f), new Vector2(0f, -30f));
            fondo.pivot = new Vector2(0.5f, 1f);
            Borde(fondo, Color.white, 3f);

            _relleno = Imagen("Relleno", fondo, Dibujos.Redondeado, Color.white, new Vector2(0f, 0.5f), new Vector2(36f, 36f), new Vector2(10f, 0f));
            _relleno.pivot = new Vector2(0f, 0.5f);
            _rellenoColor = _relleno.GetComponent<Image>();

            var meta = Imagen("Meta", fondo, Dibujos.Circulo, ColorDelPan, new Vector2(1f, 0.5f), new Vector2(34f, 34f), new Vector2(-28f, 0f));
            Borde(meta, Color.white, 2f);

            _carita = Imagen("Carita", fondo, Dibujos.Circulo, Color.white, new Vector2(0f, 0.5f), new Vector2(66f, 66f), new Vector2(10f, 0f));
            _caritaColor = _carita.GetComponent<Image>();
            Borde(_carita, Color.white, 4f);
            _caritaLetra = Texto("Letra", _carita, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(66f, 66f), Vector2.zero);

            _aguante = Texto("Aguante", transform, 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(1000f, 50f), new Vector2(0f, -96f));
            _aguante.rectTransform.pivot = new Vector2(0.5f, 1f);
        }

        private void ActualizarBarra(Arbitro arbitro)
        {
            bool hayDuenio = arbitro.Duenio != Arbitro.Nadie;
            _relleno.gameObject.SetActive(hayDuenio);
            _carita.gameObject.SetActive(hayDuenio);
            if (!hayDuenio)
            {
                _aguante.text = arbitro.MuerteSubita ? "¡Muerte súbita! El pan está suelto" : "El pan está suelto";
                _aguante.color = Color.white;
                return;
            }

            var duenio = partida.Participantes[arbitro.Duenio];
            float avance = arbitro.Progreso * AnchoBarra;
            _relleno.sizeDelta = new Vector2(Mathf.Max(36f, avance), 36f);
            _rellenoColor.color = duenio.Color;
            _carita.anchoredPosition = new Vector2(10f + avance, 0f);
            _caritaColor.color = duenio.Color;
            _caritaLetra.text = duenio.Nombre.Substring(0, 1);
            _aguante.text = $"{duenio.Nombre}: {arbitro.TiempoAguantado:0.0} / {arbitro.TiempoParaGanar:0} s" + (arbitro.MuerteSubita ? " (muerte súbita)" : "");
            _aguante.color = Color.Lerp(duenio.Color, Color.white, 0.35f);
        }

        // ------------------------------------------------------------------ flecha

        private void ArmarFlecha()
        {
            _flecha = Imagen("Flecha", transform, Dibujos.Flecha, Color.white, new Vector2(0.5f, 0.5f), new Vector2(80f, 80f), Vector2.zero);
            _flechaColor = Imagen("Color", _flecha, Dibujos.Flecha, Color.white, new Vector2(0.5f, 0.5f), new Vector2(56f, 56f), new Vector2(0f, -4f)).GetComponent<Image>();
        }

        /// <summary>
        /// Apunta al dueño de la comida (o a la comida, si está suelta). Si está en pantalla, la flecha flota
        /// arriba de él; si no, queda en el borde de la pantalla, apuntando hacia donde está.
        /// </summary>
        private void ActualizarFlecha(Arbitro arbitro)
        {
            bool mostrar = arbitro.Estado == EstadoDePartida.Jugando && arbitro.Duenio != jugador;
            _flecha.gameObject.SetActive(mostrar);
            if (!mostrar) return;

            Vector3 objetivo;
            if (arbitro.Duenio == Arbitro.Nadie)
            {
                objetivo = partida.Comida.Centro + partida.Comida.Arriba * 1.6f;
                _flechaColor.color = ColorDelPan;
            }
            else
            {
                var duenio = partida.Participantes[arbitro.Duenio];
                objetivo = duenio.Kart.Chasis.position + duenio.Kart.Arriba * 3.6f;
                _flechaColor.color = duenio.Color;
            }

            var pantalla = camara.WorldToScreenPoint(objetivo);
            var centro = new Vector2(Screen.width, Screen.height) * 0.5f;
            var desdeElCentro = (Vector2)pantalla - centro;
            bool detras = pantalla.z < 0f;
            if (detras) desdeElCentro = -desdeElCentro;

            float limiteX = centro.x - MargenFlecha;
            float limiteY = desdeElCentro.y > 0f ? centro.y - MargenFlechaArriba : centro.y - MargenFlecha;
            bool adentro = !detras && Mathf.Abs(desdeElCentro.x) < limiteX && Mathf.Abs(desdeElCentro.y) < limiteY;
            float angulo;
            if (adentro)
            {
                // Flota arriba del objetivo, apuntando para abajo (sin taparle la barra).
                desdeElCentro.y = Mathf.Min(desdeElCentro.y + 30f + Mathf.Sin(Time.time * 6f) * 8f, centro.y - MargenFlechaArriba);
                angulo = 180f;
            }
            else
            {
                if (desdeElCentro.sqrMagnitude < 1f) desdeElCentro = Vector2.down;
                float escala = Mathf.Min(limiteX / Mathf.Max(Mathf.Abs(desdeElCentro.x), 1e-3f), limiteY / Mathf.Max(Mathf.Abs(desdeElCentro.y), 1e-3f));
                desdeElCentro *= escala;
                angulo = Mathf.Atan2(desdeElCentro.y, desdeElCentro.x) * Mathf.Rad2Deg - 90f;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_lienzo, centro + desdeElCentro, camara, out var lugar);
            _flecha.localPosition = lugar;
            _flecha.localRotation = Quaternion.Euler(0f, 0f, angulo);
        }

        // ------------------------------------------------------------------ minimapa

        private void ArmarMinimapa()
        {
            var sala = partida.Cocina.Medidas;
            const float ancho = 320f;
            var medidas = new Vector2(ancho, ancho * sala.z / sala.x);
            var fondo = Imagen("Minimapa", transform, Dibujos.Redondeado, Holograma, new Vector2(1f, 0f), medidas + Vector2.one * 28f, new Vector2(-30f, 30f));
            fondo.pivot = new Vector2(1f, 0f);
            Borde(fondo, BordeHolograma, 2f);

            _mapa = Imagen("Sala", fondo, Dibujos.Redondeado, new Color(1f, 1f, 1f, 0.08f), new Vector2(0.5f, 0.5f), medidas, Vector2.zero);
            _medidasMapa = medidas;

            _iconoPan = Imagen("Pan", _mapa, Dibujos.Circulo, ColorDelPan, new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), Vector2.zero);
            _iconoPanColor = _iconoPan.GetComponent<Image>();

            _iconos.Clear();
            for (int i = 0; i < partida.Participantes.Count; i++)
            {
                var participante = partida.Participantes[i];
                bool esJugador = i == jugador;
                float tamano = esJugador ? 30f : 24f;
                var lugar = Imagen(participante.Nombre, _mapa, null, Color.clear, new Vector2(0.5f, 0.5f), new Vector2(tamano, tamano), Vector2.zero);
                var relleno = Imagen("Relleno", lugar, Dibujos.Circulo, participante.Color, new Vector2(0.5f, 0.5f), new Vector2(tamano, tamano), Vector2.zero);
                var anillo = Imagen("Anillo", lugar, Dibujos.Anillo, participante.Color, new Vector2(0.5f, 0.5f), new Vector2(tamano + 8f, tamano + 8f), Vector2.zero);
                if (esJugador)
                {
                    Borde(relleno, Color.white, 2f);
                    _rumbo = Imagen("Rumbo", lugar, Dibujos.Flecha, Color.white, new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), Vector2.zero);
                }
                _iconos.Add(new IconoDelMapa { Lugar = lugar, Relleno = relleno.GetComponent<Image>(), Anillo = anillo.GetComponent<Image>() });
            }
            // El jugador arriba de todo, para que siempre se vea.
            _iconos[jugador].Lugar.SetAsLastSibling();
            _iconoPan.SetAsLastSibling();
        }

        /// <summary>
        /// Vista de arriba. El ícono de cada kart cambia según dónde está: lleno en el piso, anillo en el
        /// techo y las dos cosas (más chico) en una pared. El pan titila.
        /// </summary>
        private void ActualizarMinimapa(Arbitro arbitro)
        {
            var cocina = partida.Cocina;
            for (int i = 0; i < _iconos.Count; i++)
            {
                var kart = partida.Participantes[i].Kart;
                var icono = _iconos[i];
                icono.Lugar.anchoredPosition = EnElMapa(kart.transform.position);
                float arriba = Vector3.Dot(kart.Arriba, cocina.transform.up);
                bool piso = arriba > 0.6f, techo = arriba < -0.6f;
                icono.Relleno.enabled = !techo;
                icono.Anillo.enabled = !piso;
                icono.Lugar.localScale = Vector3.one * (piso || techo ? 1f : 0.75f);
            }

            if (_rumbo != null)
            {
                var adelante = cocina.transform.InverseTransformDirection(partida.Participantes[jugador].Kart.Chasis.forward);
                _rumbo.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(adelante.x, adelante.z) * Mathf.Rad2Deg);
            }

            var pan = arbitro.Duenio == Arbitro.Nadie ? partida.Comida.Centro : partida.Participantes[arbitro.Duenio].Kart.transform.position;
            _iconoPan.anchoredPosition = EnElMapa(pan) + (arbitro.Duenio == Arbitro.Nadie ? Vector2.zero : new Vector2(12f, 12f));
            var color = ColorDelPan;
            color.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 8f);
            _iconoPanColor.color = color;
        }

        private Vector2 EnElMapa(Vector3 lugar)
        {
            var sala = partida.Cocina.Medidas;
            var local = partida.Cocina.transform.InverseTransformPoint(lugar);
            return new Vector2(Mathf.Clamp(local.x / sala.x, -0.5f, 0.5f) * _medidasMapa.x, Mathf.Clamp(local.z / sala.z, -0.5f, 0.5f) * _medidasMapa.y);
        }

        // ------------------------------------------------------------------ marcador y avisos

        private void ActualizarMarcador(Arbitro arbitro)
        {
            var marcador = new System.Text.StringBuilder($"Ronda {arbitro.Ronda}\n");
            for (int i = 0; i < partida.Participantes.Count; i++)
            {
                var p = partida.Participantes[i];
                string color = ColorUtility.ToHtmlStringRGB(p.Color);
                marcador.Append($"<color=#{color}>{p.Nombre}</color> {new string('●', arbitro.Victorias(i))}\n");
            }
            _marcador.text = marcador.ToString();
        }

        private void Avisar(EventoDePartida evento)
        {
            string Nombre(int i) => i >= 0 ? partida.Participantes[i].Nombre : "";
            switch (evento.Tipo)
            {
                case TipoDeEvento.Cuenta: Mostrar(evento.Numero.ToString(), 1f); break;
                case TipoDeEvento.Arranca: Mostrar("¡Ya!"); break;
                case TipoDeEvento.Agarro: Mostrar($"¡{Nombre(evento.Kart)} agarró el pan!"); break;
                case TipoDeEvento.Robo: Mostrar($"¡{Nombre(evento.Kart)} se lo robó a {Nombre(evento.Otro)}!"); break;
                case TipoDeEvento.Cayo: Mostrar($"¡A {Nombre(evento.Kart)} se le cayó el pan!"); break;
                case TipoDeEvento.MuerteSubita: Mostrar("¡Muerte súbita!\nEl próximo que aguante 5 segundos gana", 2.5f); break;
                case TipoDeEvento.GanoRonda: Mostrar($"¡{Nombre(evento.Kart)} gana la ronda!", 3f); break;
                case TipoDeEvento.GanoPartida: Mostrar($"¡{Nombre(evento.Kart)} gana la partida!", 6f); break;
                case TipoDeEvento.NuevaRonda: Mostrar(evento.Numero == 1 ? "Partida nueva" : $"Ronda {evento.Numero}", 1.2f); break;
            }
        }

        private void Mostrar(string texto, float segundos = DuracionAviso)
        {
            _aviso.text = texto;
            _avisoHasta = Time.time + segundos;
        }

        // ------------------------------------------------------------------ armado

        private void ArmarLienzo()
        {
            // En el espacio de la cámara (no encima de todo), para que salga también en las capturas.
            var lienzo = gameObject.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = camara;
            lienzo.planeDistance = 0.5f;
            var escala = gameObject.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;
            _lienzo = (RectTransform)transform;
        }

        private RectTransform Imagen(string nombre, Transform padre, Sprite dibujo, Color color, Vector2 ancla, Vector2 medidas, Vector2 lugar)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = ancla;
            rect.sizeDelta = medidas;
            rect.anchoredPosition = lugar;
            if (dibujo == null) return rect;

            var imagen = go.AddComponent<Image>();
            imagen.sprite = dibujo;
            imagen.color = color;
            imagen.type = dibujo == Dibujos.Redondeado ? Image.Type.Sliced : Image.Type.Simple;
            imagen.raycastTarget = false;
            return rect;
        }

        private static void Borde(RectTransform rect, Color color, float grosor)
        {
            var borde = rect.gameObject.AddComponent<Outline>();
            borde.effectColor = color;
            borde.effectDistance = new Vector2(grosor, -grosor);
        }

        private Text Texto(string nombre, Transform padre, int tamano, TextAnchor alineacion, Vector2 ancla, Vector2 medidas, Vector2 lugar)
        {
            var rect = Imagen(nombre, padre, null, Color.clear, ancla, medidas, lugar);
            var texto = rect.gameObject.AddComponent<Text>();
            texto.font = _letra;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
            texto.color = Color.white;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            var borde = rect.gameObject.AddComponent<Outline>();
            borde.effectColor = new Color(0.11f, 0.14f, 0.25f, 0.9f);
            borde.effectDistance = new Vector2(3f, -3f);
            return texto;
        }
    }
}
