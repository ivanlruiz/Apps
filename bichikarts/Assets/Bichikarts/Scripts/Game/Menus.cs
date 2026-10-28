using System.Collections.Generic;
using Bichikarts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bichikarts
{
    public enum Page
    {
        Title,
        Garage,
        Levels,
        HowTo,
        Playing,
        Paused,
        Results,
    }

    /// <summary>Resultado de la última partida, para la pantalla final.</summary>
    public sealed class MatchSummary
    {
        public int Winner;
        public string[] Names = new string[2];
        public Look[] Looks = new Look[2];
        public int[] Wins = new int[2];
        public PlayerStats[] Stats = new PlayerStats[2];
        public int StarsEarned;
        public int StarsTotal;
        public List<Item> Unlocked = new List<Item>();
    }

    /// <summary>Todas las pantallas de menú, armadas desde código.</summary>
    public sealed class Menus
    {
        private readonly GameRoot _g;
        private readonly RectTransform _canvas;
        private readonly Image _dim;
        private readonly Dictionary<Page, RectTransform> _pages = new Dictionary<Page, RectTransform>();

        // Inicio
        private Text _starsText;
        private Button _musicButton, _soundButton;

        // Garaje
        private readonly GarageCard[] _cards = new GarageCard[2];

        // Salas
        private readonly List<(Ui.Chip chip, string id)> _levelChips = new List<(Ui.Chip, string)>();
        private readonly List<(Ui.Chip chip, float value)> _holdChips = new List<(Ui.Chip, float)>();
        private readonly List<(Ui.Chip chip, int value)> _roundChips = new List<(Ui.Chip, int)>();
        private readonly List<(Ui.Chip chip, string value)> _foodChips = new List<(Ui.Chip, string)>();

        // Resultados
        private Text _resultTitle, _resultSub, _resultStars, _resultUnlock;
        private Image _resultPortrait;
        private readonly Text[,] _statCells = new Text[6, 2];

        public Menus(GameRoot game, RectTransform canvas)
        {
            _g = game;
            _canvas = canvas;
            _dim = Ui.Image(canvas, Art.UiWhite(), new Color(0.086f, 0.055f, 0.17f, 0.55f), "Oscurecer");
            Ui.Fill(_dim.rectTransform);
            _dim.raycastTarget = true;

            BuildTitle();
            BuildGarage();
            BuildLevels();
            BuildHowTo();
            BuildPause();
            BuildResults();
        }

        private RectTransform NewPage(Page p)
        {
            var rt = Ui.Fill(Ui.Node(_canvas, "Pantalla " + p));
            rt.gameObject.SetActive(false);
            _pages[p] = rt;
            return rt;
        }

        public void Show(Page page)
        {
            foreach (var kv in _pages) kv.Value.gameObject.SetActive(kv.Key == page);
            _dim.gameObject.SetActive(page != Page.Playing);
            _dim.color = page == Page.Title ? new Color(0.086f, 0.055f, 0.17f, 0.35f) : new Color(0.086f, 0.055f, 0.17f, 0.72f);
            switch (page)
            {
                case Page.Title: RefreshTitle(); break;
                case Page.Garage: foreach (var c in _cards) c.Refresh(); break;
                case Page.Levels: RefreshLevels(); break;
                case Page.Results: RefreshResults(); break;
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private static Text Heading(Transform parent, string text, float y, int size = 64)
        {
            var t = Ui.Inked(Ui.Text(parent, text, size, Palette.Cheese, true), 4);
            Ui.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(1400, 90));
            return t;
        }

        private static Button BottomButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action, bool right, bool dots = false)
        {
            var b = Ui.Button(parent, label, color, action, 38, dots);
            Ui.Place(Ui.Rt(b), new Vector2(right ? 1 : 0, 0), new Vector2(right ? 1 : 0, 0), new Vector2(right ? -40 : 40, 26), new Vector2(right ? 380 : 260, 92));
            return b;
        }

        // ------------------------------------------------------------------ inicio

        private void BuildTitle()
        {
            var page = NewPage(Page.Title);

            var logo = Ui.Inked(Ui.Text(page, "Bichikarts", 170, Palette.Cheese, true), 7);
            Ui.Place(logo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1400, 200));
            var tag = Ui.Inked(Ui.Text(page, "Agarrá la comida, aguantala arriba del karting y que no te la roben.", 34, Palette.Crumb), 2);
            Ui.Place(tag.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(1300, 50));

            var column = Ui.Node(page, "Opciones");
            Ui.Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(560, 360));
            Ui.Group<VerticalLayoutGroup>(column, 22, 0, TextAnchor.MiddleCenter, true, false);
            Ui.Size(Ui.Button(column, "Contra la compu", Palette.Crust, () => { _g.VsCpu = true; _g.Open(Page.Garage); }, 44, true), -1, 100);
            Ui.Size(Ui.Button(column, "2 jugadores", Palette.Alien, () => { _g.VsCpu = false; _g.Open(Page.Garage); }, 44), -1, 100);
            Ui.Size(Ui.Button(column, "Cómo se juega", Palette.PanelLight, () => _g.Open(Page.HowTo), 36, false, Palette.Crumb), -1, 84);

            var starChip = Ui.Panel(page, new Color(0.08f, 0.04f, 0.16f, 0.85f), "Estrellas", 24);
            Ui.Place(starChip.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 30), new Vector2(330, 80));
            var star = Ui.Image(starChip.transform, Art.Star(), Palette.Cheese, "Estrella");
            Ui.Place(star.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(54, 54));
            _starsText = Ui.Text(starChip.transform, "", 30, Palette.Crumb, true, TextAnchor.MiddleLeft);
            Ui.Fill(_starsText.rectTransform, 84, 0, 10, 0);

            var toggles = Ui.Node(page, "Sonido");
            Ui.Place(toggles, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(520, 80));
            Ui.Group<HorizontalLayoutGroup>(toggles, 16, 0, TextAnchor.MiddleRight, true, true);
            _musicButton = Ui.Button(toggles, "", Palette.PanelLight, () => { _g.MusicOn = !_g.MusicOn; _g.SaveSettings(); RefreshTitle(); }, 28, false, Palette.Crumb);
            _soundButton = Ui.Button(toggles, "", Palette.PanelLight, () => { _g.SoundOn = !_g.SoundOn; _g.SaveSettings(); RefreshTitle(); }, 28, false, Palette.Crumb);

            var hint = Ui.Inked(Ui.Text(page, TouchControls.DeviceHasTouch()
                ? "En el celu: cruz para moverte, botón para cambiar la gravedad."
                : "J1: WASD + E   ·   J2: flechas + Enter   ·   Joystick: palanca + A", 24, Palette.Muted), 2);
            Ui.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 125), new Vector2(1200, 40));
        }

        private void RefreshTitle()
        {
            int stars = _g.Stars;
            _starsText.text = stars == 1 ? "1 estrella" : stars + " estrellas";
            Ui.Label(_musicButton).text = _g.MusicOn ? "Música: sí" : "Música: no";
            Ui.Label(_soundButton).text = _g.SoundOn ? "Sonido: sí" : "Sonido: no";
        }

        // ------------------------------------------------------------------ garaje

        private void BuildGarage()
        {
            var page = NewPage(Page.Garage);
            Heading(page, "Elegí tu bichito", -14);
            for (int i = 0; i < 2; i++)
            {
                _cards[i] = new GarageCard(_g, page, i);
                Ui.Place(_cards[i].Root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(i == 0 ? -392 : 392, -104), new Vector2(764, 760));
            }
            BottomButton(page, "Volver", Palette.PanelLight, () => _g.Open(Page.Title), false);
            BottomButton(page, "Elegir sala", Palette.Crust, () => _g.Open(Page.Levels), true, true);
        }

        /// <summary>La tarjeta del garaje de un jugador: vista previa, pestañas y opciones.</summary>
        private sealed class GarageCard
        {
            public readonly RectTransform Root;
            private readonly GameRoot _g;
            private readonly int _player;
            private readonly Image _preview;
            private readonly Text _title, _name, _info;
            private readonly RectTransform _difficultyRow;
            private readonly List<(Ui.Chip chip, CpuDifficulty d)> _difficulty = new List<(Ui.Chip, CpuDifficulty)>();
            private readonly RectTransform[] _tabs = new RectTransform[4];
            private readonly Ui.Chip[] _tabButtons = new Ui.Chip[4];
            private readonly List<(Ui.Chip chip, Item item)> _species = new List<(Ui.Chip, Item)>();
            private readonly List<(Ui.Chip chip, string color)> _bodies = new List<(Ui.Chip, string)>();
            private readonly List<(Ui.Chip chip, string color)> _karts = new List<(Ui.Chip, string)>();
            private readonly List<(Ui.Chip chip, Item item)> _styles = new List<(Ui.Chip, Item)>();
            private readonly List<(Ui.Chip chip, Item item)> _hats = new List<(Ui.Chip, Item)>();
            private int _tab;

            private Look Look => _g.Looks[_player];

            public GarageCard(GameRoot g, Transform parent, int player)
            {
                _g = g;
                _player = player;
                Root = Ui.Panel(parent, Palette.Panel, "Jugador " + (player + 1), 26).rectTransform;
                var border = Ui.Image(Root, Art.UiFrame(), Palette.WithAlpha(Palette.Player(player), 0.9f), "Borde");
                Ui.Fill(border.rectTransform, -3, -3, -3, -3);

                var tag = Ui.Panel(Root, Palette.Player(player), "Etiqueta", 16);
                Ui.Place(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -18), new Vector2(240, 54));
                _title = Ui.Text(tag.transform, "", 30, Palette.Ink, true);
                Ui.Fill(_title.rectTransform);

                var random = Ui.Button(Root, "Sorprendeme", Palette.Alien, Randomize, 28);
                Ui.Place(Ui.Rt(random), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -14), new Vector2(240, 64));

                var previewBg = Ui.Panel(Root, Palette.PanelLight, "Vista", 22);
                Ui.Place(previewBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -86), new Vector2(250, 230));
                _preview = Ui.Image(previewBg.transform, null, Color.white, "Karting");
                _preview.preserveAspect = true;
                Ui.Fill(_preview.rectTransform, 6, 6, 6, 6);

                _name = Ui.Inked(Ui.Text(Root, "", 52, Palette.Crumb, true, TextAnchor.MiddleLeft), 3);
                Ui.Place(_name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, -92), new Vector2(440, 64));
                _info = Ui.Text(Root, "", 24, Palette.Muted, false, TextAnchor.UpperLeft);
                Ui.Place(_info.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, -160), new Vector2(440, 70));

                _difficultyRow = Ui.Node(Root, "Dificultad");
                Ui.Place(_difficultyRow, new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, -236), new Vector2(440, 70));
                Ui.Group<HorizontalLayoutGroup>(_difficultyRow, 10, 0, TextAnchor.MiddleLeft, true, true);
                foreach (var (d, label) in new[] { (CpuDifficulty.Easy, "Fácil"), (CpuDifficulty.Normal, "Normal"), (CpuDifficulty.Hard, "Difícil") })
                {
                    var diff = d;
                    var chip = new Ui.Chip(_difficultyRow, label, null, Palette.Crumb, () => { _g.Difficulty = diff; _g.SaveSettings(); Refresh(); }, 26);
                    _difficulty.Add((chip, d));
                }

                var tabs = Ui.Node(Root, "Pestañas");
                Ui.Place(tabs, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(720, 62));
                Ui.Group<HorizontalLayoutGroup>(tabs, 10, 0, TextAnchor.MiddleCenter, true, true);
                string[] names = { "Bichito", "Colores", "Karting", "Accesorio" };
                for (int i = 0; i < 4; i++)
                {
                    int tab = i;
                    _tabButtons[i] = new Ui.Chip(tabs, names[i], null, Palette.PanelLight, () => { _tab = tab; Refresh(); }, 26);
                    _tabButtons[i].Caption.color = Palette.Crumb;
                    _tabs[i] = Ui.Node(Root, "Contenido " + names[i]);
                    Ui.Place(_tabs[i], new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -410), new Vector2(720, 330));
                }

                // Bichitos
                Ui.Grid(_tabs[0], new Vector2(166, 150), new Vector2(12, 14), 4);
                foreach (var sp in Catalog.Species)
                {
                    var item = sp;
                    var chip = new Ui.Chip(_tabs[0], sp.Name, Art.Head(new Look { species = sp.Id, body = sp.Color, hat = "nada" }), Palette.Crumb, () =>
                    {
                        Look.species = item.Id;
                        Look.body = item.Color;
                        Changed();
                    });
                    _species.Add((chip, sp));
                }

                // Colores
                Ui.Group<VerticalLayoutGroup>(_tabs[1], 8, 0, TextAnchor.UpperCenter, true, false);
                Swatches(_tabs[1], "Color del bichito", Catalog.BodyColors, _bodies, c => { Look.body = c; Changed(); });
                Swatches(_tabs[1], "Color del karting", Catalog.KartColors, _karts, c => { Look.kart = c; Changed(); });

                // Karting
                Ui.Grid(_tabs[2], new Vector2(226, 220), new Vector2(14, 14), 3);
                foreach (var st in Catalog.KartStyles)
                {
                    var item = st;
                    var preview = Look.Clone();
                    preview.style = st.Id;
                    var chip = new Ui.Chip(_tabs[2], st.Name, Art.Portrait(preview, player + 1), Palette.Crumb, () => { Look.style = item.Id; Changed(); }, 26);
                    _styles.Add((chip, st));
                }

                // Accesorios
                Ui.Grid(_tabs[3], new Vector2(166, 150), new Vector2(12, 14), 4);
                foreach (var h in Catalog.Hats)
                {
                    var item = h;
                    var chip = new Ui.Chip(_tabs[3], h.Name, h.Id == "nada" ? null : Art.Hat(h.Id), Palette.Crumb, () => { Look.hat = item.Id; Changed(); }, 20);
                    _hats.Add((chip, h));
                }
            }

            private static void Swatches(Transform parent, string label, string[] colors, List<(Ui.Chip, string)> list, System.Action<string> pick)
            {
                var t = Ui.Text(parent, label, 26, Palette.Crumb, true, TextAnchor.MiddleLeft);
                Ui.Size(t, -1, 40);
                var row = Ui.Node(parent, label);
                Ui.Size(row, -1, 120);
                Ui.Grid(row, new Vector2(62, 52), new Vector2(10, 12), 10);
                foreach (var c in colors)
                {
                    var color = c;
                    var chip = new Ui.Chip(row, "", null, Palette.Hex(c), () => pick(color));
                    list.Add((chip, c));
                }
            }

            private void Randomize()
            {
                var rng = new System.Random();
                _g.Looks[_player] = Catalog.RandomLook(rng, _g.Stars);
                Changed();
            }

            private void Changed()
            {
                _g.SaveLook(_player);
                Refresh();
            }

            public void Refresh()
            {
                int stars = _g.Stars;
                bool cpu = _g.VsCpu && _player == 1;
                _title.text = cpu ? "La compu" : "Jugador " + (_player + 1);
                _preview.sprite = Art.Portrait(Look, _player + 1);
                _name.text = Catalog.Find(Catalog.Species, Look.species).Name;
                _info.text = $"{Catalog.Find(Catalog.KartStyles, Look.style).Name} · {Catalog.Find(Catalog.Hats, Look.hat).Name}";
                _difficultyRow.gameObject.SetActive(cpu);
                foreach (var (chip, d) in _difficulty) chip.SetSelected(d == _g.Difficulty);

                for (int i = 0; i < 4; i++)
                {
                    _tabs[i].gameObject.SetActive(i == _tab);
                    _tabButtons[i].SetSelected(i == _tab);
                }
                foreach (var (chip, item) in _species)
                {
                    chip.SetSelected(item.Id == Look.species);
                    chip.SetLocked(!Catalog.IsUnlocked(item, stars), item.Cost);
                }
                foreach (var (chip, c) in _bodies) chip.SetSelected(string.Equals(c, Look.body, System.StringComparison.OrdinalIgnoreCase));
                foreach (var (chip, c) in _karts) chip.SetSelected(string.Equals(c, Look.kart, System.StringComparison.OrdinalIgnoreCase));
                foreach (var (chip, item) in _styles)
                {
                    chip.SetSelected(item.Id == Look.style);
                    chip.SetLocked(!Catalog.IsUnlocked(item, stars), item.Cost);
                    var preview = Look.Clone();
                    preview.style = item.Id;
                    chip.Icon.sprite = Art.Portrait(preview, _player + 1);
                }
                foreach (var (chip, item) in _hats)
                {
                    chip.SetSelected(item.Id == Look.hat);
                    chip.SetLocked(!Catalog.IsUnlocked(item, stars), item.Cost);
                }
            }

        }

        // ------------------------------------------------------------------ salas

        private void BuildLevels()
        {
            var page = NewPage(Page.Levels);
            Heading(page, "Elegí la sala", -14);

            var row = Ui.Node(page, "Salas");
            Ui.Place(row, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1540, 330));
            Ui.Group<HorizontalLayoutGroup>(row, 20, 0, TextAnchor.MiddleCenter, false, true);
            foreach (var level in Levels.All)
            {
                var l = level;
                var chip = new Ui.Chip(row, "", null, Palette.Crumb, () => { _g.LevelId = l.Id; _g.SaveSettings(); RefreshLevels(); });
                Ui.Size(chip.Button, 360, 320);
                var thumb = Ui.Image(chip.Button.transform, LevelArt.Build(l, 0.2f), Color.white, "Miniatura");
                Ui.Place(thumb.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(330, 186));
                thumb.transform.SetSiblingIndex(0);
                var name = Ui.Text(chip.Button.transform, l.Name, 32, Palette.Ink, true);
                Ui.Place(name.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(340, 40));
                var food = Ui.Text(chip.Button.transform, "Comida: " + Catalog.Find(Catalog.Foods, l.DefaultFood).Name, 22, Palette.WithAlpha(Palette.Ink, 0.75f));
                Ui.Place(food.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(340, 34));
                _levelChips.Add((chip, l.Id));
            }

            var options = Ui.Panel(page, Palette.Panel, "Opciones", 26);
            Ui.Place(options.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -470), new Vector2(1500, 360));
            Ui.Group<VerticalLayoutGroup>(options.transform, 14, 24, TextAnchor.UpperLeft, true, false);

            var hold = OptionRow(options.transform, "Tiempo para ganar");
            foreach (float v in new[] { 10f, 15f, 20f, 30f })
            {
                float value = v;
                var chip = new Ui.Chip(hold, value + " segundos", null, Palette.Crumb, () => { _g.HoldTime = value; _g.SaveSettings(); RefreshLevels(); }, 26);
                Ui.Size(chip.Button, 220, 70);
                _holdChips.Add((chip, v));
            }
            var rounds = OptionRow(options.transform, "Rondas para ganar");
            foreach (int v in new[] { 1, 2, 3, 5 })
            {
                int value = v;
                var chip = new Ui.Chip(rounds, value == 1 ? "1 ronda" : value + " rondas", null, Palette.Crumb, () => { _g.Rounds = value; _g.SaveSettings(); RefreshLevels(); }, 26);
                Ui.Size(chip.Button, 220, 70);
                _roundChips.Add((chip, v));
            }
            var foods = OptionRow(options.transform, "Comida");
            var foodOptions = new List<(string id, string name)> { ("sala", "La de la sala") };
            foreach (var f in Catalog.Foods) foodOptions.Add((f.Id, f.Name));
            foodOptions.Add(("sorpresa", "Sorpresa"));
            foreach (var (id, label) in foodOptions)
            {
                string value = id;
                var icon = id == "sala" || id == "sorpresa" ? null : Art.Food(id);
                var chip = new Ui.Chip(foods, icon == null ? label : "", icon, Palette.Crumb, () => { _g.FoodChoice = value; _g.SaveSettings(); RefreshLevels(); }, 22);
                Ui.Size(chip.Button, icon == null ? 200 : 96, 70);
                _foodChips.Add((chip, value));
            }

            BottomButton(page, "Volver", Palette.PanelLight, () => _g.Open(Page.Garage), false);
            BottomButton(page, "¡A jugar!", Palette.Crust, _g.StartMatch, true, true);
        }

        private static Transform OptionRow(Transform parent, string label)
        {
            var row = Ui.Node(parent, label);
            Ui.Size(row, -1, 80);
            Ui.Group<HorizontalLayoutGroup>(row, 14, 0, TextAnchor.MiddleLeft, false, false);
            var t = Ui.Text(row, label, 30, Palette.Crumb, true, TextAnchor.MiddleLeft);
            Ui.Size(t, 330, 70);
            return row;
        }

        private void RefreshLevels()
        {
            foreach (var (chip, id) in _levelChips) chip.SetSelected(id == _g.LevelId);
            foreach (var (chip, v) in _holdChips) chip.SetSelected(Mathf.Approximately(v, _g.HoldTime));
            foreach (var (chip, v) in _roundChips) chip.SetSelected(v == _g.Rounds);
            foreach (var (chip, v) in _foodChips) chip.SetSelected(v == _g.FoodChoice);
        }

        // ------------------------------------------------------------------ cómo se juega

        private void BuildHowTo()
        {
            var page = NewPage(Page.HowTo);
            Heading(page, "Cómo se juega", -14);

            string[] left =
            {
                "<b>La comida</b>\nArrancan en las puntas de la sala y en el medio hay comida. El primero que la toca la lleva arriba del karting.",
                "<b>Aguantar</b>\nSi la aguantás el tiempo elegido (15 segundos, por ejemplo) ganás la ronda. Con la comida vas más lento.",
                "<b>Robar</b>\nEl otro va más rápido. Si te choca, te roba la comida y su tiempo arranca de cero. Después de robar quedás protegido un ratito.",
            };
            string[] right =
            {
                "<b>Gravedad</b>\nEl botón de gravedad te da vuelta: del piso al techo y al revés. Si lo apretás empujando contra una pared, te pegás a la pared. En el aire se puede usar una vez.",
                "<b>Saltar</b>\nEmpujá hacia afuera de donde estás pegado: arriba en el piso, abajo en el techo.",
                "<b>Tuberías</b>\nParate sobre una boca y empujá hacia adentro. Salís por la otra boca del mismo color, pegado a esa superficie.",
            };
            Column(page, left, -400);
            Column(page, right, 400);

            string controls = TouchControls.DeviceHasTouch()
                ? "Celular: cruz para moverte y saltar, botón redondo para la gravedad."
                : "<b>J1</b>: WASD + E (o Q, F, Espacio)    <b>J2</b>: flechas + Enter (o Shift derecho, 0)    <b>Joystick</b>: palanca + A, B salta";
            var c = Ui.Inked(Ui.Text(page, controls, 26, Palette.Alien), 2);
            Ui.Place(c.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1500, 50));
            BottomButton(page, "Volver", Palette.PanelLight, () => _g.Open(Page.Title), false);
        }

        private static void Column(Transform page, string[] blocks, float x)
        {
            var panel = Ui.Panel(page, Palette.Panel, "Columna", 26);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, -110), new Vector2(760, 620));
            Ui.Group<VerticalLayoutGroup>(panel.transform, 18, 30, TextAnchor.UpperLeft, true, false);
            foreach (var b in blocks)
            {
                var t = Ui.Text(panel.transform, b, 28, Palette.Crumb, false, TextAnchor.UpperLeft);
                Ui.Size(t, -1, 170);
            }
        }

        // ------------------------------------------------------------------ pausa

        private void BuildPause()
        {
            var page = NewPage(Page.Paused);
            var panel = Ui.Panel(page, Palette.Panel, "Pausa", 30);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 560));
            Ui.Group<VerticalLayoutGroup>(panel.transform, 20, 40, TextAnchor.UpperCenter, true, false);
            var t = Ui.Inked(Ui.Text(panel.transform, "Pausa", 80, Palette.Cheese, true), 4);
            Ui.Size(t, -1, 110);
            Ui.Size(Ui.Button(panel.transform, "Seguir", Palette.Crust, _g.Resume, 40, true), -1, 96);
            Ui.Size(Ui.Button(panel.transform, "Empezar de nuevo", Palette.Alien, _g.StartMatch, 36), -1, 90);
            Ui.Size(Ui.Button(panel.transform, "Salir al menú", Palette.PanelLight, _g.QuitToMenu, 36, false, Palette.Crumb), -1, 90);
        }

        // ------------------------------------------------------------------ resultados

        private void BuildResults()
        {
            var page = NewPage(Page.Results);
            var panel = Ui.Panel(page, Palette.Panel, "Resultado", 30);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1240, 800));

            _resultTitle = Ui.Inked(Ui.Text(panel.transform, "", 84, Palette.Cheese, true), 5);
            Ui.Place(_resultTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1180, 110));
            _resultSub = Ui.Text(panel.transform, "", 30, Palette.Muted, true);
            Ui.Place(_resultSub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1180, 44));

            var portraitBg = Ui.Panel(panel.transform, Palette.PanelLight, "Ganador", 26);
            Ui.Place(portraitBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -200), new Vector2(360, 360));
            _resultPortrait = Ui.Image(portraitBg.transform, null, Color.white, "Retrato");
            _resultPortrait.preserveAspect = true;
            Ui.Fill(_resultPortrait.rectTransform, 10, 10, 10, 10);

            var table = Ui.Node(panel.transform, "Tabla");
            Ui.Place(table, new Vector2(0, 1), new Vector2(0, 1), new Vector2(450, -200), new Vector2(740, 360));
            Ui.Grid(table, new Vector2(240, 52), new Vector2(10, 4), 3);
            string[] rows = { "", "Rondas ganadas", "Robos", "Mejor aguante", "Cambios de gravedad", "Viajes por tubería" };
            for (int r = 0; r < rows.Length; r++)
            {
                var label = Ui.Text(table, rows[r], 26, Palette.Crumb, r == 0, TextAnchor.MiddleLeft);
                for (int c = 0; c < 2; c++)
                {
                    var cell = Ui.Text(table, "", 28, r == 0 ? Palette.Player(c) : Color.white, true);
                    _statCells[r, c] = cell;
                }
                _ = label;
            }

            _resultStars = Ui.Inked(Ui.Text(panel.transform, "", 34, Palette.Cheese, true), 2);
            Ui.Place(_resultStars.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 190), new Vector2(1180, 50));
            _resultUnlock = Ui.Text(panel.transform, "", 28, Palette.Alien, true);
            Ui.Place(_resultUnlock.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1180, 44));

            var buttons = Ui.Node(panel.transform, "Botones");
            Ui.Place(buttons, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1160, 96));
            Ui.Group<HorizontalLayoutGroup>(buttons, 20, 0, TextAnchor.MiddleCenter, true, true);
            Ui.Button(buttons, "Revancha", Palette.Crust, _g.StartMatch, 38, true);
            Ui.Button(buttons, "Cambiar bichitos", Palette.Alien, () => _g.Open(Page.Garage), 34);
            Ui.Button(buttons, "Menú", Palette.PanelLight, _g.QuitToMenu, 36, false, Palette.Crumb);
        }

        private void RefreshResults()
        {
            var s = _g.LastResult;
            if (s == null) return;
            int w = s.Winner;
            _resultTitle.text = "¡Ganó " + Catalog.Find(Catalog.Species, s.Looks[w].species).Name + "!";
            _resultTitle.color = Palette.Player(w);
            _resultSub.text = s.Names[w];
            _resultPortrait.sprite = Art.Portrait(s.Looks[w], w + 1);
            for (int c = 0; c < 2; c++)
            {
                _statCells[0, c].text = s.Names[c];
                _statCells[1, c].text = s.Wins[c].ToString();
                _statCells[2, c].text = s.Stats[c].Steals.ToString();
                _statCells[3, c].text = $"{s.Stats[c].BestHold:0.0} s";
                _statCells[4, c].text = s.Stats[c].Flips.ToString();
                _statCells[5, c].text = s.Stats[c].Pipes.ToString();
            }
            _resultStars.text = $"+{s.StarsEarned} {(s.StarsEarned == 1 ? "estrella" : "estrellas")}  ·  total {s.StarsTotal}";
            if (s.Unlocked.Count > 0)
            {
                var names = new List<string>();
                foreach (var it in s.Unlocked) names.Add(it.Name);
                _resultUnlock.text = "¡Desbloqueaste: " + string.Join(", ", names) + "!";
            }
            else
            {
                _resultUnlock.text = NextUnlockText(s.StarsTotal);
            }
        }

        private static string NextUnlockText(int stars)
        {
            Item next = null;
            foreach (var list in new[] { Catalog.Species, Catalog.KartStyles, Catalog.Hats })
                foreach (var it in list)
                    if (it.Cost > stars && (next == null || it.Cost < next.Cost)) next = it;
            if (next == null) return "Ya desbloqueaste todo.";
            int missing = next.Cost - stars;
            return $"Te {(missing == 1 ? "falta 1 estrella" : $"faltan {missing} estrellas")} para {next.Name}.";
        }
    }
}
