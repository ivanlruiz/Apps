using Bichikarts.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bichikarts
{
    /// <summary>
    /// Lo que se ve encima de la partida: barra de cada jugador (rondas ganadas y tiempo aguantando),
    /// la ronda, la cuenta regresiva, carteles, textos flotantes y las marcas J1/J2 sobre los karting.
    /// </summary>
    public sealed class Hud
    {
        public const float BarHeight = 90f;

        private sealed class PlayerPanel
        {
            public Image Portrait, Head, BarFill, Food;
            public Text Name, Seconds;
            public Image[] Stars;
            public RectTransform Root;
        }

        private sealed class FloatingText
        {
            public Text Text;
            public Vector3 World;
            public float Life;
        }

        private readonly RectTransform _root;
        private readonly PlayerPanel[] _panels = new PlayerPanel[2];
        private readonly Text _round;
        private readonly Text _countdown;
        private readonly RectTransform _banner;
        private readonly Text _bannerText;
        private readonly FloatingText[] _popups = new FloatingText[6];
        private readonly RectTransform[] _markers = new RectTransform[2];
        private int _nextPopup;
        private float _bannerTime;
        private float _countPulse;
        private string _lastCount = "";

        public Hud(Transform canvas, UnityAction onPause)
        {
            _root = Ui.Fill(Ui.Node(canvas, "HUD"));

            var bar = Ui.Image(_root, Art.UiWhite(), new Color(0.06f, 0.03f, 0.13f, 0.82f), "Barra");
            var barRt = bar.rectTransform;
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0, BarHeight);
            var edge = Ui.Image(barRt, Art.UiWhite(), Palette.Crust, "Borde");
            edge.rectTransform.anchorMin = new Vector2(0, 0);
            edge.rectTransform.anchorMax = new Vector2(1, 0);
            edge.rectTransform.pivot = new Vector2(0.5f, 1);
            edge.rectTransform.sizeDelta = new Vector2(0, 4);

            for (int i = 0; i < 2; i++) _panels[i] = MakePanel(barRt, i);

            var pause = Ui.Button(barRt, "II", Palette.Crumb, onPause, 30);
            Ui.Place(Ui.Rt(pause), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(64, 50));
            _round = Ui.Inked(Ui.Text(barRt, "Ronda 1", 26, Palette.Crumb, true), 2);
            Ui.Place(_round.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(240, 32));

            _countdown = Ui.Inked(Ui.Text(_root, "", 220, Palette.Cheese, true), 6);
            Ui.Place(_countdown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(900, 300));

            var bannerPanel = Ui.Panel(_root, new Color(0.08f, 0.04f, 0.16f, 0.88f), "Cartel", 24);
            _banner = bannerPanel.rectTransform;
            Ui.Place(_banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 150));
            _bannerText = Ui.Inked(Ui.Text(_banner, "", 70, Palette.Cheese, true), 4);
            Ui.Fill(_bannerText.rectTransform, 20, 10, 20, 10);
            _banner.gameObject.SetActive(false);

            for (int i = 0; i < _popups.Length; i++)
            {
                var t = Ui.Inked(Ui.Text(_root, "", 44, Color.white, true), 3);
                Ui.Place(t.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 70));
                t.gameObject.SetActive(false);
                _popups[i] = new FloatingText { Text = t };
            }

            for (int i = 0; i < 2; i++)
            {
                var m = Ui.Node(_root, "Marca J" + (i + 1));
                Ui.Place(m, new Vector2(0, 0), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(70, 56));
                var bg = Ui.Panel(m, Palette.Player(i), "Fondo", 12);
                Ui.Place(bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(62, 36));
                var arrow = Ui.Image(m, Art.UiTriangle(), Palette.Player(i), "Flecha");
                Ui.Place(arrow.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(26, 22));
                var label = Ui.Text(bg.transform, "J" + (i + 1), 24, Palette.Ink, true);
                Ui.Fill(label.rectTransform);
                _markers[i] = m;
            }

            _root.gameObject.SetActive(false);
        }

        private static PlayerPanel MakePanel(RectTransform bar, int i)
        {
            bool left = i == 0;
            float sx = left ? 1f : -1f;
            var anchor = new Vector2(left ? 0 : 1, 1);
            var pivot = new Vector2(left ? 0 : 1, 1);
            var p = new PlayerPanel();
            p.Root = Ui.Node(bar, "Jugador " + (i + 1));
            Ui.Fill(p.Root);

            p.Portrait = Ui.Image(p.Root, Art.UiCircle(), Palette.Player(i), "Retrato");
            Ui.Place(p.Portrait.rectTransform, anchor, pivot, new Vector2(16 * sx, -8), new Vector2(74, 74));
            p.Head = Ui.Image(p.Portrait.transform, null, Color.white, "Cara");
            p.Head.preserveAspect = true;
            Ui.Fill(p.Head.rectTransform, 4, 2, 4, 6);

            p.Name = Ui.Inked(Ui.Text(p.Root, "", 28, Color.white, true, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight), 2);
            Ui.Place(p.Name.rectTransform, anchor, pivot, new Vector2(104 * sx, -6), new Vector2(330, 36));

            p.Stars = new Image[5];
            for (int s = 0; s < 5; s++)
            {
                p.Stars[s] = Ui.Image(p.Root, Art.Star(), Palette.Cheese, "Ronda");
                Ui.Place(p.Stars[s].rectTransform, anchor, pivot, new Vector2((440 + s * 32) * sx, -8), new Vector2(30, 30));
            }

            var barBg = Ui.Panel(p.Root, new Color(1f, 1f, 1f, 0.12f), "Tiempo", 10);
            Ui.Place(barBg.rectTransform, anchor, pivot, new Vector2(104 * sx, -50), new Vector2(380, 26));
            p.BarFill = Ui.Image(barBg.transform, Art.UiWhite(), Palette.Player(i), "Lleno");
            p.BarFill.type = Image.Type.Filled;
            p.BarFill.fillMethod = Image.FillMethod.Horizontal;
            p.BarFill.fillOrigin = left ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
            Ui.Fill(p.BarFill.rectTransform, 4, 4, 4, 4);

            p.Seconds = Ui.Inked(Ui.Text(p.Root, "", 24, Palette.Crumb, true, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight), 2);
            Ui.Place(p.Seconds.rectTransform, anchor, pivot, new Vector2(494 * sx, -46), new Vector2(110, 32));

            p.Food = Ui.Image(p.Root, null, Color.white, "Comida");
            p.Food.preserveAspect = true;
            Ui.Place(p.Food.rectTransform, anchor, pivot, new Vector2(600 * sx, -34), new Vector2(48, 48));
            return p;
        }

        public void Show(bool on) => _root.gameObject.SetActive(on);

        public void Bind(Match m, Look[] looks, string[] names, string food)
        {
            for (int i = 0; i < 2; i++)
            {
                var p = _panels[i];
                p.Head.sprite = Art.Head(looks[i]);
                p.Name.text = names[i];
                p.Food.sprite = Art.Food(food);
                for (int s = 0; s < p.Stars.Length; s++) p.Stars[s].gameObject.SetActive(s < m.Config.RoundsToWin);
            }
            _banner.gameObject.SetActive(false);
            _bannerTime = 0f;
            _countdown.text = "";
            foreach (var pop in _popups) { pop.Life = 0f; pop.Text.gameObject.SetActive(false); }
        }

        public void Banner(string text, Color color, float seconds = 2.6f)
        {
            _bannerText.text = text;
            _bannerText.color = color;
            _bannerTime = seconds;
            _banner.gameObject.SetActive(true);
            _banner.localScale = Vector3.one * 0.6f;
        }

        public void Popup(Vector3 world, string text, Color color)
        {
            var p = _popups[_nextPopup];
            _nextPopup = (_nextPopup + 1) % _popups.Length;
            p.World = world;
            p.Life = 1.1f;
            p.Text.text = text;
            p.Text.color = color;
            p.Text.gameObject.SetActive(true);
        }

        public void Update(Match m, Camera cam, float dt, float time)
        {
            for (int i = 0; i < 2; i++)
            {
                var p = _panels[i];
                float f = m.HoldFraction(i);
                p.BarFill.fillAmount = f;
                bool carrying = m.Food.Carrier == i;
                p.Seconds.text = carrying || m.Hold[i] > 0f ? $"{m.Hold[i]:0.0} s" : "";
                p.Food.gameObject.SetActive(carrying);
                if (carrying) p.Food.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(time * 8f) * 0.08f);
                float remaining = m.Config.HoldTime - m.Hold[i];
                p.BarFill.color = carrying && remaining < 3f && Mathf.Sin(time * 18f) > 0 ? Color.white : Palette.Player(i);
                for (int s = 0; s < p.Stars.Length; s++)
                    p.Stars[s].color = s < m.Wins[i] ? Palette.Cheese : new Color(1f, 1f, 1f, 0.18f);
            }
            _round.text = "Ronda " + m.Round;

            // Cuenta regresiva.
            string count = "";
            if (m.State == MatchState.Countdown) count = Mathf.CeilToInt(m.StateTimer).ToString();
            else if (m.State == MatchState.Playing && m.Time < 0.1f) count = "";
            if (count != _lastCount)
            {
                _lastCount = count;
                _countPulse = 1f;
            }
            _countPulse = Mathf.MoveTowards(_countPulse, 0f, dt * 2.5f);
            if (m.State == MatchState.Countdown)
            {
                _countdown.text = count;
                _countdown.rectTransform.localScale = Vector3.one * (1f + _countPulse * 0.5f);
                _countdown.color = Palette.WithAlpha(Palette.Cheese, Mathf.Clamp01(0.4f + _countPulse));
            }
            else if (_countdown.text != "" && _countdown.text != "¡YA!")
            {
                _countdown.text = "";
            }

            if (_bannerTime > 0f)
            {
                _bannerTime -= dt;
                _banner.localScale = Vector3.Lerp(_banner.localScale, Vector3.one, dt * 12f);
                if (_bannerTime <= 0f) _banner.gameObject.SetActive(false);
            }

            foreach (var pop in _popups)
            {
                if (pop.Life <= 0f) continue;
                pop.Life -= dt;
                if (pop.Life <= 0f) { pop.Text.gameObject.SetActive(false); continue; }
                float age = 1.1f - pop.Life;
                pop.Text.rectTransform.position = cam.WorldToScreenPoint(pop.World + Vector3.up * (0.4f + age * 0.8f));
                pop.Text.rectTransform.localScale = Vector3.one * Mathf.Min(1f, 0.5f + age * 4f);
                var c = pop.Text.color;
                c.a = Mathf.Clamp01(pop.Life * 2.5f);
                pop.Text.color = c;
            }

            for (int i = 0; i < 2; i++)
            {
                var k = m.Karts[i];
                var up = -Coords.WorldDir(k.Gravity);
                float lift = m.Food.Carrier == i ? 0.95f : 0.68f;
                var world = Coords.ToWorld(k.X, k.Y) + (Vector3)(up * lift);
                _markers[i].position = cam.WorldToScreenPoint(world);
                _markers[i].gameObject.SetActive(!k.InPipe || Mathf.Sin(time * 20f) > 0f);
            }
        }

        /// <summary>Muestra "¡YA!" un momento cuando arranca la ronda.</summary>
        public void Go()
        {
            _countdown.text = "¡YA!";
            _countdown.color = Palette.Alien;
            _countPulse = 1f;
            _countdown.rectTransform.localScale = Vector3.one * 1.4f;
        }

        public void TickGo(float dt)
        {
            if (_countdown.text != "¡YA!") return;
            var c = _countdown.color;
            c.a -= dt * 1.6f;
            _countdown.color = c;
            if (c.a <= 0f) _countdown.text = "";
        }
    }
}
