namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;

public readonly record struct GridPosition(int X, int Y)
{
    public int DistanceTo(GridPosition other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);
}
