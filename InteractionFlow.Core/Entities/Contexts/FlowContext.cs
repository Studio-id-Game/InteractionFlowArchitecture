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

        public virtual bool TryGet<T>(out T? value)
        {
            value = default;
            return false;
        }

        public virtual bool TrySet<T>(T? value)
        {
            return false;
        }

        public virtual bool TrySet<T>(Func<T> select)
        {
            return false;
        }
    }
}
