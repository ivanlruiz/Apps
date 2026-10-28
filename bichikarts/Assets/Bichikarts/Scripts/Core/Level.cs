using System;
using System.Collections.Generic;

namespace Bichikarts.Core
{
    /// <summary>Un bloque sólido. Los karting se pueden pegar a cualquiera de sus cuatro caras.</summary>
    public sealed class Solid
    {
        public readonly float X, Y, W, H;
        public readonly string Kind;
        public readonly bool IsFrame;

        public Solid(float x, float y, float w, float h, string kind, bool isFrame = false)
        {
            X = x; Y = y; W = w; H = h; Kind = kind; IsFrame = isFrame;
        }

        public float Right => X + W;
        public float Bottom => Y + H;
    }

    /// <summary>Boca de una tubería: un agujero sobre una superficie. Normal apunta hacia la sala.</summary>
    public sealed class PipeEnd
    {
        public readonly float X, Y;
        public readonly int Normal;

        public PipeEnd(float x, float y, int normal)
        {
            X = x; Y = y; Normal = normal;
        }
    }

    /// <summary>Tubería de dos bocas. El recorrido es una línea quebrada que se dibuja en la pared del fondo.</summary>
    public sealed class Pipe
    {
        public readonly int Id;
        public readonly string Color;
        public readonly PipeEnd[] Ends;
        public readonly float[] Path;
        public readonly float Length;
        private readonly float[] _cumulative;

        public Pipe(int id, string color, PipeEnd a, PipeEnd b, params float[] path)
        {
            if (path.Length < 4 || path.Length % 2 != 0) throw new ArgumentException("El recorrido necesita al menos dos puntos.");
            Id = id; Color = color; Ends = new[] { a, b }; Path = path;
            int n = path.Length / 2;
            _cumulative = new float[n];
            for (int i = 1; i < n; i++)
            {
                float dx = path[i * 2] - path[i * 2 - 2], dy = path[i * 2 + 1] - path[i * 2 - 1];
                _cumulative[i] = _cumulative[i - 1] + MathF.Sqrt(dx * dx + dy * dy);
            }
            Length = _cumulative[n - 1];
        }

        public int PointCount => Path.Length / 2;

        /// <summary>Punto del recorrido en la fracción f (0 = boca A, 1 = boca B).</summary>
        public void PointAt(float f, out float x, out float y)
        {
            float d = Math.Clamp(f, 0f, 1f) * Length;
            int n = PointCount;
            for (int i = 1; i < n; i++)
            {
                if (d <= _cumulative[i] || i == n - 1)
                {
                    float seg = _cumulative[i] - _cumulative[i - 1];
                    float t = seg > 0f ? (d - _cumulative[i - 1]) / seg : 0f;
                    x = Path[i * 2 - 2] + (Path[i * 2] - Path[i * 2 - 2]) * t;
                    y = Path[i * 2 - 1] + (Path[i * 2 + 1] - Path[i * 2 - 1]) * t;
                    return;
                }
            }
            x = Path[0]; y = Path[1];
        }
    }

    /// <summary>Adornos del fondo (no chocan): ventanas, patas de mesa, relojes...</summary>
    public sealed class Decor
    {
        public readonly string Kind;
        public readonly float X, Y, W, H;

        public Decor(string kind, float x, float y, float w = 0f, float h = 0f)
        {
            Kind = kind; X = x; Y = y; W = w; H = h;
        }
    }

    public sealed class Level
    {
        public string Id = "";
        public string Name = "";
        public string Description = "";
        public string Theme = "";
        public string DefaultFood = "miguita";
        public readonly List<Solid> Solids = new List<Solid>();
        public readonly List<Pipe> Pipes = new List<Pipe>();
        public readonly List<Decor> Decor = new List<Decor>();
        public readonly float[] SpawnX = new float[2];
        public readonly float[] SpawnY = new float[2];
        public float FoodX, FoodY;

        public Level AddFrame()
        {
            float w = Tuning.RoomWidth, h = Tuning.RoomHeight, t = Tuning.WallThickness;
            Solids.Add(new Solid(0, h - t, w, t, "piso", true));
            Solids.Add(new Solid(0, 0, w, t, "techo", true));
            Solids.Add(new Solid(0, 0, t, h, "pared", true));
            Solids.Add(new Solid(w - t, 0, t, h, "pared", true));
            return this;
        }

        public Level Block(string kind, float x, float y, float w, float h)
        {
            Solids.Add(new Solid(x, y, w, h, kind));
            return this;
        }

        public Level AddPipe(string color, PipeEnd a, PipeEnd b, params float[] path)
        {
            Pipes.Add(new Pipe(Pipes.Count, color, a, b, path));
            return this;
        }

        public Level Spawns(float x1, float y1, float x2, float y2)
        {
            SpawnX[0] = x1; SpawnY[0] = y1; SpawnX[1] = x2; SpawnY[1] = y2;
            return this;
        }

        public Level FoodAt(float x, float y)
        {
            FoodX = x; FoodY = y;
            return this;
        }

        public Level Add(Decor d)
        {
            Decor.Add(d);
            return this;
        }
    }
}
