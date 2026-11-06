using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SerializationSystem.Externals
{
    /// <summary>
    /// 文字列形式の変換を基準に、ストリームとの相互変換も提供する Serializer 基底クラスです。
    /// </summary>
    /// <typeparam name="TValue">変換対象の値の型。</typeparam>
    /// <param name="dependency">この Serializer が依存する補助ノード。</param>
    public abstract class TextSerializer<TValue>(params IDependencyNode[] dependency)
        : StreamSerializer<TValue>(dependency), ISerializerPort<string, TValue>
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>
        /// 文字列とストリームの変換に使用する文字コード。既定は BOM なし・不正データで例外となる UTF-8 です。
        /// </summary>
        public virtual Encoding Encoding => DefaultEncoding;

        /// <summary>BOM に基づく入力文字コードの検出を有効にするかどうか。既定は有効です。</summary>
        public virtual bool DetectEncodingFromByteOrderMarks => true;

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
        /// Encoding で指定された文字コードを用いて、値を文字列へ変換しストリームへ書き込みます。
        /// </summary>
        /// <param name="inputValue">変換する値。</param>
        /// <param name="refData">書き込み先として使用するストリーム。</param>
        /// <returns>書き込み後のストリーム。失敗時は失敗結果。</returns>
        /// <remarks>
        /// 現在位置から書き込み、巻き戻し・切り詰めはせず、処理後もストリームを開いたままにします。
        /// 文字列版には空文字を参照値として渡します。BOM 出力は Encoding のプリアンブルに従います。
        /// 書き込み先の失敗は文字列変換前に返し、Stream 操作の例外だけを失敗 Result に変換します。
        /// 文字列版の変換が送出する例外は捕捉しません。
        /// </remarks>
        public override async Task<Result<Stream>> Serialize(Result<TValue> inputValue, Result<Stream> refData)
        {
            return await refData.StartAsync()
                .ThenAsync(async stream =>
                {
                    return await Serialize(inputValue, "".AsResult())
                        .MapAsync(text => (stream, text))
                        .ConfigureAwait(false);
                })
                .ThenAsync(async value =>
                {
                    var (stream, text) = value;

                    try
                    {
                        await using StreamWriter writer = new(
                            stream,
                            Encoding,
                            bufferSize: 1024,
                            leaveOpen: true
                        );

                        await writer.WriteAsync(text).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                        return stream.AsResult();
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Encoding で指定された文字コードを用いて、ストリームから文字列を読み取り値へ変換します。
        /// </summary>
        /// <param name="inputData">読み取り元のストリーム。</param>
        /// <param name="refValue">変換時に参照または再利用する値。</param>
        /// <returns>変換された値。失敗時は失敗結果。</returns>
        /// <remarks>
        /// 現在位置から末尾まで読み、位置を復元せず、処理後もストリームを開いたままにします。
        /// refValue の失敗も文字列版へ渡します。Stream 操作の例外だけを失敗 Result に変換します。
        /// 文字列版の変換が送出する例外は捕捉しません。
        /// </remarks>
        public override async Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue)
        {
            return await inputData.StartAsync()
                .ThenAsync(async stream =>
                {
                    try
                    {
                        using StreamReader reader = new(
                            stream,
                            Encoding,
                            detectEncodingFromByteOrderMarks: DetectEncodingFromByteOrderMarks,
                            bufferSize: 1024,
                            leaveOpen: true
                        );

                        var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                        return text.AsResult();
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                })
                .ThenAsync(text => Deserialize(text.AsResult(), refValue))
                .ConfigureAwait(false);
        }
    }
}
