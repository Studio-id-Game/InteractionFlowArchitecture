using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public sealed class FlowEndToken
    {
        public IFlowContext LastContext { get; }

        public OperationCanceledException? CanceledException { get; set; }

        public bool HasCanceledException => CanceledException != null;

        public Exception? Exception { get; set; }

        public bool HasException => Exception != null;

        internal FlowEndToken(IFlowContext lastContext)
        {
            LastContext = lastContext;
        }
    }
}
