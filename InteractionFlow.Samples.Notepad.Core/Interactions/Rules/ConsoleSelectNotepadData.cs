using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions.Rules
{
    internal readonly struct ConsoleSelectNotepadData(
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadDataStoragePort notepadDataFiles,
        INotepadDataPersistencePort notepadDataPersistence)
    {
        public async Task<KeyValuePair<string, NotepadDataKey>> GetSelectAsync(IFlowContext context, NotepadUserData userData, bool includeErrorItem)
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

            foreach (var (index, item) in userData.OrderBy(e => e.NoteId).Select((item, index) => (index, item)))
            {
                dataKey.Value = item;

                var _notepadDataPersistence = notepadDataPersistence;
                var _notepadDataFiles = notepadDataFiles;

                var result = await notepadDataFiles.GetOrCreate(item).StartAsync()
                    .ThenAsync(async notepadEntity => await notepadEntity.Load(_notepadDataPersistence))
                    .ThenAsync(notepadData =>
                    {
                        var title = notepadData.Title;

                        _notepadDataFiles.RemoveAndDispose(item);

                        fileDict[$"{index + 1}. {title} ({item.UserKey.Name}/{item.NoteId})"] = item;

                        return Task.FromResult(Result.Success);
                    })
                    .ThenErrorAsync(e =>
                    {
                        if (includeErrorItem)
                        {
                            fileDict[$"{index + 1}. Error ({item.UserKey.Name}/{item.NoteId})"] = item;
                            return Task.FromResult(Result.Success);
                        }
                        else
                        {
                            return Task.FromResult<Result>(e);
                        }
                    });

                //result.ThrowIfError();
            }

            var detaKeySelect = new ConsoleSelectItem<NotepadDataKey>(consoleReaction, consoleCursorPositionAccess, consoleOperation, fileDict);

            return await detaKeySelect.GetSelectAsync(context);
        }
    }
}
