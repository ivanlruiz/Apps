namespace Bichikarts.Core
{
    /// <summary>
    /// Números que definen cómo se siente el juego. Las distancias están en "píxeles de sala":
    /// la sala mide 1600 x 900 y el eje Y apunta hacia abajo (la capa de Unity lo convierte).
    /// </summary>
    public static class Tuning
    {
        public const float RoomWidth = 1600f;
        public const float RoomHeight = 900f;
        public const float WallThickness = 50f;

        // El karting choca como un cuadrado de 46 x 46.
        public const float Half = 23f;

        public const float Gravity = 2400f;
        public const float MaxFall = 1200f;

        public const float Speed = 520f;
        public const float AccelGround = 2800f;
        public const float AccelAir = 1600f;
        public const float Friction = 2400f;
        public const float AirDrag = 300f;

        public const float JumpSpeed = 900f;
        public const float CoyoteTime = 0.08f;
        public const float JumpBuffer = 0.12f;

        public const float FlipCooldown = 0.3f;
        public const float FlipKick = 160f;

        // Con la comida vas más lento; el que persigue va más rápido.
        public const float CarrierSpeed = 0.85f;
        public const float ChaserSpeed = 1.1f;

        public const float StealStun = 1.0f;
        public const float StealShield = 2.0f;
        public const float PickupShield = 0.6f;
        public const float Knockback = 650f;

        public const float KartRadius = 22f;
        public const float FoodRadius = 20f;

        public const float PipeSpeed = 1100f;
        public const float PipeMinTime = 0.45f;
        public const float PipeCooldown = 0.5f;
        public const float PipePop = 260f;
        public const float MouthTolerance = 24f;
        public const float MouthWidth = 74f;

        public const float CountdownTime = 3f;
        public const float RoundOverTime = 3f;
        public const float MatchOverTime = 3.5f;

        public const float Step = 1f / 120f;
    }
}
