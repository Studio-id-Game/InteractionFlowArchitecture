using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.SystemFlows
{
    public sealed class MainLoop(Login login, NoteListView list, SelectUserAction select,
        NoteCreate create, NoteEdit edit, NoteDelete delete)
        : SystemFlow<NotepadContext>(login, list, select, create, edit, delete)
    {
        protected override async Task<FlowEndToken> ExecuteCoreAsync(NotepadContext context)
        {
            var end = await login.ExecuteAsync(context);
            while (!end.HasException && !context.ExitRequested)
            {
                if (context.User == null) { end = await login.ExecuteAsync(context); continue; }
                end = await select.ExecuteAsync(context);
                if (end.HasException || context.ExitRequested) break;
                end = context.Action switch
                {
                    NotepadAction.Create => await create.ExecuteAsync(context),
                    NotepadAction.Edit => await edit.ExecuteAsync(context),
                    NotepadAction.Delete => await delete.ExecuteAsync(context),
                    NotepadAction.List => await list.ExecuteAsync(context),
                    NotepadAction.Login => await login.ExecuteAsync(context),
                    _ => end
                };
            }
            return end;
        }
    }
}
