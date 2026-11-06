using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.FileSystem.Externals
{
    /// <summary>ID をディレクトリへ対応付け、内容の永続化と列挙を派生実装へ委ねます。</summary>
    /// <remarks>内容更新の整合性やロールバックは派生実装の責務です。単一ファイル保存の保証は適用しません。</remarks>
    public abstract class DirectoryPersistence<TDirectoryId, TValue> : IPersistencePort<TDirectoryId, TValue>
    {
        private readonly IDependencyNode[] dependency;

        /// <summary>補助ノードを保持します。ディレクトリは作成しません。</summary>
        protected DirectoryPersistence(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
        }

        /// <summary>依存する補助ノード。</summary>
        public virtual ReadOnlyMemory<IDependencyNode> Dependency => dependency;

        /// <summary>現在のルートを返します。相対パスも許可し、各操作で取得直後に一度だけ絶対パス化します。</summary>
        public virtual string GetCurrentRootPath() => Environment.CurrentDirectory;

        /// <summary>ID を階層も許可するルート相対ディレクトリパスへ変換します。</summary>
        protected abstract string GetRelativeDirectoryPath(TDirectoryId id);

        /// <summary>確定済みの絶対ルートを使用して、ID のディレクトリパスを解決・検証します。</summary>
        protected string GetDirectoryPath(string rootPath, TDirectoryId id)
            => FileSystemPath.Resolve(rootPath, GetRelativeDirectoryPath(id));

        /// <summary>保存ごとに必要な状態を整えます。既定はディレクトリ作成で、既存でも成功します。</summary>
        protected virtual Task<Result> EnsureDirectory(string path, TDirectoryId id)
        {
            try
            {
                Directory.CreateDirectory(path);
                return Task.FromResult(Result.Success);
            }
            catch (Exception exception) { return Task.FromResult<Result>(exception); }
        }

        /// <summary>内容を保存します。渡されたパスを使い、失敗時の整合性は派生側で定義します。</summary>
        protected abstract Task<Result> SaveDirectory(string path, TDirectoryId id, Result<TValue> value);

        /// <summary>存在確認済みの対象から読み込みます。既存値を再利用する場合の整合性も派生側の責務です。</summary>
        protected abstract Task<Result<TValue>> LoadDirectory(string path, TDirectoryId id, Result<TValue> oldValue);

        /// <summary>既定は全内容の再帰削除です。親を削除すると、その下の別 ID の対象も削除されます。</summary>
        /// <remarks>未作成対象にも呼ばれます。派生側の失敗結果は成功へ置き換えられません。</remarks>
        protected virtual Task<Result> DeleteDirectory(string path, TDirectoryId id)
        {
            try
            {
                try { Directory.Delete(path, recursive: true); }
                catch (DirectoryNotFoundException) { FileSystemPath.RequireVolumeRoot(path); }
                return Task.FromResult(Result.Success);
            }
            catch (Exception exception) { return Task.FromResult<Result>(exception); }
        }

        /// <summary>確定済みルートから全保存対象の ID を列挙します。未作成ルートには呼ばれません。</summary>
        /// <remarks>保存対象でない中間・補助ディレクトリを除外し、ID を重複させません。順序は保証しません。</remarks>
        protected abstract Task<Result<TDirectoryId[]>> GetDirectoryIds(string rootPath);

        /// <summary>入力を確認し、EnsureDirectory の成功後に内容を保存します。</summary>
        public async Task<Result> Save(TDirectoryId id, Result<TValue> value)
        {
            if (!value.Try(out _, out var inputFailure)) return inputFailure;

            string path;
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                path = GetDirectoryPath(rootPath, id);
            }
            catch (Exception exception) { return exception; }

            var ensured = await EnsureDirectory(path, id).ConfigureAwait(false);
            if (!ensured.Try(out _)) return ensured;
            return await SaveDirectory(path, id, value).ConfigureAwait(false);
        }

        /// <summary>対象の存在を確認してから読込へ委譲します。準備処理や作成は行いません。</summary>
        public async Task<Result<TValue>> Load(TDirectoryId id, Result<TValue> oldValue)
        {
            string path;
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                path = GetDirectoryPath(rootPath, id);
                FileSystemPath.RequireDirectory(path);
            }
            catch (Exception exception) { return exception; }

            return await LoadDirectory(path, id, oldValue).ConfigureAwait(false);
        }

        /// <summary>不存在でも拡張点へ委譲します。派生実装の失敗はそのまま保持します。</summary>
        public async Task<Result> Delete(TDirectoryId id)
        {
            string path;
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                path = GetDirectoryPath(rootPath, id);
            }
            catch (Exception exception) { return exception; }

            return await DeleteDirectory(path, id).ConfigureAwait(false);
        }

        /// <summary>対象がディレクトリであることを確認します。保存先の作成は行いません。</summary>
        public Task<Result> Exists(TDirectoryId id)
        {
            try
            {
                var rootPath = Path.GetFullPath(GetCurrentRootPath());
                FileSystemPath.RequireDirectory(GetDirectoryPath(rootPath, id));
                return Task.FromResult(Result.Success);
            }
            catch (Exception exception)
            {
                return Task.FromResult<Result>(exception);
            }
        }

        /// <summary>ルートの確定と例外変換を行い、列挙を派生側へ委譲します。未作成なら空配列です。</summary>
        public async Task<Result<TDirectoryId[]>> GetAllIds()
        {
            string rootPath;
            try
            {
                rootPath = Path.GetFullPath(GetCurrentRootPath());
                if (!FileSystemPath.DirectoryIsPresent(rootPath)) return Array.Empty<TDirectoryId>();
            }
            catch (Exception exception) { return exception; }

            return await GetDirectoryIds(rootPath).ConfigureAwait(false);
        }
    }
}
