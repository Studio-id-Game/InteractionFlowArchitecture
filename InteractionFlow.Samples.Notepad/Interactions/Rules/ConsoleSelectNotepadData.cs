using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.SilentExternalPorts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions.Rules
{
    internal readonly struct ConsoleSelectNotepadData(
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation)
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

            foreach (var (index, item) in userData.OrderBy(e => e.NoteId).Index())
            {
                fileDict[$"{index + 1}. {item.UserKey.Name}/{item.NoteId}"] = item;
            }

            var detaKeySelect = new ConsoleSelectItem<NotepadDataKey>(consoleReaction, consoleCursorPositionAccess, consoleOperation, fileDict);

            return await detaKeySelect.GetSelectAsync(context);
        }
    }
}
