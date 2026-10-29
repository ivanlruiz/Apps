using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichiKart
{
    /// <summary>
    /// Mueve con el mouse (o el stick derecho del joystick) la cámara que va detrás del kart, una vuelta
    /// entera si hace falta. Mientras se maneja (acelerando o doblando) y no se toca el mouse, Cinemachine
    /// la vuelve sola atrás del kart después de la espera del eje (1,5 s); quieto, queda donde la dejaste.
    /// El clic de la ruedita (o apretar el stick derecho) la centra enseguida. Para usar el mouse hay que
    /// hacer clic en el juego; en pausa el mouse queda suelto. La cámara gira con el kart cuando sube por
    /// las paredes o va por el techo; con el anti-mareo ese giro es más lento. La sensibilidad, invertir
    /// vertical y el anti-mareo salen de las <see cref="Opciones"/>.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public sealed class CamaraDelKart : MonoBehaviour
    {
        [Tooltip("Grados por segundo que gira la cámara con el stick a fondo.")]
        [SerializeField] private float sensibilidadJoystick = 140f;

        [Tooltip("Segundos que tarda la cámara en acompañar el giro del kart (normal y con anti-mareo).")]
        [SerializeField] private float suavizadoGiro = 0.25f, suavizadoAntiMareo = 1f;

        [Tooltip("Distancia de la cámara al kart cuando viaja por adentro de un caño.")]
        [SerializeField] private float distanciaEnCanio = 2.6f;

        private CinemachineOrbitalFollow _orbita;
        private Kart _kart;
        private float _distanciaNormal;
        private bool _centrando;

        private void Awake()
        {
            _orbita = GetComponent<CinemachineOrbitalFollow>();
            _distanciaNormal = _orbita.Radius;
            var objetivo = GetComponent<CinemachineCamera>().Target.TrackingTarget;
            if (objetivo != null) _kart = objetivo.GetComponentInParent<Kart>();
        }

        private void OnDisable() => SoltarMouse();

        private void Update()
        {
            if (Pausa.Activa)
            {
                SoltarMouse();
                return;
            }
            AplicarAntiMareo();
            AtraparMouse();

            // Adentro de un caño, la cámara va pegada atrás del kart, por dentro.
            float distancia = _kart != null && _kart.EnCanio ? distanciaEnCanio : _distanciaNormal;
            _orbita.Radius = Mathf.Lerp(_orbita.Radius, distancia, 1f - Mathf.Exp(-8f * Time.deltaTime));

            Vector2 giro = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked) giro += mouse.delta.ReadValue() * Opciones.GradosPorPunto;
            var joystick = Gamepad.current;
            if (joystick != null) giro += joystick.rightStick.ReadValue() * (sensibilidadJoystick * Time.deltaTime);

            if (giro != Vector2.zero)
            {
                // Cinemachine nota el cambio de valor y espera a que se suelte para volver la cámara atrás.
                _centrando = false;
                _orbita.HorizontalAxis.Value = _orbita.HorizontalAxis.ClampValue(_orbita.HorizontalAxis.Value + giro.x);
                float vertical = Opciones.InvertirVertical ? giro.y : -giro.y;
                _orbita.VerticalAxis.Value = _orbita.VerticalAxis.ClampValue(_orbita.VerticalAxis.Value + vertical);
            }
            Recentrar(mouse, joystick);
        }

        /// <summary>
        /// La cámara vuelve atrás del kart solo mientras se maneja: quieto, la espera vuelve a empezar y la
        /// cámara queda donde la dejaste. La ruedita del mouse o el stick derecho apretado la centran ya.
        /// </summary>
        private void Recentrar(Mouse mouse, Gamepad joystick)
        {
            bool centrarYa = (mouse != null && mouse.middleButton.wasPressedThisFrame)
                || (joystick != null && joystick.rightStickButton.wasPressedThisFrame);
            if (centrarYa)
            {
                _centrando = true;
                _orbita.HorizontalAxis.TriggerRecentering();
                _orbita.VerticalAxis.TriggerRecentering();
            }
            if (_centrando)
            {
                // Termina cuando llegó (o cuando se movió el mouse, más arriba).
                _centrando = Mathf.Abs(Mathf.DeltaAngle(_orbita.HorizontalAxis.Value, _orbita.HorizontalAxis.Center)) > 0.5f
                    || Mathf.Abs(_orbita.VerticalAxis.Value - _orbita.VerticalAxis.Center) > 0.5f;
                return;
            }

            var ordenes = _kart != null ? _kart.UltimasOrdenes : default;
            bool manejando = Mathf.Abs(ordenes.Avance) > 0.1f || Mathf.Abs(ordenes.Giro) > 0.1f || (_kart != null && _kart.EnCanio);
            if (manejando) return;
            _orbita.HorizontalAxis.CancelRecentering();
            _orbita.VerticalAxis.CancelRecentering();
        }

        private void AplicarAntiMareo()
        {
            var seguimiento = _orbita.TrackerSettings;
            float buscado = Opciones.AntiMareo ? suavizadoAntiMareo : suavizadoGiro;
            if (Mathf.Approximately(seguimiento.QuaternionDamping, buscado)) return;
            seguimiento.QuaternionDamping = buscado;
            _orbita.TrackerSettings = seguimiento;
        }

        private static void AtraparMouse()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static void SoltarMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
