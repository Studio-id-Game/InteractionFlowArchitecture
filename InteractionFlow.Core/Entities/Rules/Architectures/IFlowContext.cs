using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading;

namespace InteractionFlow.Core.Entities.Rules.Architectures
{
    public interface IFlowContext
    {
        public interface IRead<T> : IFlowContext
        {
            public void Get(out T? value);
        }

        public interface IWrite<T> : IFlowContext
        {
            public void Set(T? value);
        }

        public UserToken UserToken { get; }

        public CancellationToken CancellationToken { get; }

        public bool TryGet<T>(out T? value);

        public bool TrySet<T>(T? value);

        public bool TrySet<T>(Func<T> select);
    }
}