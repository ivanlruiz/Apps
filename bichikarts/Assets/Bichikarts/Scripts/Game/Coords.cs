using Bichikarts.Core;
using UnityEngine;

namespace Bichikarts
{
    /// <summary>Convierte de la sala del núcleo (píxeles, Y hacia abajo) al mundo de Unity (metros, Y hacia arriba).</summary>
    public static class Coords
    {
        public const float UnitsPerMeter = 100f;

        public static Vector3 ToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(x / UnitsPerMeter, (Tuning.RoomHeight - y) / UnitsPerMeter, z);
        }

        public static float ToWorldLength(float units) => units / UnitsPerMeter;

        /// <summary>Hacia dónde apunta una dirección del núcleo, en el mundo de Unity.</summary>
        public static Vector2 WorldDir(int dir) => new Vector2(Dir.X[dir], -Dir.Y[dir]);

        /// <summary>Rotación (grados) del karting para que sus ruedas apunten hacia su gravedad.</summary>
        public static float GravityAngle(int gravity) => -90f * gravity;

        /// <summary>
        /// +1 si el frente del karting (su X local) coincide con el sentido positivo de la superficie
        /// en el núcleo, -1 si está al revés (en el techo y en la pared derecha).
        /// </summary>
        public static int TangentSign(int gravity) => gravity == Dir.Down || gravity == Dir.Left ? 1 : -1;
    }

    public static class Palette
    {
        public static readonly Color Ink = Hex("#1a1030");
        public static readonly Color Space = Hex("#160e2c");
        public static readonly Color Panel = Hex("#23184a");
        public static readonly Color PanelLight = Hex("#33246a");
        public static readonly Color Crust = Hex("#e9993f");
        public static readonly Color CrustDeep = Hex("#b5651d");
        public static readonly Color Crumb = Hex("#fbe6bd");
        public static readonly Color Cheese = Hex("#ffd23f");
        public static readonly Color Alien = Hex("#79f0c3");
        public static readonly Color Muted = Hex("#b9a8e0");
        public static readonly Color White = Color.white;

        public static Color Player(int i) => Hex(Catalog.PlayerColors[i]);

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        /// <summary>Aclara (amount &gt; 0) u oscurece (amount &lt; 0) un color.</summary>
        public static Color Shade(Color c, float amount)
        {
            var target = amount >= 0f ? Color.white : Color.black;
            var r = Color.Lerp(c, target, Mathf.Abs(amount));
            r.a = c.a;
            return r;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
