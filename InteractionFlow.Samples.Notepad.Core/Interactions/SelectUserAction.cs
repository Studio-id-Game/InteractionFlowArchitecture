using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public sealed class SelectUserAction(
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, IConsoleChoiceOperation choices)
        : NotepadInteraction(input, output, binding, exceptionPort, cancellationPort, choices)
    {
        protected override Task OnCancellation(IFlowContext context)
        {
            if (context.TryGet<NotepadContext>(out var notepad)) notepad.ExitRequested = true;
            return Task.CompletedTask;
        }

        protected override async Task<ReactionEnd> RunAsync(NotepadContext context)
        {
            context.Action = NotepadAction.None;
            var selected = await choices.SelectAsync(context, "Select your action",
                new[] { "Edit", "Create", "Delete", "Switch user", "List", "Exit" });
            if (selected == null || selected == 5) context.ExitRequested = true;
            else context.Action = selected.Value switch
            {
                0 => NotepadAction.Edit, 1 => NotepadAction.Create,
                2 => NotepadAction.Delete, 3 => NotepadAction.Login,
                4 => NotepadAction.List, _ => NotepadAction.None
            };
            return await Output.WriteLineAsync(context, context.ExitRequested ? "Goodbye!" :
                context.Action == NotepadAction.None ? "Choose an action from 0 to 5." : $"Action: {context.Action}");
        }
    }
}
