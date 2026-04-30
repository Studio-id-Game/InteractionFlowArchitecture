using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContextMutable<T> : IFlowContext.IRead<T>, IFlowContext.IWrite<T>
    {
        private T? value;
        private readonly IFlowContext mainContext;

        public FlowContextMutable(IFlowContext mainContext, T? value)
        {
            this.mainContext = mainContext;
            this.value = value;
        }

        public UserObject User => mainContext.User;

        public CancellationObject Cancellation => mainContext.Cancellation;

        public void Get(out T? value) => value = this.value;

        public void Set(T? value) => this.value = value;

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
            if (value is T valueT && valueT != null)
            {
                this.value = valueT;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TrySet<T1>(Func<T1> select)
        {

            if (select is Func<T> selectT)
            {
                value = selectT();
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
