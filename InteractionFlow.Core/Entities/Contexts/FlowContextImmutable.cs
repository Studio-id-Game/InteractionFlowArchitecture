using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContextImmutable<T> : IFlowContext.IRead<T>
    {
        private readonly T? value;
        private readonly IFlowContext mainContext;
        public FlowContextImmutable(IFlowContext mainContext, T? value)
        {
            this.mainContext = mainContext;
            this.value = value;
        }

        public UserObject User => mainContext.User;

        public CancellationObject Cancellation => mainContext.Cancellation;

        public void Get(out T? value) => value = this.value;

        public bool TryGet<T1>(out T1? value)
        {
            if (this.value is T1 valueT && valueT != null)
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

        public bool TrySet<T1>(Func<T1> select)
        {
            return false;
        }
    }
}
