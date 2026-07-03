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
        protected SecretBuffer GetUserKey()
        {
            var lastUser = currentUserStorage.LastUser;

            if (!lastUser.IsValid)
            {
                throw new InvalidOperationException("Unknown user key");
            }

            var dataResult = currentUserStorage.GetOrCreate(lastUser);

            if (!dataResult)
            {
                throw dataResult.Exception!;
            }

            var data = dataResult.Value!;

            return new(data.Value!.LastUserKey!.ToArray());
        }

        public override async Task<Result<NotepadData>> Deserialize(Result<Stream> inputData, Result<NotepadData> refValue)
        {
            if (!inputData)
                return inputData.Exception!;
            if (!refValue)
                return refValue.Exception!;

            var notepad = refValue.Value!;
            var stream = inputData.Value!;
            byte[] bytes;

            try
            {
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                bytes = memory.ToArray();
            }
            catch (Exception e)
            {
                return e;
            }

            var decryptRsult = secureManager.DecryptNotepadData(notepad, GetUserKey(), bytes);

            if (!decryptRsult)
            {
                return decryptRsult.Exception!;
            }

            return notepad;
        }

        public override async Task<Result<Stream>> Serialize(Result<NotepadData> inputValue, Result<Stream> refData)
        {
            if (!inputValue)
                return inputValue.Exception!;
            if (!refData)
                return refData.Exception!;

            var notepad = inputValue.Value!;
            var stream = refData.Value!;

            var size = secureManager.GetCipherBytesSize(notepad);
            var cipherBytes = new byte[size];

            var encryptResult = secureManager.EncryptNotepadData(notepad, GetUserKey(), cipherBytes);

            if (!encryptResult)
            {
                return encryptResult.Exception!;
            }

            try
            {
                await stream.WriteAsync(cipherBytes);

                return stream;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
