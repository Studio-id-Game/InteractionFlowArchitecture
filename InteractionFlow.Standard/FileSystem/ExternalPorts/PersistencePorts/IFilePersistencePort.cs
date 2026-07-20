using InteractionFlow.Core.ExternalPorts.StoragePorts.PersistencePorts;

namespace InteractionFlow.Standard.FileSystem.ExternalPorts.PersistencePorts
{
    /// <summary>
    /// ファイル単位で値を永続化する Persistence ポートを表します。
    /// </summary>
    /// <remarks>
    /// Core の <see cref="IPersistencePort{TPersistenceId, TValue}"/> に対して、
    /// ファイルパス、ファイル名、拡張子などのファイルシステム固有の変換契約を追加します。
    /// </remarks>
    /// <typeparam name="TFileId">ファイルを識別する ID の型。</typeparam>
    /// <typeparam name="TValue">保存または読み込みする値の型。</typeparam>
    public interface IFilePersistencePort<TFileId, TValue> : IPersistencePort<TFileId, TValue>
    {
        /// <summary>
        /// 保存ファイルの拡張子を取得します。
        /// </summary>
        string Extention { get; }

        /// <summary>
        /// 保存先のルートパスを取得します。
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// ファイル名から ID を取得します。
        /// </summary>
        /// <param name="fileName">拡張子を含まないファイル名。</param>
        /// <returns>ファイル ID。</returns>
        TFileId GetFileId(string fileName);

        /// <summary>
        /// ファイルパスから ID を取得します。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <returns>ファイル ID。</returns>
        TFileId GetFileIdFromPath(string filePath);

        /// <summary>
        /// ID からファイル名を取得します。
        /// </summary>
        /// <param name="id">ファイル ID。</param>
        /// <returns>拡張子を含まないファイル名。</returns>
        string GetFileName(TFileId id);

        /// <summary>
        /// ID から保存ファイルのパスを取得します。
        /// </summary>
        /// <param name="id">ファイル ID。</param>
        /// <returns>保存ファイルのパス。</returns>
        string GetFilePath(TFileId id);
    }
}
