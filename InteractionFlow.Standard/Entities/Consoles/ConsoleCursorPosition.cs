namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsoleCursorPosition(int? left, int? top) : IFunctionState<ConsoleCursorPosition>
    {
        public int? Left { get; } = left;

        public int? Top { get; } = top;

        public ConsoleCursorPosition Copy()
        {
            return this;
        }
    }
}
