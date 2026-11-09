using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public sealed class Login(
        IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadLoginOperation login)
        : Interaction(exceptionPort, cancellationPort, output, binding, login)
    {
        protected override Task OnCancellation(IFlowContext context)
        {
            if (context.TryGet<NotepadContext>(out var notepad)) notepad.ExitRequested = true;
            return Task.CompletedTask;
        }

        protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IFlowContext flowContext)
        {
            var context = flowContext.GetRequired<NotepadContext>();
            using var bindingScope = binding.Bind(flowContext);
            var result = await login.LoginAsync(context);
            return await output.WriteLineAsync(context, result.Try(out var error)
                ? context.ExitRequested ? "Goodbye!" : $"Logged in: {NotepadRules.UserName(context.User!)}"
                : $"Login failed: {error.Message}");
        }
    }
}
