using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContextGroup(IFlowContext mainContext) : IFlowContext
    {
        private readonly List<IFlowContextValue> immutableValues = [];
        private readonly List<IFlowContextValue> values = [];

        public UserObject User => mainContext.User;

        public CancellationObject Cancellation => mainContext.Cancellation;

        public FlowContextGroup AddImmutable<T>(T value, out FlowContextValueImmutable<T> contextValue)
        {
            contextValue = new FlowContextValueImmutable<T>(value);
            immutableValues.Insert(0, contextValue);

            return this;
        }

        public FlowContextGroup Add<T>(T value, out FlowContextValue<T> contextValue)
        {
            contextValue = new FlowContextValue<T>(value);
            immutableValues.Insert(0, contextValue);
            values.Insert(0, contextValue);
            return this;
        }

        public void Remove<T>(IFlowContextValue contextValue)
        {
            immutableValues.Remove(contextValue);
            values.Remove(contextValue);
        }

        public bool TryGet<T>(out T? value)
        {
            foreach (var item in immutableValues)
            {
                if (item.TryGet(out value))
                {
                    return true;
                }
            }

            if (mainContext.TryGet(out value))
            {
                return true;
            }

            return false;
        }

        public bool TrySet<T>(T? value)
        {
            foreach (var item in values)
            {
                if (item.TrySet(value))
                {
                    return true;
                }
            }

            if (mainContext.TrySet(value))
            {
                return true;
            }

            return false;
        }

        public bool TrySet<T>(Func<T> select)
        {
            foreach (var item in values)
            {
                if (item.TrySet(select))
                {
                    return true;
                }
            }

            if (mainContext.TrySet(select))
            {
                return true;
            }

            return false;
        }
    }
}
