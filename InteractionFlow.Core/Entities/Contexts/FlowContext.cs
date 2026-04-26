using InteractionFlow.Core.Entities.Rules.Architectures;
using System;
using System.Threading;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContext : IFlowContext
    {
        public FlowContext(UserToken userToken, CancellationToken cancellationToken)
        {
            UserToken = userToken;
            CancellationToken = cancellationToken;
        }

        public UserToken UserToken { get; }

        public CancellationToken CancellationToken { get; }

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