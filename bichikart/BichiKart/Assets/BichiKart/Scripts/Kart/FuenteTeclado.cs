using UnityEngine;
using UnityEngine.InputSystem;

namespace BichiKart
{
    /// <summary>
    /// Órdenes del jugador desde el teclado o el joystick.
    /// W/S o flechas: acelerar y frenar. A/D o flechas: doblar. Espacio: saltar. Shift: derrapar.
    /// Ctrl: gravedad. Q: bocina. F: usar (meterse en un caño).
    /// </summary>
    public sealed class FuenteTeclado : MonoBehaviour, IFuenteDeOrdenes
    {
        private InputAction _avance, _giro, _saltar, _derrapar, _gravedad, _bocina, _usar;
        private bool _saltarApretado, _gravedadApretada, _bocinaApretada, _usarApretado;

        private void Awake()
        {
            _avance = new InputAction("Avance", InputActionType.Value);
            Eje(_avance, "<Keyboard>/s", "<Keyboard>/w");
            Eje(_avance, "<Keyboard>/downArrow", "<Keyboard>/upArrow");
            Eje(_avance, "<Gamepad>/leftTrigger", "<Gamepad>/rightTrigger");

            _giro = new InputAction("Giro", InputActionType.Value);
            Eje(_giro, "<Keyboard>/a", "<Keyboard>/d");
            Eje(_giro, "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            _giro.AddBinding("<Gamepad>/leftStick/x");

            _saltar = Boton("Saltar", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            _derrapar = Boton("Derrapar", "<Keyboard>/leftShift", "<Keyboard>/rightShift", "<Gamepad>/rightShoulder");
            _gravedad = Boton("Gravedad", "<Keyboard>/leftCtrl", "<Keyboard>/rightCtrl", "<Gamepad>/buttonNorth");
            _bocina = Boton("Bocina", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            _usar = Boton("Usar", "<Keyboard>/f", "<Gamepad>/buttonWest");
        }

        private void OnEnable()
        {
            foreach (var accion in Acciones()) accion.Enable();
        }

        private void OnDisable()
        {
            foreach (var accion in Acciones()) accion.Disable();
        }

        private void OnDestroy()
        {
            foreach (var accion in Acciones()) accion.Dispose();
        }

        private void Update()
        {
            // Los botones se anotan acá y se entregan en el próximo paso de física, que puede no
            // ocurrir en este cuadro o ocurrir varias veces. En pausa no se anotan.
            if (Pausa.Activa) return;
            _saltarApretado |= _saltar.WasPressedThisFrame();
            _gravedadApretada |= _gravedad.WasPressedThisFrame();
            _bocinaApretada |= _bocina.WasPressedThisFrame();
            _usarApretado |= _usar.WasPressedThisFrame();
        }

        public OrdenesDelKart LeerOrdenes()
        {
            var ordenes = new OrdenesDelKart
            {
                Avance = Mathf.Clamp(_avance.ReadValue<float>(), -1f, 1f),
                Giro = Mathf.Clamp(_giro.ReadValue<float>(), -1f, 1f),
                Saltar = _saltarApretado,
                Derrapar = _derrapar.IsPressed(),
                Gravedad = _gravedadApretada,
                Bocina = _bocinaApretada,
                // Usar vale apretado o mantenido: se puede llegar a la boca con la F ya apretada.
                Usar = _usarApretado || _usar.IsPressed(),
            };
            _saltarApretado = _gravedadApretada = _bocinaApretada = _usarApretado = false;
            return ordenes;
        }

        private InputAction[] Acciones() => new[] { _avance, _giro, _saltar, _derrapar, _gravedad, _bocina, _usar };

        private static void Eje(InputAction accion, string negativo, string positivo)
        {
            accion.AddCompositeBinding("1DAxis").With("Negative", negativo).With("Positive", positivo);
        }

        private static InputAction Boton(string nombre, params string[] teclas)
        {
            var accion = new InputAction(nombre, InputActionType.Button);
            foreach (var tecla in teclas) accion.AddBinding(tecla);
            return accion;
        }
    }
}
