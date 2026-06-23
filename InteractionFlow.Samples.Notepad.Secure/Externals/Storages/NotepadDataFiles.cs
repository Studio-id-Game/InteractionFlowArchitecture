using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.Silents;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
{
    internal class NotepadDataFiles(INotepadDataMemory memory, ISecureManager secureManager, INotepadUserSecureDataFiles notepadUserSecureDataFiles)
        : FileStorageModifiable<NotepadData, INotepadDataMemory>(memory), INotepadDataFiles
    {
        protected override FileInfo? GetFileInfo(IFlowContext context)
        {
            if (context.TryGet(out NotepadDataKey key))
            {
                return key.GetNoteFile();
            }

            return null;
        }

        protected override async Task<Result<NotepadData>> ReadFileAsync(IFlowContext context, FileInfo file)
        {
            try
            {
                var noteKey = NotepadDataKey.CreateFromNoteFile(file);
                if (noteKey.HasValue)
                {
                    using var reader = file.OpenRead();
                    using var ms = new MemoryStream();
                    await reader.CopyToAsync(ms);
                    var bytes = ms.ToArray();

                    var loadUserSecureDataResult = await notepadUserSecureDataFiles.LoadFromPersistentAsync(context);
                    if (!loadUserSecureDataResult &&
                        notepadUserSecureDataFiles.TryGetOrCreateDefault(context, out var loadUserSecureData) &&
                        loadUserSecureData != null)
                    {
                        loadUserSecureDataResult = loadUserSecureData;
                    }

                    if (!loadUserSecureDataResult)
                    {
                        throw new Exception("userSecureData notfound");
                    }

                    return Decrypt(loadUserSecureDataResult.Value!, bytes, noteKey.Value);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid FileInfo '{file.FullName}'");
                }
            }
            catch (Exception e)
            {
                return e;
            }

            NotepadData Decrypt(NotepadUserSecureData userSecureData, ReadOnlySpan<byte> bytes, NotepadDataKey noteKey)
            {
                if (!secureManager.TryGetLastUserKey(userSecureData, out var userKey))
                {
                    throw new Exception("LastUserKey notfound");
                }

                var data = new NotepadData(noteKey);

                using (userKey)
                {
                    var decryptResult = secureManager.DecryptNotepadData(data, userKey, bytes);
                    if (!decryptResult) throw decryptResult.Exception!;
                }

                return data;
            }
        }

        protected override async Task<Result> WriteFileAsync(IFlowContext context, FileInfo file, NotepadData value)
        {
            try
            {
                MakeDirectory(file.Directory);

                var tempFile = new FileInfo(Path.ChangeExtension(file.FullName, ".temp"));

                using (var writer = tempFile.Create())
                {
                    var bytes = Encrypt(context, value);
                    await writer.WriteAsync(bytes);
                }

                File.Move(tempFile.FullName, file.FullName, true);

                return true;
            }
            catch (Exception e)
            {
                return e;
            }

            ReadOnlyMemory<byte> Encrypt(IFlowContext context, NotepadData value)
            {
                if (!notepadUserSecureDataFiles.TryGet(context, out var userSecureData) || userSecureData == null)
                {
                    throw new Exception("userSecureData notfound");
                }

                if (!secureManager.TryGetLastUserKey(userSecureData, out var userKey))
                {
                    throw new Exception("LastUserKey notfound");
                }


                Span<byte> bytes = stackalloc byte[secureManager.GetCipherBytesSize(value)];

                using (userKey)
                {
                    var encryptResult = secureManager.EncryptNotepadData(value, userKey, bytes);
                    if (!encryptResult) throw encryptResult.Exception!;
                }

                return bytes.ToArray();
            }
        }

        protected override Result<NotepadData> ValidateNormalize(NotepadData value)
        {
            if (value.NoteKey.IsValid)
            {
                value.Title = value.Title.Trim(' ', '\n', '\t', '\r');
                return value;
            }
            else
            {
                return new InvalidOperationException("Invalid NotepadData");
            }
        }

        public void Clear()
        {
            CacheStorage.Clear();
        }
    }
}
