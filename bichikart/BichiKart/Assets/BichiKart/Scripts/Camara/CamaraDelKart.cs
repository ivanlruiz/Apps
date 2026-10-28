using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichiKart
{
    /// <summary>
    /// Mueve con el mouse (o el stick derecho del joystick) la cámara que va detrás del kart. Al soltar,
    /// Cinemachine la vuelve sola atrás del kart. Para usar el mouse hay que hacer clic en el juego;
    /// Escape suelta el mouse.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public sealed class CamaraDelKart : MonoBehaviour
    {
        [Tooltip("Grados que gira la cámara por cada punto que se mueve el mouse.")]
        [SerializeField] private float sensibilidadMouse = 0.12f;

        [Tooltip("Grados por segundo que gira la cámara con el stick a fondo.")]
        [SerializeField] private float sensibilidadJoystick = 140f;

        [SerializeField] private bool invertirVertical;

        private CinemachineOrbitalFollow _orbita;

        private void Awake() => _orbita = GetComponent<CinemachineOrbitalFollow>();

        private void OnDisable() => SoltarMouse();

        private void Update()
        {
            AtraparOSoltarMouse();

            Vector2 giro = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked) giro += mouse.delta.ReadValue() * sensibilidadMouse;
            var joystick = Gamepad.current;
            if (joystick != null) giro += joystick.rightStick.ReadValue() * (sensibilidadJoystick * Time.deltaTime);
            if (giro == Vector2.zero) return;

            // Cinemachine nota el cambio de valor y espera a que se suelte para volver la cámara atrás.
            _orbita.HorizontalAxis.Value = _orbita.HorizontalAxis.ClampValue(_orbita.HorizontalAxis.Value + giro.x);
            float vertical = invertirVertical ? giro.y : -giro.y;
            _orbita.VerticalAxis.Value = _orbita.VerticalAxis.ClampValue(_orbita.VerticalAxis.Value + vertical);
        }

        private static void AtraparOSoltarMouse()
        {
            var teclado = Keyboard.current;
            var mouse = Mouse.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) SoltarMouse();
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
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
