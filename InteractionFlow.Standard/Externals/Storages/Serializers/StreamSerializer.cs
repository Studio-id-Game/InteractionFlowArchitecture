using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Serializers
{
    /// <summary>
    /// <see cref="Stream"/> と値を相互変換する Serializer の基底クラスです。
    /// </summary>
    /// <typeparam name="TValue">変換対象の値の型。</typeparam>
    public abstract class StreamSerializer<TValue> : ISerializerPort<Stream, TValue>
    {
        /// <summary>
        /// この Serializer が依存する補助ノードを取得します。
        /// </summary>
        public virtual ReadOnlySpan<IDependencyNode> Dependency => [];

        /// <summary>
        /// ストリームから値へ変換します。
        /// </summary>
        /// <param name="inputData">変換するストリーム。</param>
        /// <param name="refValue">変換時に参照または再利用する値。</param>
        /// <returns>変換された値。失敗時は失敗結果。</returns>
        public abstract Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue);

        /// <summary>
        /// 値をストリームへ変換します。
        /// </summary>
        /// <param name="inputValue">変換する値。</param>
        /// <param name="refData">書き込み先として参照または再利用するストリーム。</param>
        /// <returns>書き込み後のストリーム。失敗時は失敗結果。</returns>
        public abstract Task<Result<Stream>> Serialize(Result<TValue> inputValue, Result<Stream> refData);
    }
}
