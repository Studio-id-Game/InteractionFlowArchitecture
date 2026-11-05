using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.FileSystem.Externals
{
    /// <summary>ID と相対パスを対応付け、単一ファイルへ値を永続化する実装基底です。</summary>
    /// <remarks>
    /// 変換・書き込み・ストリーム破棄に成功してから保存先を置き換えます。
    /// ストリームはこの実装が所有します。Serializer は渡されたストリームへ書き込み、所有権を取得しません。
    /// Serializer の戻り値は成否の確認に使い、別のストリームへの差し替えには使用しません。
    /// リンクを解決した隔離、停電後の永続性、複数プロセス間の更新競合解決は保証しません。
    /// </remarks>
    public abstract class FilePersistence<TFileId, TValue> : IPersistencePort<TFileId, TValue>
    {
        private const string TemporaryFilePrefix = ".interactionflow-save-";
        private readonly ISerializerPort<Stream, TValue> serializer;
        private readonly IDependencyNode[] dependencies;

        /// <summary>Serializer、固定の接尾辞、補助ノードを指定します。ファイルは作成しません。</summary>
        /// <param name="serializer">借りたストリームと値を相互変換する実装。</param>
        /// <param name="extension">空文字、または .txt / .data.json のような接尾辞。大文字小文字を区別します。</param>
        /// <param name="dependency">依存グラフへ追加する補助ノード。</param>
        protected FilePersistence(
            ISerializerPort<Stream, TValue> serializer, string extension = ".bin",
            params IDependencyNode[] dependency)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            ValidateExtension(extension);
            Extension = extension;
            dependencies = [serializer, .. dependency];
        }

        /// <summary>依存する Serializer と補助ノード。</summary>
        public virtual ReadOnlyMemory<IDependencyNode> Dependency => dependencies;

        /// <summary>一まとまりとして付加・除去する接尾辞。</summary>
        public string Extension { get; }

        /// <summary>現在のルートを返します。相対パスも許可し、各操作で取得直後に一度だけ絶対パス化します。</summary>
        public virtual string GetCurrentRootPath() => Environment.CurrentDirectory;

        /// <summary>ID を、設定した接尾辞を含まないルート相対パスへ変換します。</summary>
        protected abstract string GetRelativePathWithoutExtension(TFileId id);

        /// <summary>設定した接尾辞を除いたルート相対パスを ID へ変換します。</summary>
        protected abstract TFileId GetIdFromRelativePathWithoutExtension(string relativePath);

        /// <summary>確定済みの絶対ルートを使用して、ID のファイルパスを解決・検証します。</summary>
        protected string GetFilePath(string rootPath, TFileId id)
        {
            var relativePath = GetRelativePathWithoutExtension(id);
            if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
                throw new InvalidOperationException("The ID must identify a nonempty relative file path.");

            var path = FileSystemPath.Resolve(rootPath, relativePath + Extension);
            if (IsTemporaryFile(path))
                throw new InvalidOperationException($"File names starting with {TemporaryFilePrefix} are reserved.");

            return path;
        }

        /// <summary>一時ファイルへの出力成功後に保存先を置き換えます。失敗入力では I/O を行いません。</summary>
        public async Task<Result> Save(TFileId id, Result<TValue> value)
        {
            if (!value.Try(out _, out var inputFailure)) return inputFailure;

            string? temporaryPath = null;
            var ownsTemporaryFile = false;
            Exception failure;
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                var path = GetFilePath(rootPath, id);
                var parentPath = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(parentPath);
                temporaryPath = CreateTemporaryPath(parentPath);

                await using (var stream = new FileStream(
                    temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true))
                {
                    ownsTemporaryFile = true;
                    var serialized = await serializer.Serialize(value, stream).ConfigureAwait(false);
                    serialized.ThrowIfError();
                    await stream.FlushAsync().ConfigureAwait(false);
                }

                // 同じ親の中で置き換える。先に保存先を削除するフォールバックは行わない。
                Commit(temporaryPath, path);
                return Result.Success;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (ownsTemporaryFile)
            {
                try
                {
                    File.Delete(temporaryPath!);
                }
                catch (Exception cleanupFailure)
                {
                    failure = new AggregateException(failure, cleanupFailure);
                }
            }

            return failure;
        }

        /// <summary>対象から読み込みます。失敗の oldValue も Serializer へ渡し、保存先は作成しません。</summary>
        public async Task<Result<TValue>> Load(TFileId id, Result<TValue> oldValue)
        {
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                var path = GetFilePath(rootPath, id);
                await using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: 81920, useAsync: true);
                return await serializer.Deserialize(stream, oldValue).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        /// <summary>ファイルを削除します。明確な不存在は成功、それ以外の失敗は保持します。</summary>
        public Task<Result> Delete(TFileId id)
        {
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                var path = GetFilePath(rootPath, id);
                try
                {
                    File.Delete(path);
                }
                catch (DirectoryNotFoundException)
                {
                    FileSystemPath.RequireVolumeRoot(path);
                }
                return Task.FromResult(Result.Success);
            }
            catch (Exception exception)
            {
                return Task.FromResult<Result>(exception);
            }
        }

        /// <summary>対象がファイルであることを確認します。次の I/O の成功を保証するものではありません。</summary>
        public Task<Result> Exists(TFileId id)
        {
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                FileSystemPath.RequireFile(GetFilePath(rootPath, id));
                return Task.FromResult(Result.Success);
            }
            catch (Exception exception)
            {
                return Task.FromResult<Result>(exception);
            }
        }

        /// <summary>全階層の対象接尾辞の ID を返します。一時ファイルを除外し、未作成ルートでは空配列です。</summary>
        public Task<Result<TFileId[]>> GetAllIds()
        {
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                if (!FileSystemPath.DirectoryIsPresent(rootPath))
                    return Task.FromResult<Result<TFileId[]>>(Array.Empty<TFileId>());

                var ids = new List<TFileId>();
                foreach (var path in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    if (IsTemporaryFile(path) || !path.EndsWith(Extension, StringComparison.Ordinal)) continue;

                    var relativePath = Path.GetRelativePath(rootPath, path);
                    var name = Extension.Length == 0 ? relativePath : relativePath[..^Extension.Length];
                    ids.Add(GetIdFromRelativePathWithoutExtension(name));
                }
                return Task.FromResult<Result<TFileId[]>>(ids.ToArray());
            }
            catch (Exception exception)
            {
                return Task.FromResult<Result<TFileId[]>>(exception);
            }
        }

        private string CreateTemporaryPath(string parentPath)
        {
            string name;
            do
            {
                name = TemporaryFilePrefix + Guid.NewGuid().ToString("N");
            }
            while (Extension.Length != 0 && name.EndsWith(Extension, StringComparison.Ordinal));
            return Path.Combine(parentPath, name);
        }

        private static void Commit(string temporaryPath, string path)
        {
            try
            {
                File.Move(temporaryPath, path);
            }
            catch (IOException moveFailure)
            {
                // netstandard2.1 では上書き付き Move を使用できない。
                // 新規配置の失敗後に対象を確認し、既存のファイルだけを Replace する。
                try
                {
                    FileSystemPath.RequireFile(path);
                }
                catch (Exception inspectionFailure)
                {
                    throw new AggregateException(moveFailure, inspectionFailure);
                }

                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            }
        }

        private static bool IsTemporaryFile(string path)
            => Path.GetFileName(path).StartsWith(TemporaryFilePrefix, StringComparison.OrdinalIgnoreCase);

        private static void ValidateExtension(string extension)
        {
            if (extension is null) throw new ArgumentNullException(nameof(extension));
            if (extension.Length == 0) return;

            if (extension[0] != '.' || extension.Length == 1 ||
                extension.EndsWith(".", StringComparison.Ordinal) || extension.EndsWith(" ", StringComparison.Ordinal) ||
                extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                extension.IndexOfAny(['/', '\\', '*', '?']) >= 0)
            {
                throw new ArgumentException("An extension must be empty or a valid dot-prefixed suffix.", nameof(extension));
            }
        }
    }
}
