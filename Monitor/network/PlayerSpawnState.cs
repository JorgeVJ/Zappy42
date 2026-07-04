using Godot;

public partial class Connection
{
    /// <summary>
    /// Datos de posición/orientación/nivel de un jugador en el momento del spawn
    /// o reconexión (pnw), agrupados para no exceder el máximo de parámetros por
    /// método.
    /// </summary>
    private readonly struct PlayerSpawnState
    {
        public readonly Vector3 WorldPos;
        public readonly int X;
        public readonly int Y;
        public readonly int Orientation;
        public readonly int Level;

        public PlayerSpawnState(Vector3 worldPos, int x, int y, int orientation, int level)
        {
            WorldPos = worldPos;
            X = x;
            Y = y;
            Orientation = orientation;
            Level = level;
        }
    }
}
