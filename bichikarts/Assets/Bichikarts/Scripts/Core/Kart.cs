namespace Bichikarts.Core
{
    /// <summary>Lo que un jugador (persona o compu) pide en un paso de simulación.</summary>
    public struct KartInput
    {
        /// <summary>Dirección en la pantalla: -1, 0 o 1 (Y positivo = abajo).</summary>
        public int X, Y;

        /// <summary>Se apretó el botón de gravedad en este paso.</summary>
        public bool Flip;

        /// <summary>Se apretó un botón de salto dedicado (opcional; empujar hacia afuera también salta).</summary>
        public bool Jump;

        public static readonly KartInput None = default;
    }

    public struct PipeTravel
    {
        public bool Active;
        public int Pipe;
        public int From;
        public float T;
        public float Duration;
    }

    public sealed class Kart
    {
        public int Index;
        public float X, Y, VX, VY;

        /// <summary>Hacia dónde tira la gravedad de este karting (ver <see cref="Dir"/>).</summary>
        public int Gravity;

        public bool Grounded;
        public float CoyoteTimer, JumpBufferTimer;
        public int AirFlips;
        public float FlipCooldown, Stun, Shield, PipeCooldown;
        public PipeTravel Pipe;
        public bool AwayHeld, IntoLock;

        /// <summary>Hacia dónde mira sobre la superficie (+1 / -1 en el eje tangente).</summary>
        public int Facing = 1;

        public float WheelTurn;
        public float Handicap = 1f;

        public Kart(int index, float x, float y)
        {
            Index = index; X = x; Y = y;
            Gravity = Dir.Down;
            AirFlips = 1;
            Facing = index == 0 ? 1 : -1;
        }

        public bool InPipe => Pipe.Active;

        public Kart Clone() => (Kart)MemberwiseClone();
    }
}
