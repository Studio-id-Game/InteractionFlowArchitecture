using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Reactions
{
    public abstract class ExceptionHandling(params IFlowNode[] dependency) : ExceptionHandling<Exception>(dependency)
    {
    }

    public abstract class ExceptionHandling<TException>(params IFlowNode[] dependency) : Reaction(dependency), IExceptionPort<TException>
        where TException : Exception
    {
        public bool ThrowException { get; set; } = false;

        public ValueTask<FlowEndToken> HandleExceptionAsync(IFlowContext context, TException exception)
        {
            if (ThrowException)
            {
                throw exception;
            }
            else
            {
                return HandleExceptionCoreAsync(context, exception);
            }
        }

        protected abstract ValueTask<FlowEndToken> HandleExceptionCoreAsync(IFlowContext context, TException exception);
    }
}
