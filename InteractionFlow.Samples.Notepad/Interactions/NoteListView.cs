using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteListView(
        IExceptionPort exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleReaction consoleReaction,
        INotepadUserDataFiles notepadUserDataFiles,
        INotepadDataFiles notepadDataFiles) :
        Interaction(exceptionPort, cancellationPort)
    {
        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            return await TryCatchBlock(context, async context =>
            {
                await WriteLine(context, "# Note List View :");

                var userDataLoadResult = await notepadUserDataFiles.LoadFromPersistent(context);

                if (!userDataLoadResult)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not load NotepadUserData."));
                }

                var userData = userDataLoadResult.Value!;

                var noteContext = new FlowContextGroup(context)
                    .Add(NotepadDataKey.Empty, out var noteDataKeyContext);

                foreach (var noteDataKey in userData)
                {
                    noteDataKeyContext.Value = noteDataKey;
                    var noteDataLoadResult = await notepadDataFiles.LoadFromPersistent(noteContext);
                    if (noteDataLoadResult)
                    {
                        var fileName = noteDataKey.NoteId;
                        var title = noteDataLoadResult.Value!.Title;
                        await WriteLine(context, $"  - '{fileName}' (title:{title})");
                    }
                }

                return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> End of List."));
            });
        }

        private async Task WriteLine(IFlowContext context, string text)
        {
            using var scope = consoleReaction.State.Customize(e => consoleReaction.State = e);
            scope.Set(writeLine: true);
            await consoleReaction.ReactToUserAsync(context, new ConsoleOutput(text));
        }
    }
}
