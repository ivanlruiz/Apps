using UnityEngine;

namespace Bichikarts
{
    /// <summary>Partículas simples con un pool de sprites: polvo, migas, estrellitas y ondas.</summary>
    public sealed class Fx
    {
        private struct Particle
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel, Accel;
            public float Life, Max, Size0, Size1, Spin, Angle;
            public Color C0, C1;
            public bool Active;
        }

        private readonly Particle[] _pool;
        private readonly Transform _root;
        private int _next;

        public Fx(Transform parent, int capacity = 320)
        {
            _root = new GameObject("Efectos").transform;
            _root.SetParent(parent, false);
            _pool = new Particle[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var sr = new GameObject("p").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(_root, false);
                sr.gameObject.SetActive(false);
                _pool[i].Sr = sr;
            }
        }

        public void Emit(Sprite sprite, Vector2 pos, Vector2 vel, float life, float size0, float size1, Color c0, Color c1,
            Vector2 accel = default, float spin = 0f, int order = 70)
        {
            ref var p = ref _pool[_next];
            _next = (_next + 1) % _pool.Length;
            p.Pos = pos; p.Vel = vel; p.Accel = accel;
            p.Life = 0f; p.Max = life; p.Size0 = size0; p.Size1 = size1;
            p.C0 = c0; p.C1 = c1; p.Spin = spin; p.Angle = Random.Range(0f, 360f);
            p.Active = true;
            p.Sr.sprite = sprite;
            p.Sr.sortingOrder = order;
            p.Sr.gameObject.SetActive(true);
            Apply(ref p);
        }

        private static void Apply(ref Particle p)
        {
            float t = p.Max > 0f ? p.Life / p.Max : 1f;
            p.Sr.transform.position = p.Pos;
            p.Sr.transform.rotation = Quaternion.Euler(0, 0, p.Angle);
            p.Sr.transform.localScale = Vector3.one * Mathf.Lerp(p.Size0, p.Size1, t);
            p.Sr.color = Color.Lerp(p.C0, p.C1, t);
        }

        public void Update(float dt)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                ref var p = ref _pool[i];
                if (!p.Active) continue;
                p.Life += dt;
                if (p.Life >= p.Max)
                {
                    p.Active = false;
                    p.Sr.gameObject.SetActive(false);
                    continue;
                }
                p.Vel += p.Accel * dt;
                p.Pos += p.Vel * dt;
                p.Angle += p.Spin * dt;
                Apply(ref p);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i].Active = false;
                _pool[i].Sr.gameObject.SetActive(false);
            }
        }

        // --- Recetas ---

        /// <summary>Polvito que sale de las ruedas. "up" es hacia afuera de la superficie (mundo).</summary>
        public void Dust(Vector2 pos, Vector2 up, int count, float strength = 1f)
        {
            var tangent = new Vector2(up.y, -up.x);
            for (int i = 0; i < count; i++)
            {
                var v = (up * Random.Range(0.3f, 1.1f) + tangent * Random.Range(-1.4f, 1.4f)) * strength;
                Emit(Art.Soft(), pos + tangent * Random.Range(-0.15f, 0.15f), v, Random.Range(0.35f, 0.6f), 0.25f, 0.6f,
                    new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0f), -v * 1.5f);
            }
        }

        public void Crumbs(Vector2 pos, Vector2 down, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-1.2f, 1.2f)) - down * Random.Range(0.4f, 1.6f);
                Emit(Art.Crumb(), pos, v, Random.Range(0.5f, 0.9f), 0.7f, 0.5f, Color.white, new Color(1f, 1f, 1f, 0f), down * 9f, Random.Range(-400f, 400f), 69);
            }
        }

        public void Burst(Vector2 pos, Color color, int count, float speed = 3f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(speed * 0.4f, speed);
                Emit(Art.Star(), pos, v, Random.Range(0.45f, 0.8f), 0.9f, 0.2f, color, Palette.WithAlpha(color, 0f), -v * 1.2f, Random.Range(-500f, 500f), 72);
            }
        }

        public void Wave(Vector2 pos, Color color, float size = 1.6f)
        {
            Emit(Art.Ring(), pos, Vector2.zero, 0.45f, 0.3f, size, Palette.WithAlpha(color, 0.9f), Palette.WithAlpha(color, 0f), default, 0f, 71);
        }
    }
}
