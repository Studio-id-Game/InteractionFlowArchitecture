using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.Externals.Operations;
using InteractionFlow.Standard.Externals.Reactions;
using InteractionFlow.Standard.Externals.SilentExternals;
using System;

namespace InteractionFlow.Standard.Builders
{
    public class ConsoleBuilder : IScopeProfile
    {
        private readonly bool useCancellation;

        public static IScopeProfile Profile { get; } = new ConsoleBuilder(false);
        public static IScopeProfile ProfileUseCancellation { get; } = new ConsoleBuilder(true);

        protected ConsoleBuilder(bool useCancellation)
        {
            this.useCancellation = useCancellation;
        }

        public void Configure(IScopeServices builder)
        {
            builder.UseFunction<IConsoleOperation, ConsoleOperation>();
            builder.UseTransient<IConsoleOperation.IDummy, ConsoleOperation.Dummy>();
            builder.UseFunction<IConsoleWriter, ConsoleWriter>();
            builder.UseFunction<IExceptionPort<Exception>, ConsoleExceptionHandling>();
            builder.UseFunction<ICancellationPort, ConsoleCancellationHandling>();
            builder.UseFunction<IConsoleColorAccess, ConsoleColorAccess>();
            builder.UseFunction<IConsoleCursorPositionAccess, ConsoleCursorPositionAccess>();

            if (useCancellation)
            {
                builder.UseFunction<ICancellationWithConsole, CancellationWithConsole>();
            }
        }
    }
}
