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
    /// las paredes o va por el techo; con el anti-mareo ese giro es más lento. Para arriba se puede mirar
    /// desde casi encima del kart; para abajo la cámara llega al ras del piso y, si se sigue, levanta la
    /// mirada hacia el techo. La sensibilidad, invertir vertical y el anti-mareo salen de las
    /// <see cref="Opciones"/>.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public sealed class CamaraDelKart : MonoBehaviour
    {
        [Tooltip("Grados por segundo que gira la cámara con el stick a fondo.")]
        [SerializeField] private float sensibilidadJoystick = 140f;

        [Tooltip("Grados de mouse, pasado el límite de abajo, para levantar la mirada del todo.")]
        [SerializeField] private float gradosParaMirarArriba = 60f;

        [Tooltip("Cuánto sube el punto al que mira la cámara cuando se mira para arriba del todo.")]
        [SerializeField] private float subidaDeLaMirada = 10f;

        [Tooltip("Si la cámara se le acerca al kart (por ejemplo, empujada por una pared), el kart se desvanece: se ve entero a esta distancia o más y desaparece a la segunda.")]
        [SerializeField] private float distanciaVisible = 4.5f, distanciaInvisible = 1.5f;

        [Tooltip("Segundos que tarda la cámara en acompañar el giro del kart (normal y con anti-mareo).")]
        [SerializeField] private float suavizadoGiro = 0.25f, suavizadoAntiMareo = 1f;

        [Tooltip("Distancia de la cámara al kart cuando viaja por adentro de un caño.")]
        [SerializeField] private float distanciaEnCanio = 2.6f;

        private CinemachineOrbitalFollow _orbita;
        private CinemachineRotationComposer _mira;
        private Vector3 _miraNormal;
        private Kart _kart;
        private ModeloKart _modelo;
        private Camera _camara;
        private float _distanciaNormal;
        private bool _centrando;

        /// <summary>De 0 a 1: cuánto se levantó la mirada hacia el techo.</summary>
        private float _mirarArriba;
        private float _ultimoGiro;

        private void Awake()
        {
            _orbita = GetComponent<CinemachineOrbitalFollow>();
            _mira = GetComponent<CinemachineRotationComposer>();
            if (_mira != null) _miraNormal = _mira.TargetOffset;
            _distanciaNormal = _orbita.Radius;
            var objetivo = GetComponent<CinemachineCamera>().Target.TrackingTarget;
            if (objetivo != null) _kart = objetivo.GetComponentInParent<Kart>();
            if (_kart != null && _kart.Chasis != null) _modelo = _kart.Chasis.GetComponent<ModeloKart>();
        }

        /// <summary>
        /// El kart de esta cámara (solo ese) se desvanece de a poco cuanto más se le acerca la cámara, para que
        /// no tape la vista cuando una pared o el piso la empujan contra él.
        /// </summary>
        private void LateUpdate()
        {
            if (_modelo == null) return;
            if (_camara == null) _camara = Camera.main;
            if (_camara == null) return;
            var centro = _kart.Chasis.position + _kart.Arriba * 0.6f;
            float distancia = Vector3.Distance(_camara.transform.position, centro);
            _modelo.Alfa = Mathf.InverseLerp(distanciaInvisible, distanciaVisible, distancia);
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
                _ultimoGiro = Time.unscaledTime;
                _orbita.HorizontalAxis.Value = _orbita.HorizontalAxis.ClampValue(_orbita.HorizontalAxis.Value + giro.x);
                MoverVertical(Opciones.InvertirVertical ? giro.y : -giro.y);
            }
            Recentrar(mouse, joystick);
            if (_mira != null) _mira.TargetOffset = _miraNormal + Vector3.up * (_mirarArriba * subidaDeLaMirada);
        }

        /// <summary>
        /// Sube o baja la cámara. Más abajo del límite del eje (al ras del piso), en vez de meterla debajo del
        /// piso (que la empuja contra el kart y no deja ver nada), levanta la mirada hacia el techo.
        /// </summary>
        private void MoverVertical(float grados)
        {
            if (grados < 0f)
            {
                float hastaElPiso = Mathf.Max(0f, _orbita.VerticalAxis.Value - _orbita.VerticalAxis.Range.x);
                float bajar = Mathf.Min(-grados, hastaElPiso);
                _orbita.VerticalAxis.Value -= bajar;
                _mirarArriba = Mathf.Clamp01(_mirarArriba + (-grados - bajar) / gradosParaMirarArriba);
            }
            else
            {
                // Primero baja la mirada; después sube la cámara.
                float bajarMirada = Mathf.Min(grados, _mirarArriba * gradosParaMirarArriba);
                _mirarArriba -= bajarMirada / gradosParaMirarArriba;
                _orbita.VerticalAxis.Value = _orbita.VerticalAxis.ClampValue(_orbita.VerticalAxis.Value + grados - bajarMirada);
            }
        }

        /// <summary>
        /// La cámara vuelve atrás del kart solo mientras se maneja: quieto, la espera vuelve a empezar y la
        /// cámara queda donde la dejaste. La ruedita del mouse o el stick derecho apretado la centran ya.
        /// </summary>
        private void Recentrar(Mouse mouse, Gamepad joystick)
        {
            bool centrarYa = (mouse != null && mouse.middleButton.wasPressedThisFrame)
                || (joystick != null && joystick.rightStickButton.wasPressedThisFrame);
            var ordenes = _kart != null ? _kart.UltimasOrdenes : default;
            bool manejando = Mathf.Abs(ordenes.Avance) > 0.1f || Mathf.Abs(ordenes.Giro) > 0.1f || (_kart != null && _kart.EnCanio);

            // La mirada vuelve a la altura normal cuando vuelve la cámara (manejando sin tocar el mouse, o al centrarla).
            bool volverMirada = centrarYa || _centrando || (manejando && Time.unscaledTime - _ultimoGiro > _orbita.VerticalAxis.Recentering.Wait);
            if (volverMirada) _mirarArriba = Mathf.MoveTowards(_mirarArriba, 0f, Time.unscaledDeltaTime / 0.6f);

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
