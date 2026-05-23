using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentPorts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions.Rules
{
    internal readonly struct ConsoleSelectNotepadData(
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadDataFiles notepadDataFiles)
    {
        public async Task<KeyValuePair<string, NotepadDataKey>> GetSelectAsync(IFlowContext context, NotepadUserData userData)
        {
            if (!userData.Any())
            {
                await consoleReaction.Write(context, new ConsoleOutput("> Not found NotepadData."));
            }

            var fileDict = new Dictionary<string, NotepadDataKey>()
            {
                ["0. Cancel"] = NotepadDataKey.Empty
            };

            var loadContext = new FlowContextGroup(context)
                .Add(NotepadDataKey.Empty, out var dataKey);

            foreach (var (index, item) in userData.OrderBy(e => e.NoteId).Index())
            {
                dataKey.Value = item;
                var titleResult = await notepadDataFiles.LoadFromPersistentAsync(loadContext);
                var title = titleResult ? titleResult.Value!.Title : "Title Error";

                fileDict[$"{index + 1}. {title} ({item.UserKey.Name}/{item.NoteId})"] = item;
            }

            var detaKeySelect = new ConsoleSelectItem<NotepadDataKey>(consoleReaction, consoleCursorPositionAccess, consoleOperation, fileDict);

            return await detaKeySelect.GetSelectAsync(context);
        }
    }
}
