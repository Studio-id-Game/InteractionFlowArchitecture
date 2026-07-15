using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts
{
    /// <summary>
    /// 値と保存用データ形式を相互変換する Serializer ポートを表します。
    /// </summary>
    /// <typeparam name="TData">保存や転送に使用するデータ形式。</typeparam>
    /// <typeparam name="TValue">変換対象の値の型。</typeparam>
    public interface ISerializerPort<TData, TValue> : IFlowSubNode
    {
        /// <summary>
        /// 値を保存用データ形式へ変換します。
        /// </summary>
        /// <param name="inputValue">変換する値。</param>
        /// <param name="refData">変換時に参照または再利用するデータ。</param>
        /// <returns>変換されたデータ。失敗時は失敗結果。</returns>
        Task<Result<TData>> Serialize(Result<TValue> inputValue, Result<TData> refData);

        /// <summary>
        /// 保存用データ形式から値へ変換します。
        /// </summary>
        /// <param name="inputData">変換するデータ。</param>
        /// <param name="refValue">変換時に参照または再利用する値。</param>
        /// <returns>変換された値。失敗時は失敗結果。</returns>
        Task<Result<TValue>> Deserialize(Result<TData> inputData, Result<TValue> refValue);
    }
}
