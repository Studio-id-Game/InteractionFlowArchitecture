using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Serialization.Externals.Serializers
{
    /// <summary>
    /// 文字列形式の変換を基準に、ストリームとの相互変換も提供する Serializer 基底クラスです。
    /// </summary>
    /// <typeparam name="TValue">変換対象の値の型。</typeparam>
    /// <param name="dependency">この Serializer が依存する補助ノード。</param>
    public abstract class TextSerializer<TValue>(params IDependencyNode[] dependency)
        : StreamSerializer<TValue>(dependency), ISerializerPort<string, TValue>
    {
        /// <summary>
        /// 値を文字列へ変換します。
        /// </summary>
        /// <param name="inputValue">変換する値。</param>
        /// <param name="refText">変換時に参照または再利用する文字列。</param>
        /// <returns>変換された文字列。失敗時は失敗結果。</returns>
        public abstract Task<Result<string>> Serialize(Result<TValue> inputValue, Result<string> refText);

        /// <summary>
        /// 文字列から値へ変換します。
        /// </summary>
        /// <param name="inputText">変換する文字列。</param>
        /// <param name="refValue">変換時に参照または再利用する値。</param>
        /// <returns>変換された値。失敗時は失敗結果。</returns>
        public abstract Task<Result<TValue>> Deserialize(Result<string> inputText, Result<TValue> refValue);

        /// <summary>
        /// ストリーム変換時に使用する既定の参照文字列を取得します。
        /// </summary>
        /// <param name="refData">参照元のストリーム結果。</param>
        /// <returns>既定の参照文字列。</returns>
        public virtual Result<string> DefaultRefText(Result<Stream> refData)
        {
            return "";
        }

        /// <summary>
        /// 値を文字列へ変換し、UTF-8 でストリームへ書き込みます。
        /// </summary>
        /// <param name="inputValue">変換する値。</param>
        /// <param name="refData">書き込み先として使用するストリーム。</param>
        /// <returns>書き込み後のストリーム。失敗時は失敗結果。</returns>
        public override async Task<Result<Stream>> Serialize(Result<TValue> inputValue, Result<Stream> refData)
        {
            return await Serialize(inputValue, DefaultRefText(refData))
                .ThenAsync(text =>
                {
                    return Task.FromResult(refData.Then(stream => (text, stream).AsResult()));
                })
                .ThenAsync(async value =>
                {
                    var (text, stream) = value;

                    try
                    {
                        await using StreamWriter writer = new(stream, Encoding.UTF8, 1024, true);
                        await writer.WriteAsync(text).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                        return stream.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// ストリームから UTF-8 文字列を読み取り、値へ変換します。
        /// </summary>
        /// <param name="inputData">読み取り元のストリーム。</param>
        /// <param name="refValue">変換時に参照または再利用する値。</param>
        /// <returns>変換された値。失敗時は失敗結果。</returns>
        public override async Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue)
        {
            return await inputData.StartAsync()
                .ThenAsync(async stream =>
                {
                    try
                    {
                        using StreamReader reader = new(stream, Encoding.UTF8, true, 1024, true);
                        var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                        return text.AsResult();
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                })
                .ThenAsync(async text =>
                {
                    return await Deserialize(text, refValue).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }
    }
}
