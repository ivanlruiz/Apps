using Bichikarts.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Bichikarts
{
    /// <summary>
    /// Junta teclado, joysticks y controles táctiles en un <see cref="KartInput"/> por jugador.
    /// Funciona con el Input System nuevo, con el viejo, o con los dos activados.
    ///
    /// Jugador 1: WASD + E (también Q, F, Espacio o Shift izquierdo) para la gravedad.
    /// Jugador 2: flechas + Enter (también Shift derecho, Ctrl derecho, 0 del teclado numérico, punto o L).
    /// Joystick: palanca o cruz para moverse, A/X para la gravedad, B/Y para saltar, Start para pausar.
    /// </summary>
    public sealed class InputHub
    {
        /// <summary>Si es false, el jugador 1 puede usar los dos lados del teclado y todos los joysticks.</summary>
        public bool TwoPlayers;

        public TouchControls Touch;

        private readonly bool[] _flip = new bool[2];
        private readonly bool[] _jump = new bool[2];
        private readonly int[] _x = new int[2];
        private readonly int[] _y = new int[2];

        public bool PausePressed { get; private set; }
        public bool AnyPressed { get; private set; }

        /// <summary>Lee los dispositivos. Llamar una vez por cuadro, antes de avanzar la simulación.</summary>
        public void Poll()
        {
            PausePressed = false;
            AnyPressed = false;
            for (int p = 0; p < 2; p++) { _x[p] = 0; _y[p] = 0; }

            ReadKeyboard();
            ReadGamepads();

            if (Touch != null)
            {
                for (int p = 0; p < 2; p++)
                {
                    var t = Touch.Read(p, TwoPlayers);
                    if (t.X != 0) _x[p] = t.X;
                    if (t.Y != 0) _y[p] = t.Y;
                    if (t.Flip) _flip[p] = true;
                }
            }
        }

        public KartInput Read(int player)
        {
            return new KartInput { X = _x[player], Y = _y[player], Flip = _flip[player], Jump = _jump[player] };
        }

        /// <summary>Marca como usados los botones apretados (se llama después de cada paso de simulación).</summary>
        public void ConsumeEdges()
        {
            for (int p = 0; p < 2; p++) { _flip[p] = false; _jump[p] = false; }
        }

        private void Combine(int player, int left, int right, int up, int down)
        {
            int x = right - left, y = down - up;
            if (x != 0) _x[player] = x;
            if (y != 0) _y[player] = y;
        }

        private int P2 => TwoPlayers ? 1 : 0;

        private void ReadKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                Combine(0, H(kb, Key.A), H(kb, Key.D), H(kb, Key.W), H(kb, Key.S));
                Combine(P2, H(kb, Key.LeftArrow), H(kb, Key.RightArrow), H(kb, Key.UpArrow), H(kb, Key.DownArrow));
                if (P(kb, Key.E) || P(kb, Key.Q) || P(kb, Key.F) || P(kb, Key.Space) || P(kb, Key.LeftShift)) _flip[0] = true;
                if (P(kb, Key.Enter) || P(kb, Key.NumpadEnter) || P(kb, Key.RightShift) || P(kb, Key.RightCtrl) ||
                    P(kb, Key.Numpad0) || P(kb, Key.Period) || P(kb, Key.L)) _flip[P2] = true;
                if (P(kb, Key.Escape) || P(kb, Key.P)) PausePressed = true;
                if (kb.anyKey.wasPressedThisFrame) AnyPressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            Combine(0, LH(KeyCode.A), LH(KeyCode.D), LH(KeyCode.W), LH(KeyCode.S));
            Combine(P2, LH(KeyCode.LeftArrow), LH(KeyCode.RightArrow), LH(KeyCode.UpArrow), LH(KeyCode.DownArrow));
            if (LP(KeyCode.E) || LP(KeyCode.Q) || LP(KeyCode.F) || LP(KeyCode.Space) || LP(KeyCode.LeftShift)) _flip[0] = true;
            if (LP(KeyCode.Return) || LP(KeyCode.KeypadEnter) || LP(KeyCode.RightShift) || LP(KeyCode.RightControl) ||
                LP(KeyCode.Keypad0) || LP(KeyCode.Period) || LP(KeyCode.L)) _flip[P2] = true;
            if (LP(KeyCode.Escape) || LP(KeyCode.P)) PausePressed = true;
            if (Input.anyKeyDown) AnyPressed = true;
#endif
        }

        private void ReadGamepads()
        {
#if ENABLE_INPUT_SYSTEM
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count && i < 4; i++)
            {
                var pad = pads[i];
                int player = TwoPlayers && i >= 1 ? 1 : 0;
                Vector2 v = pad.leftStick.ReadValue() + pad.dpad.ReadValue();
                int x = v.x > 0.5f ? 1 : v.x < -0.5f ? -1 : 0;
                int y = v.y > 0.5f ? -1 : v.y < -0.5f ? 1 : 0;
                if (x != 0) _x[player] = x;
                if (y != 0) _y[player] = y;
                if (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                    pad.rightShoulder.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame) _flip[player] = true;
                if (pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame) _jump[player] = true;
                if (pad.startButton.wasPressedThisFrame) PausePressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (LP(KeyCode.Joystick1Button0) || LP(KeyCode.Joystick1Button2)) _flip[0] = true;
            if (LP(KeyCode.Joystick1Button1) || LP(KeyCode.Joystick1Button3)) _jump[0] = true;
            if (LP(KeyCode.Joystick2Button0) || LP(KeyCode.Joystick2Button2)) _flip[P2] = true;
            if (LP(KeyCode.Joystick2Button1) || LP(KeyCode.Joystick2Button3)) _jump[P2] = true;
            if (LP(KeyCode.Joystick1Button7) || LP(KeyCode.Joystick2Button7)) PausePressed = true;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static int H(Keyboard kb, Key k) => kb[k].isPressed ? 1 : 0;
        private static bool P(Keyboard kb, Key k) => kb[k].wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        private static int LH(KeyCode k) => Input.GetKey(k) ? 1 : 0;
        private static bool LP(KeyCode k) => Input.GetKeyDown(k);
#endif
    }
}
