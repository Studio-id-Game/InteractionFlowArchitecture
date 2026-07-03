using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.Externals.Storages.Serializers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages.Serializers
{

    public class UserSecureDataSerializer : StreamSerializer<UserSecureData>, IUserSecureDataSerializerPort
    {
        public override async Task<Result<UserSecureData>> Deserialize(Result<Stream> inputData, Result<UserSecureData> refValue)
        {
            if (!inputData)
                return inputData.Exception!;

            var data = refValue ? refValue.Value! : new();
            var stream = inputData.Value!;

            try
            {
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                data.UserSalt = memory.ToArray();
            }
            catch (Exception e)
            {
                return e;
            }

            return data;
        }

        public override async Task<Result<Stream>> Serialize(Result<UserSecureData> inputValue, Result<Stream> refData)
        {
            if (!inputValue)
                return inputValue.Exception!;
            if (!refData)
                return refData.Exception!;

            var data = inputValue.Value!;
            var stream = refData.Value!;

            try
            {
                await stream.WriteAsync(data.UserSalt);

                return stream;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
