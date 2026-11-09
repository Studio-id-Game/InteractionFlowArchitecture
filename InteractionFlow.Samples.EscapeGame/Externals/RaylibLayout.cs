using Raylib_cs;

namespace InteractionFlow.Samples.EscapeGame.Externals;

internal static class RaylibLayout
{
    internal const int Width = 960;
    internal const int Height = 720;
    internal const int BoardX = 32;
    internal const int BoardY = 120;
    internal const int Cell = 76;

    internal static Rectangle GetButtonBounds(int index)
        => new(32 + index % 4 * 224, 566 + index / 4 * 56, 208, 42);
}
