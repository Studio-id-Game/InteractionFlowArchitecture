using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public struct ConsoleState(ConsoleColorSet colorSet, bool writeLine) : IFunctionState<ConsoleState>
    {
        public static ConsoleState Default { get; } = new ConsoleState()
        {
            colorSet = ConsoleColorSet.Default,
            writeLine = true,
        };

        public static ConsoleState DefaultNoLine { get; } = new ConsoleState()
        {
            colorSet = ConsoleColorSet.Default,
            writeLine = false,
        };

        public ConsoleColorSet colorSet = colorSet;

        public bool writeLine = writeLine;

        public readonly ConsoleColor BackgroundColor => colorSet.Background;

        public readonly ConsoleColor ForegroundColor => colorSet.Foreground;

        public ConsoleState Update(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null)
        {
            if (foregroundColor != null && backgroundColor != null)
                colorSet = new(foregroundColor.Value, backgroundColor.Value);

            else if (foregroundColor != null)
                colorSet = new(foregroundColor.Value, BackgroundColor);

            else if (backgroundColor != null)
                colorSet = new(ForegroundColor, backgroundColor.Value);

            if (writeLine != null)
                this.writeLine = writeLine.Value;

            return this;
        }

        public readonly ConsoleState Copy()
        {
            return this;
        }
    }
}
