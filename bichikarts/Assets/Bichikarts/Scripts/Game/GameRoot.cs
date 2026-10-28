using Bichikarts.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bichikarts
{
    /// <summary>
    /// Arma el juego entero al arrancar (cámara, interfaz, sonido) y maneja las pantallas y la partida.
    /// La simulación corre a paso fijo (120 por segundo) con el núcleo de Bichikarts.Core.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        // Opciones que se eligen en los menús.
        public bool VsCpu = true;
        public CpuDifficulty Difficulty = CpuDifficulty.Normal;
        public readonly Look[] Looks = new Look[2];
        public string LevelId = "cocina";
        public float HoldTime = 15f;
        public int Rounds = 2;
        public string FoodChoice = "sala";

        public int Stars => Save.Stars;
        public MatchSummary LastResult { get; private set; }

        public bool MusicOn
        {
            get => _musicOn;
            set { _musicOn = value; _sfx?.SetMusic(value); }
        }

        public bool SoundOn
        {
            get => _sfx == null || _sfx.SoundOn;
            set { if (_sfx != null) _sfx.SoundOn = value; }
        }

        private bool _musicOn = true;
        private Camera _cam;
        private Sfx _sfx;
        private InputHub _input;
        private Menus _menus;
        private Hud _hud;
        private TouchControls _touch;
        private CanvasScaler _scaler;
        private Transform _world;

        private Match _match;
        private MatchView _view;
        private readonly CpuBrain[] _cpus = new CpuBrain[2];
        private readonly Look[] _matchLooks = new Look[2];
        private readonly string[] _names = new string[2];
        private readonly string[] _shortNames = new string[2];
        private bool _demo;
        private Page _page = Page.Title;
        private float _accumulator;
        private float _time;
        private float _shake;
        private readonly System.Random _rng = new System.Random();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.AutoRotation;

            _world = new GameObject("Mundo").transform;
            _world.SetParent(transform, false);
            SetupCamera();
            SetupEventSystem();

            _sfx = new Sfx(gameObject);
            Ui.Init();
            Ui.Click = () => _sfx.Play("ui", 0.6f);

            LoadSettings();

            var canvasGo = new GameObject("Interfaz", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1600, 990);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();
            UpdateScaler();

            _hud = new Hud(canvasGo.transform, Pause);
            _touch = new TouchControls(canvasGo.transform);
            _input = new InputHub { Touch = _touch };
            _menus = new Menus(this, (RectTransform)canvasGo.transform);
        }

        private void Start()
        {
            _sfx.SetMusic(_musicOn);
            StartDemo();
            Open(Page.Title);
        }

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Cámara");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Space;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;
            if (Object.FindAnyObjectByType<AudioListener>() == null) _cam.gameObject.AddComponent<AudioListener>();
        }

        private static void SetupEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private void UpdateScaler()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _scaler.matchWidthOrHeight = aspect >= 1600f / 990f ? 1f : 0f;
        }

        // ------------------------------------------------------------------ opciones guardadas

        private void LoadSettings()
        {
            int stars = Save.Stars;
            for (int i = 0; i < 2; i++) Looks[i] = Save.Sanitize(Save.LoadLook(i), i, stars);
            LevelId = Levels.Get(Save.GetString("sala", "cocina")).Id;
            HoldTime = Mathf.Clamp(Save.GetInt("aguante", 15), 5, 60);
            Rounds = Mathf.Clamp(Save.GetInt("rondas", 2), 1, 9);
            FoodChoice = Save.GetString("comida", "sala");
            Difficulty = (CpuDifficulty)Mathf.Clamp(Save.GetInt("dificultad", (int)CpuDifficulty.Normal), 0, 2);
            _musicOn = Save.GetInt("musica", 1) == 1;
            _sfx.SoundOn = Save.GetInt("sonido", 1) == 1;
        }

        public void SaveSettings()
        {
            Save.SetString("sala", LevelId);
            Save.SetInt("aguante", Mathf.RoundToInt(HoldTime));
            Save.SetInt("rondas", Rounds);
            Save.SetString("comida", FoodChoice);
            Save.SetInt("dificultad", (int)Difficulty);
            Save.SetInt("musica", _musicOn ? 1 : 0);
            Save.SetInt("sonido", _sfx.SoundOn ? 1 : 0);
        }

        public void SaveLook(int player) => Save.StoreLook(player, Looks[player]);

        // ------------------------------------------------------------------ pantallas

        public void Open(Page page)
        {
            bool menu = page == Page.Title || page == Page.Garage || page == Page.Levels || page == Page.HowTo;
            if (menu && !_demo) StartDemo();
            _page = page;
            _menus.Show(page);
            _hud.Show(!_demo && (page == Page.Playing || page == Page.Paused));
            _touch.Show(page == Page.Playing && !_demo && TouchControls.DeviceHasTouch(), !VsCpu);
        }

        public void Pause()
        {
            if (_page == Page.Playing) Open(Page.Paused);
        }

        public void Resume()
        {
            if (_page == Page.Paused) Open(Page.Playing);
        }

        public void QuitToMenu()
        {
            StartDemo();
            Open(Page.Title);
        }

        // ------------------------------------------------------------------ partidas

        public void StartMatch()
        {
            string food = FoodChoice;
            var level = Levels.Get(LevelId);
            if (food == "sala") food = level.DefaultFood;
            else if (food == "sorpresa") food = Catalog.Foods[_rng.Next(Catalog.Foods.Length)].Id;

            var config = new MatchConfig
            {
                LevelId = level.Id,
                HoldTime = HoldTime,
                RoundsToWin = Rounds,
                Handicap = new[] { 1f, VsCpu ? CpuBrain.HandicapFor(Difficulty) : 1f },
            };
            for (int i = 0; i < 2; i++) _matchLooks[i] = Looks[i].Clone();
            Begin(config, food, false);
            _cpus[0] = null;
            _cpus[1] = VsCpu ? new CpuBrain(_match, 1, Difficulty) : null;

            for (int i = 0; i < 2; i++)
            {
                var species = Catalog.Find(Catalog.Species, _matchLooks[i].species).Name;
                _names[i] = VsCpu && i == 1 ? "Compu · " + species : $"J{i + 1} · {species}";
            }
            bool same = _matchLooks[0].species == _matchLooks[1].species;
            for (int i = 0; i < 2; i++)
                _shortNames[i] = same ? (VsCpu && i == 1 ? "la compu" : "J" + (i + 1)) : Catalog.Find(Catalog.Species, _matchLooks[i].species).Name;

            _hud.Bind(_match, _matchLooks, _names, food);
            Open(Page.Playing);
        }

        private void StartDemo()
        {
            int stars = Save.Stars;
            var level = Levels.All[_rng.Next(Levels.All.Count)];
            for (int i = 0; i < 2; i++) _matchLooks[i] = Catalog.RandomLook(_rng, Mathf.Max(stars, 6));
            Begin(new MatchConfig { LevelId = level.Id, HoldTime = 8f, RoundsToWin = 1 }, level.DefaultFood, true);
            _cpus[0] = new CpuBrain(_match, 0, CpuDifficulty.Normal);
            _cpus[1] = new CpuBrain(_match, 1, CpuDifficulty.Normal);
            for (int i = 0; i < 2; i++) _names[i] = _shortNames[i] = Catalog.Find(Catalog.Species, _matchLooks[i].species).Name;
        }

        private void Begin(MatchConfig config, string food, bool demo)
        {
            _view?.Destroy();
            _match = new Match(config);
            _view = new MatchView(_world, _match, _matchLooks, food);
            _demo = demo;
            _sfx.Muted = demo;
            _accumulator = 0f;
            _input.ConsumeEdges();
        }

        private void FinishMatch()
        {
            int before = Save.Stars;
            int earned = VsCpu ? Catalog.StarsFor(_match.Winner == 0, true, Difficulty) : Catalog.StarsFor(true, false, Difficulty);
            Save.Stars = before + earned;
            LastResult = new MatchSummary
            {
                Winner = _match.Winner,
                Names = (string[])_names.Clone(),
                Looks = new[] { _matchLooks[0].Clone(), _matchLooks[1].Clone() },
                Wins = (int[])_match.Wins.Clone(),
                Stats = new[] { _match.Stats[0], _match.Stats[1] },
                StarsEarned = earned,
                StarsTotal = before + earned,
                Unlocked = Catalog.NewlyUnlocked(before, before + earned),
            };
            Open(Page.Results);
        }

        // ------------------------------------------------------------------ cuadro a cuadro

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            _time += dt;
            UpdateScaler();

            _input.TwoPlayers = !VsCpu && !_demo;
            _input.Poll();
            if (_input.PausePressed)
            {
                if (_page == Page.Playing) Pause();
                else if (_page == Page.Paused) Resume();
            }

            bool running = _match != null && !_match.Finished && (_demo ? _page != Page.Results : _page == Page.Playing);
            if (running)
            {
                _accumulator += dt;
                int steps = 0;
                while (_accumulator >= Tuning.Step && steps < 15)
                {
                    var a = _cpus[0] != null ? _cpus[0].Step(Tuning.Step) : _input.Read(0);
                    var b = _cpus[1] != null ? _cpus[1].Step(Tuning.Step) : _input.Read(1);
                    _match.Update(Tuning.Step, a, b);
                    _input.ConsumeEdges();
                    _accumulator -= Tuning.Step;
                    steps++;
                }
                if (steps == 15) _accumulator = 0f;

                foreach (var e in _match.Events)
                    _view.Handle(e, _match, _sfx, _demo ? null : _hud, s => _shake = Mathf.Max(_shake, s), _shortNames);
                _match.Events.Clear();
            }

            _view?.Update(_match, running ? dt : 0f, _time);
            if (!_demo && (_page == Page.Playing || _page == Page.Paused))
            {
                _hud.Update(_match, _cam, running ? dt : 0f, _time);
                _hud.TickGo(dt);
            }

            FitCamera();

            if (_match != null && _match.Finished)
            {
                if (_demo) StartDemo();
                else if (_page == Page.Playing) FinishMatch();
            }
        }

        private void FitCamera()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float roomW = Tuning.RoomWidth / Coords.UnitsPerMeter, roomH = Tuning.RoomHeight / Coords.UnitsPerMeter;
            Vector3 pos;
            if (_demo)
            {
                // En los menús la sala llena la pantalla.
                float size = Mathf.Min(roomH / 2f, roomW / aspect / 2f);
                _cam.orthographicSize = size;
                pos = new Vector3(roomW / 2f, roomH / 2f, -10f);
            }
            else
            {
                // Jugando, entra la sala completa más la franja del HUD arriba.
                float hud = Hud.BarHeight / Coords.UnitsPerMeter;
                float size = Mathf.Max((roomH + hud) / 2f, roomW / aspect / 2f);
                _cam.orthographicSize = size;
                pos = new Vector3(roomW / 2f, roomH + hud - size, -10f);
            }

            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 0.8f);
            if (_shake > 0f) pos += (Vector3)(Random.insideUnitCircle * _shake * 0.4f);
            _cam.transform.position = pos;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Application.isMobilePlatform) Pause();
        }
    }
}
