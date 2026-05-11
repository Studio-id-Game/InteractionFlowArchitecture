using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public sealed class FlowEndToken
    {
        public IFlowContext LastContext { get; }

        public Exception? Exception { get; set; }

        public OperationCanceledException? CanceledException
        {
            get => Exception as OperationCanceledException;
            set => Exception = value;
        }

        public bool HasException => Exception != null;

        public bool HasCanceled => HasException && Exception is OperationCanceledException;

        internal FlowEndToken(IFlowContext lastContext)
        {
            LastContext = lastContext;
        }
    }
}
