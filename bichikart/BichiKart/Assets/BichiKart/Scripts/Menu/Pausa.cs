using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BichiKart
{
    /// <summary>
    /// El menú de pausa de la partida. Escape (o Start en el joystick) frena el juego, suelta el mouse y
    /// muestra Seguir, Opciones y Volver al menú. En pausa el tiempo del juego no corre y no suena nada.
    /// </summary>
    public sealed class Pausa : MonoBehaviour
    {
        /// <summary>Hay pausa: la cámara y el teclado del kart no hacen caso.</summary>
        public static bool Activa { get; private set; }

        private RectTransform _lienzo, _principal;
        private PanelDeOpciones _opciones;
        private Button _seguir;

        private void Start()
        {
            Piezas.AsegurarEventSystem();
            _lienzo = Piezas.Lienzo("Pantalla de pausa", 10, true);
            _lienzo.SetParent(transform, false);

            // Fondo oscuro sobre todo el juego.
            var fondo = Piezas.Imagen("Fondo", _lienzo, null, Color.clear, Vector2.zero, Vector2.zero, Vector2.zero);
            fondo.anchorMin = Vector2.zero;
            fondo.anchorMax = Vector2.one;
            fondo.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.14f, 0.55f);

            _principal = Piezas.Imagen("Pausa", _lienzo, Dibujos.Redondeado, Piezas.Panel, new Vector2(0.5f, 0.5f), new Vector2(560f, 540f), Vector2.zero);
            Piezas.Borde(_principal, Color.white, 4f);
            Piezas.Texto("Título", _principal, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(500f, 90f), new Vector2(0f, -75f)).text = "Pausa";
            _seguir = Piezas.Boton("Seguir", _principal, "Seguir", new Color(0.25f, 0.7f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(400f, 86f), new Vector2(0f, 50f), Seguir, 38);
            Piezas.Boton("Opciones", _principal, "Opciones", new Color(0.24f, 0.48f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(400f, 86f), new Vector2(0f, -55f), AbrirOpciones, 38);
            Piezas.Boton("Volver al menú", _principal, "Volver al menú", new Color(0.89f, 0.28f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(400f, 86f), new Vector2(0f, -160f), VolverAlMenu, 38);

            _opciones = PanelDeOpciones.Crear(_lienzo, MostrarPrincipal);
            _lienzo.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Activa) Soltar();
        }

        private void Update()
        {
            var teclado = Keyboard.current;
            var joystick = Gamepad.current;
            bool tecla = (teclado != null && teclado.escapeKey.wasPressedThisFrame) || (joystick != null && joystick.startButton.wasPressedThisFrame);
            if (!tecla) return;

            if (!Activa) Pausar();
            else if (_opciones.Visible) _opciones.Cerrar();
            else Seguir();
        }

        private void Pausar()
        {
            Activa = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _lienzo.gameObject.SetActive(true);
            MostrarPrincipal();
        }

        private void MostrarPrincipal()
        {
            _principal.gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_seguir.gameObject);
        }

        private void AbrirOpciones()
        {
            _principal.gameObject.SetActive(false);
            _opciones.Mostrar();
        }

        private void Seguir()
        {
            Soltar();
            _lienzo.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void VolverAlMenu()
        {
            Soltar();
            SceneManager.LoadScene(EleccionDePartida.EscenaDelMenu);
        }

        /// <summary>Vuelve el tiempo y el sonido a la normalidad.</summary>
        private static void Soltar()
        {
            Activa = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
