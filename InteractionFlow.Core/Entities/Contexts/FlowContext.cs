using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContext : IFlowContext
    {
        public FlowContext(UserObject userToken)
        {
            User = userToken;
        }

        public FlowContext(UserObject userToken, CancellationObject cancellation)
        {
            User = userToken;
            Cancellation = cancellation;
        }

        public UserObject User { get; }

        public CancellationObject Cancellation { get; } = new();

        public bool TryGet<T>(out T? value)
        {
            if (this is IFlowContext.IRead<T> read)
            {
                read.Get(out value);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TrySet<T>(T? value)
        {
            if (this is IFlowContext.IWrite<T> write)
            {
                write.Set(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TrySet<T>(Func<T> select)
        {
            if (this is IFlowContext.IWrite<T> mutable)
            {
                mutable.Set(select());
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
