using System;

namespace InteractionFlow.Samples.EscapeGame.Entities;

public sealed class RoomDefinition
{
    private static readonly string[] rows = ["#######", "#K.G#X#", "#....D#", "#P.S..#", "#######"];
    private static readonly GridPosition[] patrol = [new(3, 1), new(3, 2), new(2, 2), new(2, 1)];
    public static RoomDefinition Default { get; } = new();
    private RoomDefinition() { }
    public int Width => 7;
    public int Height => 5;
    public GridPosition PlayerStart => new(1, 3);
    public GridPosition Key => new(1, 1);
    public GridPosition Switch => new(3, 3);
    public GridPosition Door => new(5, 2);
    public GridPosition Exit => new(5, 1);
    public int PatrolLength => patrol.Length;
    public GridPosition PatrolAt(int index) => patrol[index];
    public char TileAt(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : rows[y][x];
    public bool CanEnter(GridPosition position, bool doorUnlocked)
        => TileAt(position.X, position.Y) != '#' && (position != Door || doorUnlocked);
}
