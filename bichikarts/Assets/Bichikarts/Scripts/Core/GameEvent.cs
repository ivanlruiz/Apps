namespace Bichikarts.Core
{
    public enum GameEventType
    {
        Countdown,
        Go,
        Pickup,
        Steal,
        Bump,
        Flip,
        WallStick,
        Jump,
        Land,
        PipeIn,
        PipeOut,
        Tick,
        RoundWin,
        MatchWin,
    }

    /// <summary>Algo que pasó en la simulación y que la capa visual convierte en sonido o partículas.</summary>
    public struct GameEvent
    {
        public GameEventType Type;
        public int Kart;
        public float X, Y;
        public int Value;

        public GameEvent(GameEventType type, int kart, float x, float y, int value = 0)
        {
            Type = type; Kart = kart; X = x; Y = y; Value = value;
        }
    }
}
