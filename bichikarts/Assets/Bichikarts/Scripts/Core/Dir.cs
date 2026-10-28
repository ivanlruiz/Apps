namespace Bichikarts.Core
{
    /// <summary>Las cuatro direcciones de la gravedad. El índice por 90° da el ángulo del karting.</summary>
    public static class Dir
    {
        public const int Down = 0;
        public const int Left = 1;
        public const int Up = 2;
        public const int Right = 3;

        public static readonly int[] X = { 0, -1, 0, 1 };
        public static readonly int[] Y = { 1, 0, -1, 0 };

        public static int Opposite(int d) => (d + 2) & 3;

        /// <summary>True si la gravedad apunta arriba o abajo (el karting anda en horizontal).</summary>
        public static bool IsVertical(int d) => X[d] == 0;

        public static int FromVector(int x, int y)
        {
            if (y > 0) return Down;
            if (x < 0) return Left;
            if (y < 0) return Up;
            return Right;
        }
    }
}
