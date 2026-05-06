using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Interactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Focuses
{
    internal class MainLoop(
        Login login,
        NoteCreate noteCreate,
        NoteDelete noteDelete,
        NoteEdit noteEdit,
        NoteListView noteListView,
        SelectUserAction selectUserAction,
        ConsoleWrite consoleWrite)
        : Focus<NotepadContext>
    {
        public override IEnumerable<IInteraction> Interactions
        {
            get
            {
                yield return login;
                yield return noteCreate;
                yield return noteDelete;
                yield return noteEdit;
                yield return noteListView;
                yield return selectUserAction;
            }
        }

        public override async Task<FlowEndToken> FlowWithUserAsync(NotepadContext context)
        {
            var newContext = new FlowContextGroup(context)
                .Add<ConsoleOutput>(default, out var textContext);

            var end = await login.InteractWithUserAsync(context);
            await Write("");

            do
            {
                var loginedContext = end.LastContext as NotepadContext ?? throw new Exception();

                await noteListView.InteractWithUserAsync(loginedContext);
                await Write("");

                end = await selectUserAction.InteractWithUserAsync(loginedContext);
                await Write("");
                if (end.HasCanceled)
                {
                    break;
                }
            }
            while (true);

            return end;

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Value = new ConsoleOutput(text);
                return await consoleWrite.InteractWithUserAsync(newContext);
            }
        }
    }
}
