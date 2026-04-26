using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public struct ConsoleState
    {
        internal readonly struct UseScope : IDisposable
        {
            readonly ConsoleColor _ForegroundColor;
            readonly ConsoleColor _BackgroundColor;
            readonly bool _WriteLine;

            internal UseScope(ConsoleState state)
            {
                _ForegroundColor = Console.ForegroundColor;
                _BackgroundColor = Console.BackgroundColor;
                _WriteLine = state.writeLine;

                Console.ForegroundColor = state.foregroundColor;
                Console.BackgroundColor = state.backgroundColor;
            }

            public readonly void Dispose()
            {
                Console.ForegroundColor = _ForegroundColor;
                Console.BackgroundColor = _BackgroundColor;

                if (_WriteLine)
                {
                    Console.WriteLine();
                }
            }
        }

        public readonly struct CustomizeScope : IDisposable
        {
            private readonly ConsoleState defaultState;
            private readonly Action<ConsoleState> setter;

            public CustomizeScope(ConsoleState currentState, Action<ConsoleState> setter)
            {
                defaultState = currentState;
                this.setter = setter;
            }

            public void Set(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null)
            {
                var set = defaultState;
                if (foregroundColor != null) set.foregroundColor = foregroundColor.Value;
                if (backgroundColor != null) set.backgroundColor = backgroundColor.Value;
                if (writeLine != null) set.writeLine = writeLine.Value;
                setter(set);
            }

            public void Reset() => setter(defaultState);

            public void Dispose()
            {
                Reset();
            }
        }

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



        public ConsoleColor foregroundColor;

        public ConsoleColor backgroundColor;

        public bool writeLine;

        public ConsoleState(ConsoleColor foregroundColor, ConsoleColor backgroundColor, bool writeLine)
        {
            this.foregroundColor = foregroundColor;
            this.backgroundColor = backgroundColor;
            this.writeLine = writeLine;
        }

        internal readonly UseScope Use() => new(this);

        public readonly CustomizeScope Customize(Action<ConsoleState> reset) => new(this, reset);
    }
}