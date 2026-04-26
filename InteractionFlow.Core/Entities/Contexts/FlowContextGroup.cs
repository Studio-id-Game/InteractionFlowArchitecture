using InteractionFlow.Core.Entities.Rules.Architectures;
using System;
using System.Collections.Generic;
using System.Threading;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class FlowContextGroup : IFlowContext
    {
        private readonly List<IFlowContext> subReadableContexts;
        private readonly List<IFlowContext> subWritableContexts;

        private readonly IFlowContext mainContext;


        public FlowContextGroup(IFlowContext mainContext)
        {
            this.mainContext = mainContext;
            subReadableContexts = new();
            subWritableContexts = new();
        }

        public UserToken UserToken => mainContext.UserToken;

        public CancellationToken CancellationToken => mainContext.CancellationToken;

        public FlowContextGroup Add<T>(T? value, out FlowContextImmutable<T> subContext)
        {
            subContext = new FlowContextImmutable<T>(mainContext, value);
            subReadableContexts.Insert(0, subContext);

            return this;
        }

        public FlowContextGroup AddMutable<T>(T? value, out FlowContextMutable<T> subContext)
        {
            subContext = new FlowContextMutable<T>(mainContext, value);
            subReadableContexts.Insert(0, subContext);
            subWritableContexts.Insert(0, subContext);

            return this;
        }

        public void Remove<T>(IFlowContext subContext)
        {
            subReadableContexts.Remove(subContext);
            subWritableContexts.Remove(subContext);
        }

        public bool TryGet<T>(out T? value)
        {
            foreach (var item in subReadableContexts)
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
            foreach (var item in subWritableContexts)
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
            foreach (var item in subWritableContexts)
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
