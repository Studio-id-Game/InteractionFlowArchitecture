using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Serializers
{
    public abstract class TextSerializer<TValue> : StreamSerializer<TValue>, ISerializerPort<string, TValue>
    {
        public abstract Task<Result<string>> Serialize(Result<TValue> inputValue, Result<string> refText);

        public abstract Task<Result<TValue>> Deserialize(Result<string> inputText, Result<TValue> refValue);

        public virtual Result<string> DefaultRefText(Result<Stream> refData)
        {
            return "";
        }

        public override async Task<Result<Stream>> Serialize(Result<TValue> inputValue, Result<Stream> refData)
        {
            return await Serialize(inputValue, DefaultRefText(refData))
                .ThenAsync(async text =>
                {
                    return refData.Then(stream => (text, stream).AsResult());
                })
                .ThenAsync(async value =>
                {
                    var (text, stream) = value;

                    try
                    {
                        await using StreamWriter writer = new(stream, Encoding.UTF8, 1024, true);
                        await writer.WriteAsync(text);
                        await writer.FlushAsync();
                        return stream.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                });
        }

        public override async Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue)
        {
            return await inputData.StartAsync()
                .ThenAsync(async stream =>
                {
                    try
                    {
                        using StreamReader reader = new(stream, Encoding.UTF8, true, 1024, true);
                        var text = await reader.ReadToEndAsync();
                        return text.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                })
                .ThenAsync(async text =>
                {
                    return await Deserialize(text, refValue);
                });
        }
    }
}
