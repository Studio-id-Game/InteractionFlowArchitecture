using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.Entities.Rules;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Storages;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Storages
{
    internal class NotepadUserDataDirectories(INotepadUserDataMemory memory, INotepadDataFiles notepadDataFiles)
        : DirectoryStorageModifiable<NotepadUserData, INotepadUserDataMemory>(memory), INotepadUserDataFiles
    {
        public bool Exist(NotepadDataKey dataKey)
        {
            if (!dataKey.IsValid) return false;
            var file = dataKey.GetNoteFile();
            if (file == null) return false;

            return file.Exists;
        }

        protected override DirectoryInfo? GetDirectoryInfo(IFlowContext context)
        {
            if (context.TryGet(out NotepadUserKey notepadUserKey))
            {
                return notepadUserKey.GetUserDirectory();
            }

            return null;
        }

        protected override async Task<Result<NotepadUserData>> ReadDirectoryAsync(IFlowContext context, DirectoryInfo directory)
        {
            try
            {
                if (!directory.Exists)
                {
                    return new NotepadUserData();
                }

                var userKey = NotepadUserKey.CreateFromUserDirectory(directory);
                if (userKey.HasValue)
                {
                    var value = new NotepadUserData();

                    foreach (var file in directory.EnumerateFiles())
                    {
                        if (!file.Name.EndsWith(NotepadRule.Extention))
                            continue;

                        var noteId = Path.GetFileNameWithoutExtension(file.Name);
                        value.Add(new NotepadDataKey(userKey.Value.Id, noteId));
                    }

                    return value;
                }
                else
                {
                    return new InvalidOperationException("Invalid DirectoryInfo");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override async Task<Result> WriteDirectoryAsync(IFlowContext context, DirectoryInfo directory, NotepadUserData value)
        {
            try
            {
                if (!directory.Exists)
                {
                    MakeDirectory(directory);
                }
                else
                {
                    foreach (var file in directory.EnumerateFiles())
                    {
                        if (!file.Name.EndsWith(NotepadRule.Extention))
                            continue;

                        if (value.Any(y => y.NoteId == file.Name))
                        {
                            file.Delete();
                        }
                    }
                }

                var fileContext = new FlowContextGroup(context)
                    .Add(NotepadDataKey.Empty, out var notepadDataKey);

                foreach (var key in value)
                {
                    notepadDataKey.Value = key;
                    await notepadDataFiles.SaveToPersistentAsync(fileContext);
                }

                return true;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected override Result<NotepadUserData> ValidateNormalize(NotepadUserData value)
        {
            if (value.UserId.IsValid)
            {
                return value;
            }
            else
            {
                return new InvalidOperationException("Invalid NotepadUserData");
            }
        }
    }
}
