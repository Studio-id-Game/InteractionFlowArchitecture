using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts.Entries
{
    /// <summary>
    /// <see cref="PersistentEntry{TPersistenceId, TValue}"/> と Persistence ポートをつなぐ拡張メソッドを提供します。
    /// </summary>
    public static class PersistentEntryExtensions
    {
        /// <summary>
        /// 複数の Entry に対応する値をまとめて読み込み、非 <see langword="null"/> の結果で各 Entry を更新します。
        /// </summary>
        /// <typeparam name="TPersistenceId">永続化先を識別する ID の型。</typeparam>
        /// <typeparam name="TValue">読み込む値の型。</typeparam>
        /// <param name="entries">読み込み対象の Entry。配列の順序は読み込み結果との対応に使用されます。</param>
        /// <param name="persistencePort">複数の ID と値をまとめて扱う Persistence ポート。</param>
        /// <returns>
        /// 読み込まれた値の配列。<see langword="null"/> の要素は、対応する Entry が未更新であることを表します。
        /// 読み込みに失敗した場合、または読み込み結果の件数が Entry の件数と一致しない場合は失敗結果。
        /// </returns>
        /// <remarks>
        /// 各 Entry の現在値は <see langword="null"/> を許容した配列として
        /// <see cref="IPersistencePort{TPersistenceId, TValue}.Load(TPersistenceId, Result{TValue})"/> の
        /// <c>oldValue</c> に渡します。読み込み結果が <see langword="null"/> の要素は更新せず、
        /// 非 <see langword="null"/> の要素だけを対応する Entry に設定します。
        /// </remarks>
        public static async Task<Result<TValue?[]>> LoadAll<TPersistenceId, TValue>(
            this PersistentEntry<TPersistenceId, TValue>[] entries,
            IPersistencePort<TPersistenceId[], TValue?[]> persistencePort)
        {
            try
            {
                var keys = entries.Select(e => e.PersistenceId).ToArray();
                var values = entries.Select(e => e.Value).ToArray();

                return await persistencePort.Load(keys, values)
                    .ThenAsync(loaded =>
                    {
                        if (loaded.Length != entries.Length)
                        {
                            Result<TValue?[]> error = new InvalidOperationException(
                                "The number of loaded values must match the number of entries.");
                            return error.StartAsync();
                        }

                        for (int i = 0; i < entries.Length; i++)
                        {
                            var value = loaded[i];
                            if (value is not null)
                            {
                                var entry = entries[i];
                                entry.Load(value);
                            }
                        }

                        return loaded.AsResultAsync();
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>
        /// Entry の現在値を指定された Persistence ポートへ保存します。
        /// </summary>
        /// <typeparam name="TPersistenceId">永続化先を識別する ID の型。</typeparam>
        /// <typeparam name="TValue">保存する値の型。</typeparam>
        /// <param name="entry">保存対象の Entry。</param>
        /// <param name="persistencePort">保存に使用する Persistence ポート。</param>
        /// <returns>保存結果。</returns>
        public static async Task<Result> Save<TPersistenceId, TValue>(
            this PersistentEntry<TPersistenceId, TValue> entry,
            IPersistencePort<TPersistenceId, TValue> persistencePort)
        {
            if (entry.Value == null)
            {
                return new NullReferenceException(nameof(entry.Value));
            }
            else
            {
                return await persistencePort.Save(entry.PersistenceId, entry.Value).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Entry の永続化 ID に対応する保存データが存在するかを確認します。
        /// </summary>
        /// <typeparam name="TPersistenceId">永続化先を識別する ID の型。</typeparam>
        /// <typeparam name="TValue">保存されている値の型。</typeparam>
        /// <param name="entry">存在確認対象の Entry。</param>
        /// <param name="persistencePort">存在確認に使用する Persistence ポート。</param>
        /// <returns>存在する場合は成功結果。存在しない場合は失敗結果。</returns>
        public static Task<Result> Exists<TPersistenceId, TValue>(
            this PersistentEntry<TPersistenceId, TValue> entry,
            IPersistencePort<TPersistenceId, TValue> persistencePort)
        {
            return persistencePort.Exists(entry.PersistenceId);
        }
    }
}
