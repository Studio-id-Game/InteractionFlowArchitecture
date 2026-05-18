using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsoleColorSet(ConsoleColor foreground, ConsoleColor background) : IFunctionState<ConsoleColorSet>
    {
        public static ConsoleColorSet Default { get; } = new ConsoleColorSet(ConsoleColor.Gray, ConsoleColor.Black);

        public ConsoleColor Foreground { get; } = foreground;
        public ConsoleColor Background { get; } = background;

        public ConsoleColorSet Copy()
        {
            return this;
        }
    }
}
