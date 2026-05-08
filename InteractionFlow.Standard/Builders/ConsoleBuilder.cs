using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.Operations;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.Reactions;
using System;

namespace InteractionFlow.Standard.Builders
{
    public sealed class ConsoleBuilder : IScopeProfile
    {
        public static IScopeProfile Profile { get; } = new ConsoleBuilder();

        private ConsoleBuilder() { }

        public void Configure(IScopeServices builder)
        {
            builder.UseFunction<IConsoleOperation, ConsoleOperation>();
            builder.UseTransient<IConsoleOperation.IDummy, ConsoleOperation.Dummy>();

            builder.UseFunction<IConsoleWriter, ConsoleWriter>();
            builder.UseFunction<IExceptionPort<Exception>, ConsoleExceptionHandling>();
            builder.UseFunction<ICancellationPort, ConsoleCancellationHandling>();
        }
    }
}
