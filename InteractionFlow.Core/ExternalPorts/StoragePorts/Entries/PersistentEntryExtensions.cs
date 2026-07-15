using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts.Entries
{
    /// <summary>
    /// <see cref="PersistentEntry{TPersistentId, TValue}"/> と Persistence ポートをつなぐ拡張メソッドを提供します。
    /// </summary>
    public static class PersistentEntryExtensions
    {
        /// <summary>
        /// Entry の現在値を指定された Persistence ポートへ保存します。
        /// </summary>
        /// <typeparam name="TPersistentId">永続化先を識別する ID の型。</typeparam>
        /// <typeparam name="TValue">保存する値の型。</typeparam>
        /// <param name="entry">保存対象の Entry。</param>
        /// <param name="fileController">保存に使用する Persistence ポート。</param>
        /// <returns>保存結果。</returns>
        public static async Task<Result> Save<TPersistentId, TValue>(
            this PersistentEntry<TPersistentId, TValue> entry,
            IPersistencePort<TPersistentId, TValue> fileController)
        {
            if (entry.Value == null)
            {
                return new NullReferenceException(nameof(entry.Value));
            }
            else
            {
                return await fileController.Save(entry.FileID, entry.Value).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Entry の永続化 ID に対応する保存データが存在するかを確認します。
        /// </summary>
        /// <typeparam name="TPersistentId">永続化先を識別する ID の型。</typeparam>
        /// <typeparam name="TValue">保存されている値の型。</typeparam>
        /// <param name="entry">存在確認対象の Entry。</param>
        /// <param name="fileController">存在確認に使用する Persistence ポート。</param>
        /// <returns>存在する場合は成功結果。存在しない場合は失敗結果。</returns>
        public static Task<Result> Exist<TPersistentId, TValue>(
            this PersistentEntry<TPersistentId, TValue> entry,
            IPersistencePort<TPersistentId, TValue> fileController)
        {
            return fileController.Exist(entry.FileID);
        }
    }
}
