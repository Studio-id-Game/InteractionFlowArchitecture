using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts.Entries
{
    /// <summary>
    /// 永続化 ID と値を関連付ける Entry です。
    /// </summary>
    /// <typeparam name="TPersistentId">永続化先を識別する ID の型。</typeparam>
    /// <typeparam name="TValue">ラップする値の型。</typeparam>
    /// <param name="fileID">永続化先を識別する ID。</param>
    /// <param name="value">初期値。</param>
    public class PersistentEntry<TPersistentId, TValue>(TPersistentId fileID, TValue? value) : Entry<TValue>(value)
    {
        /// <summary>
        /// 永続化先を識別する ID を取得します。
        /// </summary>
        public TPersistentId FileID => fileID;

        /// <summary>
        /// 指定された Persistence ポートから値を読み込みます。
        /// </summary>
        /// <param name="fileController">読み込みに使用する Persistence ポート。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        public async Task<Result<TValue>> Load(IPersistencePort<TPersistentId, TValue> fileController)
        {
            var oldValue = Value != null ? Value.AsResult() : new NullReferenceException(nameof(Value));

            return await fileController.Load(fileID, oldValue)
                .ThenAsync(value =>
                {
                    Value = value;
                    return value.AsResult().StartAsync();
                })
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Entry の永続化 ID に対応する保存データを削除し、削除に成功した場合は保持する値を default にします。
        /// </summary>
        /// <param name="fileController">削除に使用する Persistence ポート。</param>
        /// <returns>削除結果。</returns>
        public async Task<Result> DeleteAndReset(IPersistencePort<TPersistentId, TValue> fileController)
        {
            return await fileController.Delete(FileID)
                .ResolveAsync(
                onSuccess: () =>
                {
                    Value = default;
                    return Result.Success.StartAsync();
                },
                onFailure: e => e.AsResult().StartAsync()
                );
        }
    }
}
