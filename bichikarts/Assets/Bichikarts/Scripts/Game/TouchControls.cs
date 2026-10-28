using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bichikarts
{
    /// <summary>
    /// Controles en pantalla para celular y tablet: una cruz (se arrastra el dedo) y un botón de gravedad
    /// por jugador. Contra la compu, la cruz va a la izquierda y el botón a la derecha.
    /// </summary>
    public sealed class TouchControls
    {
        public struct State
        {
            public int X, Y;
            public bool Flip;
        }

        private sealed class Pad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            public RectTransform Knob;
            public int X, Y;
            private int _pointer = int.MinValue;

            public void OnPointerDown(PointerEventData e)
            {
                _pointer = e.pointerId;
                Track(e);
            }

            public void OnDrag(PointerEventData e)
            {
                if (e.pointerId == _pointer) Track(e);
            }

            public void OnPointerUp(PointerEventData e)
            {
                if (e.pointerId != _pointer) return;
                _pointer = int.MinValue;
                X = Y = 0;
                Knob.anchoredPosition = Vector2.zero;
            }

            private void OnDisable()
            {
                _pointer = int.MinValue;
                X = Y = 0;
                if (Knob != null) Knob.anchoredPosition = Vector2.zero;
            }

            private void Track(PointerEventData e)
            {
                var rt = (RectTransform)transform;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
                float radius = rt.rect.width * 0.5f;
                var v = Vector2.ClampMagnitude(local, radius * 0.55f);
                Knob.anchoredPosition = v;
                if (local.magnitude < radius * 0.2f) { X = Y = 0; return; }
                float a = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
                int sector = Mathf.RoundToInt(a / 45f);
                sector = ((sector % 8) + 8) % 8;
                int[] sx = { 1, 1, 0, -1, -1, -1, 0, 1 };
                int[] sy = { 0, -1, -1, -1, 0, 1, 1, 1 };
                X = sx[sector];
                Y = sy[sector];
            }
        }

        private sealed class FlipButton : MonoBehaviour, IPointerDownHandler
        {
            public bool Pressed;
            public Image Face;

            public void OnPointerDown(PointerEventData e)
            {
                Pressed = true;
                transform.localScale = Vector3.one * 0.9f;
            }

            private void Update()
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.unscaledDeltaTime * 12f);
            }
        }

        private readonly RectTransform _root;
        private readonly Pad[] _pads = new Pad[2];
        private readonly FlipButton[] _buttons = new FlipButton[2];

        public bool Visible => _root.gameObject.activeSelf;

        public TouchControls(Transform canvas)
        {
            _root = Ui.Fill(Ui.Node(canvas, "Controles táctiles"));
            for (int p = 0; p < 2; p++)
            {
                _pads[p] = MakePad(p);
                _buttons[p] = MakeButton(p);
            }
            _root.gameObject.SetActive(false);
        }

        private Pad MakePad(int player)
        {
            var bg = Ui.Image(_root, Art.UiCircle(), new Color(1f, 1f, 1f, 0.12f), "Cruz J" + (player + 1));
            bg.raycastTarget = true;
            var ring = Ui.Image(bg.transform, Art.UiRing(), new Color(1f, 1f, 1f, 0.35f), "Borde");
            Ui.Fill(ring.rectTransform);
            for (int i = 0; i < 4; i++)
            {
                var arrow = Ui.Image(bg.transform, Art.UiTriangle(), new Color(1f, 1f, 1f, 0.45f), "Flecha");
                float a = i * 90f;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Ui.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dir * 72f, new Vector2(34, 34));
                arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, a + 90f);
            }
            var knob = Ui.Image(bg.transform, Art.UiCircle(), Palette.WithAlpha(Palette.Player(player), 0.8f), "Perilla");
            Ui.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
            var pad = bg.gameObject.AddComponent<Pad>();
            pad.Knob = knob.rectTransform;
            return pad;
        }

        private FlipButton MakeButton(int player)
        {
            var bg = Ui.Image(_root, Art.UiCircle(), Palette.WithAlpha(Palette.Player(player), 0.55f), "Gravedad J" + (player + 1));
            bg.raycastTarget = true;
            var ring = Ui.Image(bg.transform, Art.UiRing(), new Color(1f, 1f, 1f, 0.6f), "Borde");
            Ui.Fill(ring.rectTransform);
            var up = Ui.Image(bg.transform, Art.UiTriangle(), Color.white, "Arriba");
            Ui.Place(up.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(40, 34));
            up.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            var down = Ui.Image(bg.transform, Art.UiTriangle(), Color.white, "Abajo");
            Ui.Place(down.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(40, 34));
            var b = bg.gameObject.AddComponent<FlipButton>();
            b.Face = bg;
            return b;
        }

        public void Show(bool on, bool twoPlayers)
        {
            _root.gameObject.SetActive(on);
            if (!on) return;
            var bl = new Vector2(0, 0);
            var br = new Vector2(1, 0);
            if (twoPlayers)
            {
                Ui.Place((RectTransform)_pads[0].transform, bl, new Vector2(0.5f, 0.5f), new Vector2(150, 150), new Vector2(230, 230));
                Ui.Place((RectTransform)_buttons[0].transform, bl, new Vector2(0.5f, 0.5f), new Vector2(360, 110), new Vector2(150, 150));
                Ui.Place((RectTransform)_pads[1].transform, br, new Vector2(0.5f, 0.5f), new Vector2(-150, 150), new Vector2(230, 230));
                Ui.Place((RectTransform)_buttons[1].transform, br, new Vector2(0.5f, 0.5f), new Vector2(-360, 110), new Vector2(150, 150));
                _pads[1].gameObject.SetActive(true);
                _buttons[1].gameObject.SetActive(true);
            }
            else
            {
                Ui.Place((RectTransform)_pads[0].transform, bl, new Vector2(0.5f, 0.5f), new Vector2(170, 170), new Vector2(260, 260));
                Ui.Place((RectTransform)_buttons[0].transform, br, new Vector2(0.5f, 0.5f), new Vector2(-170, 160), new Vector2(190, 190));
                _pads[1].gameObject.SetActive(false);
                _buttons[1].gameObject.SetActive(false);
            }
        }

        public State Read(int player, bool twoPlayers)
        {
            var s = new State();
            if (!Visible || (player == 1 && !twoPlayers)) return s;
            s.X = _pads[player].X;
            s.Y = _pads[player].Y;
            s.Flip = _buttons[player].Pressed;
            _buttons[player].Pressed = false;
            return s;
        }

        /// <summary>True en celulares/tablets (o en una compu con pantalla táctil).</summary>
        public static bool DeviceHasTouch()
        {
            if (Application.isMobilePlatform) return true;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchSupported) return true;
#endif
            return false;
        }
    }
}
