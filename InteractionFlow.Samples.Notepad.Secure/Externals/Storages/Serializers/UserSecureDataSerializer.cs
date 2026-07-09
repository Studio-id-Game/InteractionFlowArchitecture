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
            return await inputData.StartAsync()
                .ThenAsync(stream =>
                {
                    return Task.FromResult(refValue
                        .ThenError(e => new UserSecureData())
                        .Then(data => (stream, data).AsResult()));
                })
                .ThenAsync(async value =>
                {
                    var (stream, data) = value;

                    try
                    {
                        await using var memory = new MemoryStream();
                        await stream.CopyToAsync(memory).ConfigureAwait(false);
                        data.UserSalt = memory.ToArray();
                        return data.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                })
                .ConfigureAwait(false);
        }

        public override async Task<Result<Stream>> Serialize(Result<UserSecureData> inputValue, Result<Stream> refData)
        {
            return await inputValue.StartAsync()
                .ThenAsync(data =>
                {
                    return Task.FromResult(refData
                        .Then(stream => (data, stream).AsResult()));
                })
                .ThenAsync(async e =>
                {
                    var (data, stream) = e;

                    await stream.WriteAsync(data.UserSalt).ConfigureAwait(false);

                    return stream.AsResult();
                })
                .ConfigureAwait(false);
        }
    }
}
