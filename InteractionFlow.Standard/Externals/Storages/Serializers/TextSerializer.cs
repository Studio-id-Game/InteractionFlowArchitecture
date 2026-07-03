using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System;
using System.IO;
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
            Result<string> text = await Serialize(inputValue, DefaultRefText(refData));
            if (!text)
                return text.Exception!;

            if (!refData)
                return refData.Exception!;

            var stream = refData.Value!;

            try
            {
                await using StreamWriter writer = new(stream);
                await writer.WriteAsync(text.Value);
                await writer.FlushAsync();

                return stream;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public override async Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue)
        {
            if (!inputData)
                return inputData.Exception!;

            var stream = inputData.Value!;

            try
            {
                using StreamReader reader = new(stream);
                string text = await reader.ReadToEndAsync();
                return await Deserialize(text, refValue);
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
