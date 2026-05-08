using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public struct ConsoleState(ConsoleColor foregroundColor, ConsoleColor backgroundColor, bool writeLine) : IClonableState<ConsoleState>
    {
        public static ConsoleState Default { get; } = new ConsoleState()
        {
            foregroundColor = ConsoleColor.Gray,
            backgroundColor = ConsoleColor.Black,
            writeLine = true,
        };

        public static ConsoleState DefaultNoLine { get; } = new ConsoleState()
        {
            foregroundColor = ConsoleColor.Gray,
            backgroundColor = ConsoleColor.Black,
            writeLine = false,
        };

        public ConsoleColor foregroundColor = foregroundColor;

        public ConsoleColor backgroundColor = backgroundColor;

        public bool writeLine = writeLine;

        public void Update(ConsoleColor? foregroundColor, ConsoleColor? backgroundColor, bool? writeLine)
        {
            if (foregroundColor != null)
                this.foregroundColor = foregroundColor.Value;

            if (backgroundColor != null)
                this.backgroundColor = backgroundColor.Value;

            if (writeLine != null)
                this.writeLine = writeLine.Value;
        }

        public readonly StateScope<TTarget, ConsoleState> GetScope<TTarget>(TTarget target, Action<TTarget, ConsoleState> setter)
            where TTarget : class
        {
            return new(target, this, setter);
        }

        public readonly ConsoleState Copy()
        {
            return this;
        }
    }
}
