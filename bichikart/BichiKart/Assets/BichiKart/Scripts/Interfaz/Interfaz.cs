using UnityEngine;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// Lo que se ve en pantalla durante la partida: la cuenta regresiva y los avisos en el medio, quién
    /// tiene la comida y cuánto le falta, y el marcador de rondas. Se arma desde código al empezar.
    /// </summary>
    public sealed class Interfaz : MonoBehaviour
    {
        private const float DuracionAviso = 1.6f;

        [SerializeField] private Partida partida;
        [SerializeField] private Camera camara;

        private Font _letra;
        private Text _aviso, _aguante, _marcador;
        private float _avisoHasta;

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

            if (arbitro.Duenio == Arbitro.Nadie)
            {
                _aguante.text = arbitro.MuerteSubita ? "¡Muerte súbita! El pan está suelto" : "El pan está suelto";
                _aguante.color = Color.white;
            }
            else
            {
                var duenio = partida.Participantes[arbitro.Duenio];
                _aguante.text = $"{duenio.Nombre}: {arbitro.TiempoAguantado:0.0} / {arbitro.TiempoParaGanar:0} s";
                _aguante.color = Color.Lerp(duenio.Color, Color.white, 0.35f);
            }

            var marcador = new System.Text.StringBuilder($"Ronda {arbitro.Ronda}\n");
            foreach (var p in partida.Participantes) marcador.Append($"{p.Nombre} {new string('●', arbitro.Victorias(p.Indice))}\n");
            _marcador.text = marcador.ToString();

            if (Time.time > _avisoHasta) _aviso.text = "";
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

            _aviso = Texto("Aviso", 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(1400f, 300f));
            _aguante = Texto("Aguante", 40, TextAnchor.UpperCenter, new Vector2(0.5f, 0.97f), new Vector2(1000f, 60f));
            _marcador = Texto("Marcador", 30, TextAnchor.UpperLeft, new Vector2(0.11f, 0.95f), new Vector2(360f, 260f));
        }

        private Text Texto(string nombre, int tamano, TextAnchor alineacion, Vector2 ancla, Vector2 medidas)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = ancla;
            rect.pivot = new Vector2(0.5f, ancla.y > 0.5f ? 1f : 0.5f);
            rect.sizeDelta = medidas;
            var texto = go.AddComponent<Text>();
            texto.font = _letra;
            texto.fontSize = tamano;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
            texto.color = Color.white;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            var borde = go.AddComponent<Outline>();
            borde.effectColor = new Color(0.11f, 0.14f, 0.25f, 0.9f);
            borde.effectDistance = new Vector2(3f, -3f);
            return texto;
        }
    }
}
