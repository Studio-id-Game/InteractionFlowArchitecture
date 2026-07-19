using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Persistences
{
    /// <summary>
    /// ディレクトリ単位で値を永続化するデフォルト実装基底クラスです。
    /// </summary>
    /// <remarks>
    /// このクラスは保存先ディレクトリの作成、削除、列挙と、
    /// ディレクトリを利用した読み書きの呼び出しを担当します。
    /// メモリー上の値の生成、保持、破棄は Core の Storage 側の責務です。
    /// </remarks>
    /// <typeparam name="TDirectoryId">ディレクトリを識別する ID の型。</typeparam>
    /// <typeparam name="TValue">保存または読み込みする値の型。</typeparam>
    public abstract class DirectoryPersistence<TDirectoryId, TValue> : IDirectoryPersistencePort<TDirectoryId, TValue>
    {
        private readonly IDependencyNode[] dependency;

        /// <summary>
        /// この Persistence が依存する補助ノードを取得します。
        /// </summary>
        public virtual ReadOnlyMemory<IDependencyNode> Dependency => dependency;

        /// <summary>
        /// ルートパス配下の保存先を使用できるように初期化します。
        /// </summary>
        /// <param name="dependency">この Persistence が依存する補助ノード。</param>
        public DirectoryPersistence(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
            CreateDirectories(Environment.CurrentDirectory, RootPath);
        }

        /// <summary>
        /// 保存先のルートパスを取得します。
        /// </summary>
        public virtual string RootPath => Environment.CurrentDirectory;

        /// <summary>
        /// 指定された ID のディレクトリを削除します。存在しない場合も成功結果を返します。
        /// </summary>
        /// <param name="id">削除対象を識別する ID。</param>
        /// <returns>削除結果。</returns>
        public Task<Result> Delete(TDirectoryId id)
        {
            try
            {
                var path = GetDirectoryPath(id);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                }

                return Task.FromResult(Result.Success);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        /// <summary>
        /// 指定された ID のディレクトリが存在するかを確認します。
        /// </summary>
        /// <param name="id">存在確認する ID。</param>
        /// <returns>存在する場合は成功結果。存在しない場合は失敗結果。</returns>
        public Task<Result> Exists(TDirectoryId id)
        {
            try
            {
                var path = GetDirectoryPath(id);

                if (Directory.Exists(path))
                {
                    return Task.FromResult(Result.Success);
                }
                else
                {
                    throw new DirectoryNotFoundException($"Directory not found. {path}");
                }
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        /// <summary>
        /// ディレクトリ名から ID を取得します。
        /// </summary>
        /// <param name="directoryName">ディレクトリ名。</param>
        /// <returns>ディレクトリ ID。</returns>
        public abstract TDirectoryId GetDirectoryId(string directoryName);

        /// <summary>
        /// ID からディレクトリ名を取得します。
        /// </summary>
        /// <param name="id">ディレクトリ ID。</param>
        /// <returns>ディレクトリ名。</returns>
        public abstract string GetDirectoryName(TDirectoryId id);

        /// <summary>
        /// 指定されたディレクトリから値を読み込む派生クラス固有の処理です。
        /// </summary>
        /// <param name="path">読み込み対象のディレクトリパス。</param>
        /// <param name="id">読み込み対象を識別する ID。</param>
        /// <param name="oldValue">読み込み時に参照または再利用する既存値。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        protected abstract Task<Result<TValue>> LoadDirectory(string path, TDirectoryId id, Result<TValue> oldValue);

        /// <summary>
        /// 指定されたディレクトリへ値を保存する派生クラス固有の処理です。
        /// </summary>
        /// <param name="path">保存先のディレクトリパス。</param>
        /// <param name="id">保存先を識別する ID。</param>
        /// <param name="value">保存する値。</param>
        /// <returns>保存結果。</returns>
        protected abstract Task<Result> SaveDirectory(string path, TDirectoryId id, Result<TValue> value);

        /// <summary>
        /// ID から保存ディレクトリのパスを取得します。
        /// </summary>
        /// <param name="id">ディレクトリ ID。</param>
        /// <returns>保存ディレクトリのパス。</returns>
        public string GetDirectoryPath(TDirectoryId id)
        {
            return Path.Combine(RootPath, GetDirectoryName(id));
        }

        /// <summary>
        /// 指定された ID のディレクトリから値を読み込みます。
        /// </summary>
        /// <param name="id">読み込み対象を識別する ID。</param>
        /// <param name="oldValue">読み込み時に参照または再利用する既存値。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        public Task<Result<TValue>> Load(TDirectoryId id, Result<TValue> oldValue)
        {
            var path = GetDirectoryPath(id);
            CreateDirectories(RootPath, path);
            return LoadDirectory(path, id, oldValue);
        }

        /// <summary>
        /// 指定された ID のディレクトリへ値を保存します。
        /// </summary>
        /// <param name="id">保存先を識別する ID。</param>
        /// <param name="value">保存する値。</param>
        /// <returns>保存結果。</returns>
        public Task<Result> Save(TDirectoryId id, Result<TValue> value)
        {
            var path = GetDirectoryPath(id);
            CreateDirectories(RootPath, path);
            return SaveDirectory(path, id, value);
        }

        /// <summary>
        /// ルート直下にあるディレクトリ ID をすべて取得します。
        /// </summary>
        /// <returns>保存されているディレクトリ ID の配列。失敗時は失敗結果。</returns>
        public virtual Task<Result<TDirectoryId[]>> GetAllIds()
        {
            try
            {
                var directorys = Directory.GetDirectories(RootPath);
                var ids = directorys
                    .Select(e => Path.GetRelativePath(RootPath, e))
                    .Select(GetDirectoryId)
                    .ToArray();

                return Task.FromResult<Result<TDirectoryId[]>>(ids);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result<TDirectoryId[]>>(e);
            }
        }

        /// <summary>
        /// ルート配下に限定して対象ディレクトリを作成します。
        /// </summary>
        /// <param name="root">作成を許可するルートディレクトリ。</param>
        /// <param name="target">作成対象のディレクトリ。</param>
        protected static void CreateDirectories(string root, string target)
        {
            DirectoryUtility.CreateDirectories(root, target);
        }
    }
}
