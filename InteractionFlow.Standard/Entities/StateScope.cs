using System;

namespace InteractionFlow.Standard.Entities
{
    public readonly ref struct StateScope<TTarget, TState>(
        TTarget target,
        TState current,
        Action<TTarget, TState> setter)
        where TTarget : class
        where TState : IClonableState<TState>
    {
        readonly TState _current = current.Copy();

        public readonly void Dispose()
        {
            setter(target, _current);
        }
    }
}
