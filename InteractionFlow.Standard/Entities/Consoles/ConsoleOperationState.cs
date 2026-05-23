using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public class ConsoleOperationState(ConsoleColor backgroundColor, ConsoleColor foregroundColor, bool writeLine, int cancelWaitTime)
        : ConsoleState(backgroundColor, foregroundColor, writeLine), IFunctionState<ConsoleOperationState>
    {
        public static new ConsoleOperationState Default => new(ConsoleState.Default, 100);

        public static new ConsoleOperationState DefaultNoLine => new(ConsoleState.DefaultNoLine, 100);

        public ConsoleOperationState(ConsoleState state, int cancelWaitTime) : this(state.backgroundColor, state.foregroundColor, state.writeLine, cancelWaitTime)
        {

        }

        public int cancelWaitTime = cancelWaitTime;

        public ConsoleState ConsoleState
        {
            get => this;
            set
            {
                backgroundColor = value.backgroundColor;
                foregroundColor = value.foregroundColor;
                writeLine = value.writeLine;
            }
        }

        public void Update(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null, int? cancelWaitTime = null)
        {
            ConsoleState.Update(foregroundColor, backgroundColor, writeLine);

            if (cancelWaitTime != null)
                this.cancelWaitTime = cancelWaitTime.Value;
        }

        public new ConsoleOperationState Copy()
        {
            return new(backgroundColor, foregroundColor, writeLine, cancelWaitTime);
        }
    }
}
