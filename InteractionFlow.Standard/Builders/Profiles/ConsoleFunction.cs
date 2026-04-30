using InteractionFlow.Core.Builders;
using InteractionFlow.Core.OperationPorts;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.Operations;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.Reactions;

namespace InteractionFlow.Standard.Builders.Profiles
{
    public sealed class ConsoleFunction : IScopeProfile
    {
        public static IScopeProfile Profile { get; } = new ConsoleFunction();

        private ConsoleFunction() { }

        public void Configure(IScopeServices builder)
        {
            builder.Use<IConsoleOperation, ConsoleOperation>();
            builder.Use<IOperationPort<ConsoleInputText>, ConsoleOperation>();
            builder.Use<IOperationPort<ConsoleInputKeyInfo>, ConsoleOperation>();
            builder.Use<IOperationPort<ConsoleInputAnyKey>, ConsoleOperation>();
            builder.UseTransient<IConsoleOperation.IDummy, ConsoleOperation.Dummy>();

            builder.Use<IConsoleReaction, ConsoleReaction>();
            builder.Use<IReactionPort<ConsoleOutput>, ConsoleReaction>();
            builder.Use<IExceptionPort, ConsoleReaction>();
            builder.Use<ICancellationPort, ConsoleReaction>();
        }
    }
}
