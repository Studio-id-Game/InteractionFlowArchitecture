using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;

namespace InteractionFlow.Standard.FileSystem.ExternalPorts.PersistencePorts
{
    /// <summary>
    /// ディレクトリ単位で値を永続化する Persistence ポートを表します。
    /// </summary>
    /// <remarks>
    /// Core の <see cref="IPersistencePort{TPersistenceId, TValue}"/> に対して、
    /// ディレクトリパスやディレクトリ名などのファイルシステム固有の変換契約を追加します。
    /// </remarks>
    /// <typeparam name="TDirectoryId">ディレクトリを識別する ID の型。</typeparam>
    /// <typeparam name="TValue">保存または読み込みする値の型。</typeparam>
    public interface IDirectoryPersistencePort<TDirectoryId, TValue> : IPersistencePort<TDirectoryId, TValue>
    {
        /// <summary>
        /// 保存先のルートパスを取得します。
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// ディレクトリ名から ID を取得します。
        /// </summary>
        /// <param name="directoryName">ディレクトリ名。</param>
        /// <returns>ディレクトリ ID。</returns>
        TDirectoryId GetDirectoryId(string directoryName);

        /// <summary>
        /// ID からディレクトリ名を取得します。
        /// </summary>
        /// <param name="id">ディレクトリ ID。</param>
        /// <returns>ディレクトリ名。</returns>
        string GetDirectoryName(TDirectoryId id);

        /// <summary>
        /// ID から保存ディレクトリのパスを取得します。
        /// </summary>
        /// <param name="id">ディレクトリ ID。</param>
        /// <returns>保存ディレクトリのパス。</returns>
        string GetDirectoryPath(TDirectoryId id);
    }
}
