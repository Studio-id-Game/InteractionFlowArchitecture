using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
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
        /// 現在の値を指定された Persistence ポートへ保存します。
        /// </summary>
        /// <param name="fileController">保存に使用する Persistence ポート。</param>
        /// <returns>保存結果。</returns>
        public async Task<Result> Save(IPersistencePort<TPersistentId, TValue> fileController)
        {
            if (Value == null)
            {
                return new NullReferenceException(nameof(Value));
            }
            else
            {
                return await fileController.Save(fileID, Value);
            }
        }

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
                });
        }
    }
}
