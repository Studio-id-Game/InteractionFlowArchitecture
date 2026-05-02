using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public sealed class FlowContextValue<T> : IFlowContextValue
    {
        public T Value { get; set; }

        public FlowContextValue(T value)
        {
            Value = value;
        }

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
            if (value != null && value is T valueT)
            {
                Value = valueT;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TrySet<T1>(Func<T1?> select)
        {
            if (select is Func<T?> selectT)
            {
                var value = selectT();
                if (value != null)
                {
                    Value = value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
