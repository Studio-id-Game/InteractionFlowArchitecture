using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
{
    internal class NotepadUserSecureDataFiles(INotepadUserSecureDataMemory memory) :
        FileStorageModifiable<NotepadUserSecureData, INotepadUserSecureDataMemory>(memory), INotepadUserSecureDataFiles
    {
        protected override FileInfo? GetFileInfo(IFlowContext context)
        {
            if (!context.TryGet(out NotepadUserKey notepadUserKey))
            {
                return null;
            }

            var dir = notepadUserKey.GetUserDirectory();

            if (dir == null)
            {
                return null;
            }

            return new(Path.Combine(dir.FullName, NotepadUserSecureData.FileName));
        }

        protected override async Task<Result<NotepadUserSecureData>> ReadFileAsync(IFlowContext context, FileInfo file)
        {
            var directory = file.Directory!;

            try
            {
                if (!directory.Exists)
                {
                    MakeDirectory(directory);
                }

                CacheStorage.TryGetOrCreateDefault(context, out var result);

                if (file.Exists)
                {
                    byte[] salt = await File.ReadAllBytesAsync(file.FullName);
                    result!.UserSalt = salt.Length == 0 ? null : salt;
                }

                return result!;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected override async Task<Result> WriteFileAsync(IFlowContext context, FileInfo file, NotepadUserSecureData value)
        {
            var directory = file.Directory!;

            try
            {
                if (!directory.Exists)
                {
                    MakeDirectory(directory);
                }

                if (!file.Exists)
                {
                    await File.WriteAllBytesAsync(file.FullName, value.UserSalt ?? []);
                }

                return Result.Success;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
