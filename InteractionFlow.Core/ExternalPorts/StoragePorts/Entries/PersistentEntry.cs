using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts.Entries
{
    /// <summary>
    /// 永続化 ID と値を関連付ける Entry です。
    /// </summary>
    /// <typeparam name="TPersistenceId">永続化先を識別する ID の型。</typeparam>
    /// <typeparam name="TValue">ラップする値の型。</typeparam>
    /// <param name="persistenceId">永続化先を識別する ID。</param>
    /// <param name="value">初期値。</param>
    public class PersistentEntry<TPersistenceId, TValue>(TPersistenceId persistenceId, TValue? value) : Entry<TValue>(value)
    {
        /// <summary>
        /// 永続化先を識別する ID を取得します。
        /// </summary>
        public TPersistenceId PersistenceId => persistenceId;

        /// <summary>
        /// 指定された Persistence ポートから値を読み込み、成功した値をこの Entry の現在値として設定します。
        /// </summary>
        /// <param name="persistencePort">読み込みに使用する Persistence ポート。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        public async Task<Result<TValue>> Load(IPersistencePort<TPersistenceId, TValue> persistencePort)
        {
            var oldValue = Value != null ? Value.AsResult() : new NullReferenceException(nameof(Value));

            return await persistencePort.Load(persistenceId, oldValue)
                .ThenAsync(value =>
                {
                    Load(value);
                    return value.AsResult().StartAsync();
                })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 読み込みに成功した値を、この Entry の現在値として設定します。
        /// </summary>
        /// <param name="value">設定する読み込み済みの値。</param>
        internal void Load(TValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Entry の永続化 ID に対応する保存データを削除し、削除に成功した場合は保持する値を default にします。
        /// </summary>
        /// <param name="persistencePort">削除に使用する Persistence ポート。</param>
        /// <returns>削除結果。</returns>
        public async Task<Result> DeleteAndReset(IPersistencePort<TPersistenceId, TValue> persistencePort)
        {
            return await persistencePort.Delete(PersistenceId)
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
