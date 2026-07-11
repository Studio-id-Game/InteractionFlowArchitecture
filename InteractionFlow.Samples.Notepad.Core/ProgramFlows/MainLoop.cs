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
            FlowEndToken end;
            end = await login.ExecuteRetryLoopAsync(context);

            if (end.HasCanceled)
            {
                return end;
            }

            do
            {
                await noteListView.ExecuteAsync(context);
                await Write("");

                end = await selectUserAction.ExecuteAsync(context);
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
