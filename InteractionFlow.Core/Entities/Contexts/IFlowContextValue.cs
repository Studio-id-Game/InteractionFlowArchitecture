using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public interface IFlowContextValue
    {
        public bool TryGet<T>(out T? value);

        public bool TrySet<T>(T? value);

        public bool TrySet<T>(Func<T> select);
    }
}
