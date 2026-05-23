using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using InteractionFlow.Samples.Notepad.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ProgramFlows
{
    public class MainLoop(
        Login login,
        NoteListView noteListView,
        SelectUserAction selectUserAction,
        ConsoleWriting consoleWrite)
        : ProgramFlow<NotepadContext>(login, noteListView, selectUserAction, consoleWrite)
    {

        public override async Task<FlowEndToken> ExecuteAsync(NotepadContext context)
        {
            var end = await login.ExecuteAsync(context);
            await Write("");

            do
            {
                var loginedContext = end.LastContext as NotepadContext ?? throw new Exception();

                await noteListView.ExecuteAsync(loginedContext);
                await Write("");

                end = await selectUserAction.ExecuteAsync(loginedContext);
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
                return await consoleWrite.ExecuteAsync(context, (new ConsoleOutput(text), null));
            }
        }
    }
}
