using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using InteractionFlow.Standard.Externals.Storages.Serializers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages.Serializers
{
    internal class NotepadDataSecureSerializer(ISecureManagerPort secureManager, ICurrentUserStoragePort currentUserStorage) : StreamSerializer<NotepadData>, INotepadDataSerializerPort
    {
        private SecretBuffer GetUserKey()
        {
            var lastUser = currentUserStorage.LastUser;

            if (!lastUser.IsValid)
            {
                throw new InvalidOperationException("Unknown user key");
            }

            var dataResult = currentUserStorage.GetOrCreate(lastUser);

            if (dataResult.Try(out var entry, out var e))
            {
                return new(entry.Value!.LastUserKey!.ToArray());
            }
            else
            {
                throw e;
            }
        }

        public override async Task<Result<NotepadData>> Deserialize(Result<Stream> inputData, Result<NotepadData> refValue)
        {
            return await inputData.StartAsync()
                .ThenAsync(async stream =>
                {
                    return refValue.Then(notepad => (stream, notepad).AsResult());
                })
                .ThenAsync(async value =>
                {
                    var (stream, notepad) = value;
                    try
                    {
                        await using var memory = new MemoryStream();
                        await stream.CopyToAsync(memory);
                        var bytes = memory.ToArray();
                        return (notepad, bytes).AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                })
                .ThenAsync(async value =>
                {
                    var (notepad, bytes) = value;

                    return secureManager.DecryptNotepadData(notepad, GetUserKey(), bytes)
                        .Then(() => notepad.AsResult());
                });
        }

        public override async Task<Result<Stream>> Serialize(Result<NotepadData> inputValue, Result<Stream> refData)
        {
            return await refData.StartAsync()
                .ThenAsync(async stream =>
                {
                    return inputValue.Then(notepad => (stream, notepad).AsResult());
                })
                .ThenAsync(async value =>
                {
                    var (stream, notepad) = value;

                    var size = secureManager.GetCipherBytesSize(notepad);
                    var cipherBytes = new byte[size];
                    return secureManager.EncryptNotepadData(notepad, GetUserKey(), cipherBytes)
                        .Then(() => (stream, cipherBytes).AsResult());
                })
                .ThenAsync(async value =>
                {
                    var (stream, cipherBytes) = value;
                    try
                    {
                        await stream.WriteAsync(cipherBytes);

                        return stream.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                });
        }
    }
}
