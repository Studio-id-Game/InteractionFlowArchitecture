using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public sealed class FlowContextValueImmutable<T>(T value) : IFlowContextValue
    {
        public T Value { get; } = value;

        public bool TryGet<T1>(out T1? value)
        {
            if (Value is T1 valueT)
            {
                value = valueT;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TrySet<T1>(T1? value)
        {
            return false;
        }

        public bool TrySet<T1>(Func<T1?> select)
        {
            return false;
        }
    }
}
