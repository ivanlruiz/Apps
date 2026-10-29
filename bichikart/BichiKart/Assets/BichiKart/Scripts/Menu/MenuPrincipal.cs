using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// El menú principal: la cámara se hamaca despacio frente a la <see cref="CasaDelMenu"/> y cada cuarto
    /// tiene un cartel de madera con una flecha. Partida rápida lleva la cámara hasta el living y ahí se
    /// elige el nivel de la compu (Fácil, Normal o Difícil); los otros cuartos todavía dicen "¡Próximamente!".
    /// Abajo a la derecha están Opciones y Salir. Escape vuelve atrás. Se arma desde código al empezar.
    /// </summary>
    public sealed class MenuPrincipal : MonoBehaviour
    {
        private enum Estado { Casa, Niveles, Opciones, Cargando }

        private static readonly Color Madera = new Color(0.6f, 0.38f, 0.2f);
        private static readonly Color Crema = new Color(1f, 0.92f, 0.77f);
        private static readonly Color Naranja = new Color(1f, 0.62f, 0.11f);
        private static readonly Color Verde = new Color(0.25f, 0.7f, 0.42f);
        private static readonly Color Azul = new Color(0.24f, 0.48f, 0.88f);
        private static readonly Color Rojo = new Color(0.89f, 0.28f, 0.24f);

        private const float DuracionViaje = 1.3f;

        [SerializeField] private CasaDelMenu casa;
        [SerializeField] private Camera camara;

        private readonly Dictionary<CasaDelMenu.Cuarto, Cartel> _carteles = new Dictionary<CasaDelMenu.Cuarto, Cartel>();
        private RectTransform _lienzo, _niveles, _abajo, _logo;
        private PanelDeOpciones _opciones;
        private Text _aviso;
        private float _avisoHasta;
        private Estado _estado = Estado.Casa;
        private float _viaje;
        private Button[] _botonesDeNivel;

        private sealed class Cartel
        {
            public RectTransform Lugar, Flecha;
            public Button Boton;
            public float Sacudida;
        }

        /// <summary>Lo usa la herramienta que arma la escena.</summary>
        public void Configurar(CasaDelMenu nuevaCasa, Camera nuevaCamara)
        {
            casa = nuevaCasa;
            camara = nuevaCamara;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Piezas.AsegurarEventSystem();

            _lienzo = Piezas.Lienzo("Pantalla del menú", 0, true);
            _lienzo.SetParent(transform, false);
            ArmarLogo();
            ArmarCarteles();
            ArmarBotonesDeAbajo();
            ArmarNiveles();
            _opciones = PanelDeOpciones.Crear(_lienzo, VolverALaCasa);
            _aviso = Piezas.Texto("Aviso", _lienzo, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(1200f, 80f), new Vector2(0f, 190f));
            MostrarCasa();
        }

        private void Update()
        {
            float objetivo = _estado == Estado.Niveles || _estado == Estado.Cargando ? 1f : 0f;
            _viaje = Mathf.MoveTowards(_viaje, objetivo, Time.unscaledDeltaTime / DuracionViaje);
            MoverCamara();

            bool enLaCasa = _estado == Estado.Casa;
            foreach (var par in _carteles) ActualizarCartel(par.Key, par.Value, enLaCasa);
            // El panel de los niveles aparece cuando la cámara ya casi llegó al living.
            _niveles.gameObject.SetActive(_estado == Estado.Niveles && _viaje > 0.85f);
            // El logo se ve desde lejos; adentro del living taparía el techo.
            _logo.gameObject.SetActive(_viaje < 0.5f);
            _logo.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 2.2f));
            if (Time.unscaledTime > _avisoHasta) _aviso.text = "";

            if (Atras()) VolverAtras();
        }

        // ------------------------------------------------------------------ estados

        private void MostrarCasa()
        {
            _estado = Estado.Casa;
            _abajo.gameObject.SetActive(true);
            Elegir(_carteles[CasaDelMenu.Cuarto.PartidaRapida].Boton.gameObject);
        }

        private void ElegirCuarto(CasaDelMenu.Cuarto cuarto)
        {
            if (_estado != Estado.Casa) return;
            if (cuarto != CasaDelMenu.Cuarto.PartidaRapida)
            {
                _carteles[cuarto].Sacudida = 0.5f;
                Avisar($"{Nombre(cuarto)}: ¡próximamente!");
                return;
            }
            _estado = Estado.Niveles;
            _abajo.gameObject.SetActive(false);
            Elegir(_botonesDeNivel[Mathf.Clamp(EleccionDePartida.UltimoNivel, 0, _botonesDeNivel.Length - 1)].gameObject);
        }

        private void AbrirOpciones()
        {
            if (_estado != Estado.Casa) return;
            _estado = Estado.Opciones;
            _abajo.gameObject.SetActive(false);
            _opciones.Mostrar();
        }

        private void VolverALaCasa() => MostrarCasa();

        private void VolverAtras()
        {
            if (_estado == Estado.Opciones) _opciones.Cerrar();
            else if (_estado == Estado.Niveles) MostrarCasa();
        }

        private void Jugar(int nivel)
        {
            if (_estado != Estado.Niveles) return;
            _estado = Estado.Cargando;
            EleccionDePartida.ElegirNivel(nivel);
            _niveles.gameObject.SetActive(false);
            Avisar("¡A jugar!", 10f);
            SceneManager.LoadSceneAsync(EleccionDePartida.EscenaDeLaPartida);
        }

        private static void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static bool Atras()
        {
            var teclado = Keyboard.current;
            var joystick = Gamepad.current;
            return (teclado != null && teclado.escapeKey.wasPressedThisFrame) || (joystick != null && joystick.buttonEast.wasPressedThisFrame);
        }

        private void Avisar(string texto, float segundos = 1.8f)
        {
            _aviso.text = texto;
            _avisoHasta = Time.unscaledTime + segundos;
        }

        private static void Elegir(GameObject objeto)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(objeto);
        }

        private static string Nombre(CasaDelMenu.Cuarto cuarto)
        {
            switch (cuarto)
            {
                case CasaDelMenu.Cuarto.Historia: return "Historia";
                case CasaDelMenu.Cuarto.PartidaRapida: return "Partida rápida";
                case CasaDelMenu.Cuarto.Copas: return "Copas";
                case CasaDelMenu.Cuarto.Online: return "Online";
                default: return "Garaje";
            }
        }

        // ------------------------------------------------------------------ cámara

        /// <summary>Se hamaca frente a la casa, o viaja hasta el living (con un movimiento suave de cine).</summary>
        private void MoverCamara()
        {
            if (camara == null || casa == null) return;
            float t = Time.unscaledTime;
            var centro = casa.transform.TransformPoint(0f, 3.6f, 0f);
            var desdeLejos = centro + Quaternion.Euler(0f, Mathf.Sin(t * 0.17f) * 12f, 0f) * casa.transform.TransformVector(0f, 6.5f, -31f);
            var rotacionLejos = Quaternion.LookRotation(centro - desdeLejos);

            // De cerca, el living queda a la izquierda para que a la derecha entre el panel de los niveles.
            var living = casa.Centro(CasaDelMenu.Cuarto.PartidaRapida);
            var mira = living + casa.transform.TransformVector(2.3f, 0.3f, 0f);
            var desdeCerca = living + casa.transform.TransformVector(1.2f + Mathf.Sin(t * 0.3f) * 0.3f, 1.4f, -8.5f);
            var rotacionCerca = Quaternion.LookRotation(mira - desdeCerca);

            float s = Mathf.SmoothStep(0f, 1f, _viaje);
            camara.transform.SetPositionAndRotation(Vector3.Lerp(desdeLejos, desdeCerca, s), Quaternion.Slerp(rotacionLejos, rotacionCerca, s));
        }

        // ------------------------------------------------------------------ armado

        private void ArmarLogo()
        {
            var logo = Piezas.Texto("Logo", _lienzo, 130, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(760f, 180f), new Vector2(430f, -120f));
            logo.text = "BichiKart!";
            logo.color = Naranja;
            var borde = logo.GetComponent<Outline>();
            borde.effectColor = new Color(0.36f, 0.16f, 0.06f);
            borde.effectDistance = new Vector2(6f, -6f);
            var sombra = logo.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0.1f, 0.05f, 0.2f, 0.6f);
            sombra.effectDistance = new Vector2(8f, -12f);
            _logo = logo.rectTransform;

            var version = Piezas.Texto("Versión", _lienzo, 22, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(500f, 40f), new Vector2(280f, 40f));
            version.text = "Prototipo · etapa 2";
            version.color = new Color(1f, 1f, 1f, 0.6f);
        }

        private void ArmarCarteles()
        {
            foreach (CasaDelMenu.Cuarto cuarto in System.Enum.GetValues(typeof(CasaDelMenu.Cuarto)))
            {
                var lugar = Piezas.Imagen($"Cartel {Nombre(cuarto)}", _lienzo, null, Color.clear, new Vector2(0.5f, 0.5f), new Vector2(330f, 80f), Vector2.zero);
                var flecha = Piezas.Imagen("Flecha", lugar, Dibujos.Flecha, Crema, new Vector2(0.5f, 0.5f), new Vector2(46f, 46f), Vector2.zero);
                Piezas.Borde(flecha, Piezas.Tinta, 3f);
                var listo = cuarto == CasaDelMenu.Cuarto.PartidaRapida;
                var boton = Piezas.Boton("Tabla", lugar, Nombre(cuarto).ToUpper(), Madera, new Vector2(0.5f, 0.5f), new Vector2(listo ? 340f : 290f, listo ? 84f : 72f), Vector2.zero,
                    () => ElegirCuarto(cuarto), listo ? 38 : 32);
                boton.GetComponentInChildren<Text>().color = listo ? new Color(0.75f, 1f, 0.6f) : Crema;
                _carteles[cuarto] = new Cartel { Lugar = lugar, Flecha = flecha, Boton = boton };
            }
        }

        /// <summary>Cada cartel sigue a su cuarto en la pantalla y la flecha apunta al cuarto.</summary>
        private void ActualizarCartel(CasaDelMenu.Cuarto cuarto, Cartel cartel, bool visible)
        {
            cartel.Lugar.gameObject.SetActive(visible);
            if (!visible || camara == null || casa == null) return;

            var donde = camara.WorldToScreenPoint(casa.Cartel(cuarto));
            var adonde = camara.WorldToScreenPoint(casa.Centro(cuarto));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_lienzo, donde, null, out var lugar);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_lienzo, adonde, null, out var objetivo);
            cartel.Lugar.localPosition = lugar;

            var direccion = (objetivo - lugar).normalized;
            float distancia = Mathf.Min((objetivo - lugar).magnitude * 0.6f, 200f);
            float hamaca = Mathf.Sin(Time.unscaledTime * 4f + (int)cuarto) * 6f;
            cartel.Flecha.localPosition = direccion * Mathf.Max(distancia + hamaca, 120f);
            cartel.Flecha.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f);

            cartel.Sacudida = Mathf.Max(0f, cartel.Sacudida - Time.unscaledDeltaTime);
            cartel.Boton.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 40f) * 8f * cartel.Sacudida);
        }

        private void ArmarBotonesDeAbajo()
        {
            _abajo = Piezas.Imagen("Abajo", _lienzo, null, Color.clear, new Vector2(1f, 0f), new Vector2(600f, 120f), new Vector2(-330f, 80f));
            Piezas.Boton("Opciones", _abajo, "OPCIONES", Azul, new Vector2(0.5f, 0.5f), new Vector2(270f, 84f), new Vector2(-145f, 0f), AbrirOpciones, 34);
            Piezas.Boton("Salir", _abajo, "SALIR", Rojo, new Vector2(0.5f, 0.5f), new Vector2(230f, 84f), new Vector2(150f, 0f), Salir, 34);
        }

        private void ArmarNiveles()
        {
            _niveles = Piezas.Imagen("Partida rápida", _lienzo, Dibujos.Redondeado, Piezas.Panel, new Vector2(1f, 0.5f), new Vector2(640f, 700f), new Vector2(-400f, 0f));
            Piezas.Borde(_niveles, Color.white, 4f);
            Piezas.Texto("Título", _niveles, 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(600f, 80f), new Vector2(0f, -65f)).text = "Partida rápida";
            Piezas.Texto("Pregunta", _niveles, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(600f, 50f), new Vector2(0f, -125f)).text = "¿Qué tan difíciles son los rivales?";

            var colores = new[] { Verde, Azul, Rojo };
            var detalles = new[] { "Lentos y distraídos", "Te dan pelea", "No perdonan" };
            _botonesDeNivel = new Button[Partida.NombresDeNivel.Length];
            for (int i = 0; i < _botonesDeNivel.Length; i++)
            {
                int nivel = i;
                string texto = $"{Partida.NombresDeNivel[i]}\n<size=24>{detalles[i]}</size>";
                _botonesDeNivel[i] = Piezas.Boton(Partida.NombresDeNivel[i], _niveles, texto, colores[i], new Vector2(0.5f, 1f), new Vector2(480f, 104f), new Vector2(0f, -235f - i * 124f),
                    () => Jugar(nivel), 42);
            }
            Piezas.Boton("Volver", _niveles, "Volver", new Color(0.42f, 0.44f, 0.56f), new Vector2(0.5f, 0f), new Vector2(260f, 70f), new Vector2(0f, 70f), MostrarCasa, 32);
            _niveles.gameObject.SetActive(false);
        }
    }
}
