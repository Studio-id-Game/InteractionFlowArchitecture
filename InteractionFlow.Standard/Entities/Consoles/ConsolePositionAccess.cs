namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsoleCursorPosition(int? left, int? top)
    {
        public int? Left { get; } = left;

        public int? Top { get; } = top;
    }
}
