using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts
{
    /// <summary>
    /// ID で識別される値を保存・読み込みする Persistence ポートを表します。
    /// </summary>
    /// <typeparam name="TPersistenceId">永続化対象を識別する ID の型。</typeparam>
    /// <typeparam name="TValue">保存または読み込みする値の型。</typeparam>
    public interface IPersistencePort<TPersistenceId, TValue> : IFlowSubNode
    {
        /// <summary>
        /// 指定された ID に値を保存します。
        /// </summary>
        /// <param name="id">保存先を識別する ID。</param>
        /// <param name="value">保存する値。</param>
        /// <returns>保存結果。</returns>
        Task<Result> Save(TPersistenceId id, Result<TValue> value);

        /// <summary>
        /// 指定された ID の値を読み込みます。
        /// </summary>
        /// <param name="id">読み込み対象を識別する ID。</param>
        /// <param name="oldValue">読み込み時に参照または再利用する既存値。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        Task<Result<TValue>> Load(TPersistenceId id, Result<TValue> oldValue);

        /// <summary>
        /// 指定された ID の保存データを削除します。
        /// </summary>
        /// <param name="id">削除対象を識別する ID。</param>
        /// <returns>削除結果。</returns>
        Task<Result> Delete(TPersistenceId id);

        /// <summary>
        /// 指定された ID の保存データが存在するかを確認します。
        /// </summary>
        /// <param name="id">存在確認する ID。</param>
        /// <returns>存在する場合は成功結果。存在しない場合は失敗結果。</returns>
        Task<Result> Exist(TPersistenceId id);

        /// <summary>
        /// 保存されているすべての ID を取得します。
        /// </summary>
        /// <returns>保存されている ID の配列。失敗時は失敗結果。</returns>
        Task<Result<TPersistenceId[]>> GetAllId();
    }
}
