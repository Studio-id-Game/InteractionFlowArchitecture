using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public class ConsoleState(ConsoleColor backgroundColor, ConsoleColor foregroundColor, bool writeLine) : IFunctionState<ConsoleState>
    {
        public ConsoleState(ConsoleColorSet colorSet, bool writeLine) : this(colorSet.Background, colorSet.Foreground, writeLine)
        {

        }

        public static ConsoleState Default => new(ConsoleColorSet.Default, true);

        public static ConsoleState DefaultNoLine => new(ConsoleColorSet.Default, false);

        public ConsoleColor backgroundColor = backgroundColor;

        public ConsoleColor foregroundColor = foregroundColor;

        public bool writeLine = writeLine;

        public ConsoleColorSet ColorSet
        {
            get => new(foregroundColor, backgroundColor);
            set
            {
                foregroundColor = value.Foreground;
                backgroundColor = value.Background;
            }
        }

        public void Update(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null)
        {
            if (backgroundColor != null)
                this.backgroundColor = backgroundColor.Value;

            if (foregroundColor != null)
                this.foregroundColor = foregroundColor.Value;

            if (writeLine != null)
                this.writeLine = writeLine.Value;
        }

        public ConsoleState Copy()
        {
            return new(backgroundColor, foregroundColor, writeLine);
        }
    }
}
