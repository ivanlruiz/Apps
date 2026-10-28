using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>
    /// El karting con su bichito, armado con sprites separados para animar ojos, antena, ruedas,
    /// caras (feliz, mareado, enojado) y la comida arriba de la cabeza.
    /// </summary>
    public sealed class KartView
    {
        public readonly GameObject Root;
        public Look Look { get; private set; }

        /// <summary>El dibujo es un poco más grande que la caja de choque para que se lea bien en el celular.</summary>
        public const float VisualScale = 1.2f;

        private readonly int _index;
        private readonly int _order;
        private readonly Transform _visual, _bichito, _antenna, _food, _stars;
        private readonly SpriteRenderer _back, _body, _front, _wheelA, _wheelB, _mouth, _hat, _bubble, _flame, _foodSprite, _shield, _glow, _antennaSprite;
        private SpriteRenderer[] _eyes = new SpriteRenderer[0], _pupils = new SpriteRenderer[0], _brows = new SpriteRenderer[0];
        private readonly SpriteRenderer[] _starSprites = new SpriteRenderer[3];
        private SpeciesRig _rig;

        private float _angle;
        private float _facing = 1f;
        private float _squash;
        private float _wheel;
        private float _blinkTimer = 2f;
        private float _blink;
        private bool _wasInPipe;
        private string _expression = "";

        public KartView(Transform parent, int index, Look look, string food)
        {
            _index = index;
            _order = 20 + index * 20;
            Root = new GameObject("Karting " + (index + 1));
            Root.transform.SetParent(parent, false);

            _visual = Child(Root.transform, "Visual").transform;
            _back = MakeSprite(_visual, "Atrás", 0);
            _bichito = Child(_visual, "Bichito").transform;
            _antenna = Child(_bichito, "Antena").transform;
            _antennaSprite = MakeSprite(_antenna, "AntenaSprite", 1);
            _body = MakeSprite(_bichito, "Cuerpo", 2);
            _mouth = MakeSprite(_bichito, "Boca", 5);
            _hat = MakeSprite(_bichito, "Accesorio", 6);
            _bubble = MakeSprite(_bichito, "Burbuja", 7);
            _front = MakeSprite(_visual, "Frente", 8);
            _wheelA = MakeSprite(_visual, "RuedaAtrás", 9);
            _wheelB = MakeSprite(_visual, "RuedaAdelante", 9);
            _flame = MakeSprite(_visual, "Fuego", -1);
            _food = Child(_visual, "Comida").transform;
            _foodSprite = MakeSprite(_food, "ComidaSprite", 10);
            _shield = MakeSprite(Root.transform, "Escudo", 11);
            _glow = MakeSprite(Root.transform, "Brillo", 12);
            _stars = Child(Root.transform, "Estrellitas").transform;
            for (int i = 0; i < 3; i++)
            {
                _starSprites[i] = MakeSprite(_stars, "Estrella", 12);
                _starSprites[i].sprite = Art.Star();
                _starSprites[i].color = Palette.Cheese;
                _starSprites[i].transform.localScale = Vector3.one * 0.6f;
            }

            _wheelA.sprite = _wheelB.sprite = Art.Wheel();
            _wheelA.transform.localPosition = L(-16, 13);
            _wheelB.transform.localPosition = L(16, 13);
            _flame.sprite = Art.Flame();
            _flame.transform.localPosition = L(-36, 5);
            _shield.sprite = Art.Shield();
            _shield.transform.localPosition = L(0, -12);
            _shield.color = Palette.Player(index);
            _glow.sprite = Art.Soft();
            _glow.color = Palette.WithAlpha(Palette.Player(index), 0.9f);
            _glow.transform.localScale = Vector3.one * 1.6f;
            _food.localPosition = L(0, Art.FoodY);
            SetFood(food);
            SetLook(look);
            _facing = index == 0 ? 1f : -1f;
        }

        private static Vector3 L(float x, float y) => new Vector3(x / Coords.UnitsPerMeter, -y / Coords.UnitsPerMeter, 0f);

        private static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private SpriteRenderer MakeSprite(Transform parent, string name, int order)
        {
            var sr = Child(parent, name).AddComponent<SpriteRenderer>();
            sr.sortingOrder = _order + order;
            return sr;
        }

        public void SetFood(string food) => _foodSprite.sprite = Art.Food(food);

        public void SetLook(Look look)
        {
            Look = look.Clone();
            _rig = Art.Rig(look.species);
            var body = Palette.Hex(look.body);
            var kart = Palette.Hex(look.kart);

            _back.sprite = Art.KartBack(look.style, kart);
            _front.sprite = Art.KartFront(look.style, kart, _index + 1);
            _body.sprite = Art.Body(look.species, body);
            _bichito.localPosition = L(Art.SeatX, Art.SeatY(look.style));
            _antenna.gameObject.SetActive(_rig.Antenna);
            _antenna.localPosition = L(0, -14);
            _antennaSprite.sprite = Art.Antenna(body);
            _bubble.sprite = Art.Bubble();
            _bubble.gameObject.SetActive(_rig.Bubble);
            _mouth.transform.localPosition = L(_rig.Mouth.x, _rig.Mouth.y);

            bool hasHat = look.hat != "nada";
            _hat.gameObject.SetActive(hasHat);
            if (hasHat)
            {
                _hat.sprite = Art.Hat(look.hat);
                var a = Art.HatAnchor(look.hat, _rig);
                _hat.transform.localPosition = L(a.x, a.y);
                _hat.sortingOrder = _order + (look.hat == "casco" ? 7 : 6);
            }

            foreach (var e in _eyes) Object.Destroy(e.gameObject);
            foreach (var b in _brows) Object.Destroy(b.gameObject);
            int n = _rig.Eyes.Length;
            _eyes = new SpriteRenderer[n];
            _pupils = new SpriteRenderer[n];
            _brows = new SpriteRenderer[n];
            for (int i = 0; i < n; i++)
            {
                _eyes[i] = MakeSprite(_bichito, "Ojo", 3);
                _eyes[i].transform.localPosition = L(_rig.Eyes[i].x, _rig.Eyes[i].y);
                _pupils[i] = MakeSprite(_eyes[i].transform, "Pupila", 4);
                _brows[i] = MakeSprite(_bichito, "Ceja", 5);
                _brows[i].sprite = Art.Brow();
                _brows[i].transform.localPosition = L(_rig.Eyes[i].x, _rig.Eyes[i].y - _rig.EyeRadius - 3f);
            }
            _flame.gameObject.SetActive(false);
            _expression = "";
        }

        /// <summary>Rebote al aterrizar.</summary>
        public void Squash(float amount) => _squash = Mathf.Max(_squash, amount);

        public void Update(Match m, float dt, float time)
        {
            var k = m.Karts[_index];
            var other = m.Karts[1 - _index];
            Root.transform.position = Coords.ToWorld(k.X, k.Y);

            if (k.InPipe)
            {
                _visual.gameObject.SetActive(false);
                _glow.gameObject.SetActive(true);
                _glow.transform.localScale = Vector3.one * (1.5f + Mathf.Sin(time * 20f) * 0.15f);
                _shield.gameObject.SetActive(false);
                _stars.gameObject.SetActive(false);
                _wasInPipe = true;
                return;
            }

            _visual.gameObject.SetActive(true);
            _glow.gameObject.SetActive(false);

            float target = Coords.GravityAngle(k.Gravity);
            _angle = _wasInPipe ? target : Mathf.LerpAngle(_angle, target, 1f - Mathf.Exp(-dt * 16f));
            _wasInPipe = false;
            Root.transform.rotation = Quaternion.Euler(0, 0, _angle);

            float desired = k.Facing * Coords.TangentSign(k.Gravity);
            _facing = Mathf.MoveTowards(_facing, desired, dt * 14f);
            float fx = Mathf.Abs(_facing) < 0.15f ? 0.15f * Mathf.Sign(desired) : _facing;

            _squash = Mathf.MoveTowards(_squash, 0f, dt * 5f);
            float sq = Mathf.Sin(_squash * Mathf.PI);
            _visual.localScale = new Vector3(fx * (1f + 0.14f * sq), 1f - 0.16f * sq, 1f) * VisualScale;
            _visual.localPosition = L(0, Tuning.Half * (1f - VisualScale * (1f - 0.16f * sq)));

            float along = Dir.IsVertical(k.Gravity) ? k.VX : k.VY;
            _wheel += Mathf.Abs(along) * dt / 10f;
            var wheelRot = Quaternion.Euler(0, 0, -_wheel * Mathf.Rad2Deg);
            _wheelA.transform.localRotation = wheelRot;
            _wheelB.transform.localRotation = wheelRot;

            bool rocket = Look.style == "cohete";
            bool flame = rocket && Mathf.Abs(along) > 80f;
            _flame.gameObject.SetActive(flame);
            if (flame) _flame.transform.localScale = new Vector3(0.8f + Mathf.PerlinNoise(time * 12f, _index) * 0.6f, 1f, 1f);

            float bob = k.Grounded ? Mathf.Sin(time * 7f + _index) * 0.6f : 0f;
            var seat = L(Art.SeatX, Art.SeatY(Look.style) + bob);
            _bichito.localPosition = seat;
            _antenna.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 6f + _index * 2f) * 12f - Mathf.Clamp(along / 40f, -18f, 18f) * Mathf.Sign(fx));

            bool carrying = m.Food.Carrier == _index;
            _food.gameObject.SetActive(carrying);
            if (carrying)
            {
                _food.localPosition = L(0, Art.FoodY + Mathf.Sin(time * 5f) * 2.5f);
                _food.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 3f) * 6f);
            }

            _shield.gameObject.SetActive(k.Shield > 0f);
            if (k.Shield > 0f)
            {
                float blink = k.Shield < 0.5f ? (Mathf.Sin(time * 40f) > 0 ? 1f : 0.3f) : 1f;
                _shield.color = Palette.WithAlpha(Palette.Player(_index), 0.75f * blink);
                _shield.transform.localScale = Vector3.one * (VisualScale * (1f + Mathf.Sin(time * 8f) * 0.04f));
            }

            bool dizzy = k.Stun > 0f;
            _stars.gameObject.SetActive(dizzy);
            if (dizzy)
            {
                _stars.localPosition = L(0, -54);
                for (int i = 0; i < 3; i++)
                {
                    float a = time * 6f + i * Mathf.PI * 2f / 3f;
                    _starSprites[i].transform.localPosition = new Vector3(Mathf.Cos(a) * 0.2f, Mathf.Sin(a) * 0.06f, 0);
                    _starSprites[i].sortingOrder = _order + (Mathf.Sin(a) > 0 ? 12 : 1);
                }
            }

            string expression = dizzy ? "dizzy" : carrying ? "happy" : m.Food.Carrier == 1 - _index ? "angry" : "normal";
            ApplyExpression(expression);

            // Ojos: parpadeo y mirada hacia la comida o el otro karting.
            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blink = 0.12f;
                _blinkTimer = Random.Range(2f, 5f);
            }
            _blink -= dt;
            float eyeScaleY = _blink > 0f && expression != "happy" ? 0.15f : 1f;

            Vector3 lookAt = m.Food.Carrier < 0 ? Coords.ToWorld(m.Food.X, m.Food.Y) : Coords.ToWorld(other.X, other.Y);
            Vector3 dir = Quaternion.Inverse(Root.transform.rotation) * (lookAt - Root.transform.position);
            dir.x *= Mathf.Sign(fx);
            Vector2 look = ((Vector2)dir).normalized * (_rig.EyeRadius * 0.32f / Coords.UnitsPerMeter);
            for (int i = 0; i < _eyes.Length; i++)
            {
                _eyes[i].transform.localScale = new Vector3(1f, eyeScaleY, 1f);
                if (expression != "dizzy") _pupils[i].transform.localPosition = look;
                else _pupils[i].transform.localRotation = Quaternion.Euler(0, 0, -time * 500f);
            }
        }

        private void ApplyExpression(string expression)
        {
            if (expression == _expression) return;
            _expression = expression;
            float r = _rig.EyeRadius;
            bool dot = _rig.DotEyes;
            for (int i = 0; i < _eyes.Length; i++)
            {
                bool happy = expression == "happy";
                _eyes[i].sprite = happy ? Art.HappyEye(r) : Art.EyeWhite(r, dot);
                _pupils[i].gameObject.SetActive(!happy);
                _pupils[i].sprite = expression == "dizzy" ? Art.DizzyEye(r) : Art.Pupil(r, dot);
                _pupils[i].transform.localPosition = Vector3.zero;
                _pupils[i].transform.localRotation = Quaternion.identity;

                bool brows = expression == "angry" || _rig.AlwaysBrows;
                _brows[i].gameObject.SetActive(brows);
                float tilt = _rig.Eyes.Length == 1 ? 0f : (_rig.Eyes[i].x < 0 ? -18f : 18f);
                if (_rig.Eyes.Length == 3 && i == 1) tilt = 0f;
                _brows[i].transform.localRotation = Quaternion.Euler(0, 0, tilt);
            }
            _mouth.sprite = Art.MouthSprite(expression switch
            {
                "happy" => "grin",
                "dizzy" => "wavy",
                "angry" => "flat",
                _ => "smile",
            });
        }
    }
}
