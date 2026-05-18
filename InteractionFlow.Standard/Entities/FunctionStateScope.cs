using System;

namespace InteractionFlow.Standard.Entities
{
    public readonly struct FunctionStateScope<TState>(IHasFunctionState<TState> target) : IDisposable
        where TState : IFunctionState<TState>
    {
        private readonly TState unscopedState = target.State.Copy();

        public TState State
        {
            get => target.State;
            set => target.State = value;
        }

        public readonly void Dispose()
        {
            target.State = unscopedState;
        }
    }
}
