using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Persistences
{

    /// <summary>
    /// ファイル単位で値を永続化するデフォルト実装基底クラスです。
    /// </summary>
    /// <remarks>
    /// このクラスは保存先ファイルの読み書きと列挙を担当します。
    /// メモリー上の値の生成、保持、破棄は Core の Storage 側の責務です。
    /// </remarks>
    /// <typeparam name="TFileId">ファイルを識別する ID の型。</typeparam>
    /// <typeparam name="TValue">保存または読み込みする値の型。</typeparam>
    /// <param name="serializer">値とストリームの変換に使用する Serializer。</param>
    /// <param name="dependency">この Persistence が依存する補助ノード。</param>
    public abstract class FilePersistence<TFileId, TValue>(
        ISerializerPort<Stream, TValue> serializer,
        params IDependencyNode[] dependency)
        : IFilePersistencePort<TFileId, TValue>
    {
        private readonly IDependencyNode[] dependencies = [serializer, .. dependency];

        /// <summary>
        /// この Persistence が依存する補助ノードを取得します。
        /// </summary>
        public virtual ReadOnlyMemory<IDependencyNode> Dependency => dependencies;

        /// <summary>
        /// 保存先のルートパスを取得します。
        /// </summary>
        public virtual string RootPath => Environment.CurrentDirectory;

        /// <summary>
        /// 保存ファイルの拡張子を取得します。
        /// </summary>
        public virtual string Extention => ".bin";

        /// <summary>
        /// 拡張子を含まないファイル名から ID を取得します。
        /// </summary>
        /// <param name="fileName">拡張子を含まないファイル名。</param>
        /// <returns>ファイル ID。</returns>
        public abstract TFileId GetFileId(string fileName);

        /// <summary>
        /// ID から拡張子を含まないファイル名を取得します。
        /// </summary>
        /// <param name="fileID">ファイル ID。</param>
        /// <returns>拡張子を含まないファイル名。</returns>
        public abstract string GetFileName(TFileId fileID);

        /// <summary>
        /// ファイルパスからルート相対の ID を取得します。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <returns>ファイル ID。</returns>
        public TFileId GetFileIdFromPath(string filePath)
        {
            var fileName = Path.ChangeExtension(Path.GetRelativePath(RootPath, filePath), null);
            return GetFileId(fileName);
        }

        private async Task<Result<TValue>> LoadFile(string path, Result<TValue> oldValue)
        {
            try
            {
                await using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                return await serializer.Deserialize(stream, oldValue).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return e;
            }
        }

        private async Task<Result> SaveFile(string path, Result<TValue> value)
        {
            try
            {
                await using var stream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                return await serializer.Serialize(value, stream)
                    .ThenAsync(stream2 =>
                    {
                        return Task.FromResult(Result.Success);
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>
        /// ID から保存ファイルのパスを取得します。
        /// </summary>
        /// <param name="id">ファイル ID。</param>
        /// <returns>保存ファイルのパス。</returns>
        public string GetFilePath(TFileId id)
        {
            return Path.Combine(RootPath, GetFileName(id)) + Extention;
        }

        /// <summary>
        /// 指定された ID のファイルへ値を保存します。
        /// </summary>
        /// <param name="id">保存先を識別する ID。</param>
        /// <param name="value">保存する値。</param>
        /// <returns>保存結果。</returns>
        public Task<Result> Save(TFileId id, Result<TValue> value)
        {
            try
            {
                var path = GetFilePath(id);

                CreateDirectories(RootPath, Path.GetDirectoryName(path));
                return SaveFile(path, value);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        /// <summary>
        /// 指定された ID のファイルから値を読み込みます。
        /// </summary>
        /// <param name="id">読み込み対象を識別する ID。</param>
        /// <param name="oldValue">読み込み時に参照または再利用する既存値。</param>
        /// <returns>読み込まれた値。失敗時は失敗結果。</returns>
        public Task<Result<TValue>> Load(TFileId id, Result<TValue> oldValue)
        {
            try
            {
                var path = GetFilePath(id);

                CreateDirectories(RootPath, Path.GetDirectoryName(path));
                return LoadFile(path, oldValue);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result<TValue>>(e);
            }
        }

        /// <summary>
        /// 指定された ID のファイルを削除します。存在しない場合も成功結果を返します。
        /// </summary>
        /// <param name="id">削除対象を識別する ID。</param>
        /// <returns>削除結果。</returns>
        public Task<Result> Delete(TFileId id)
        {
            try
            {
                var path = GetFilePath(id);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return Task.FromResult(Result.Success);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        /// <summary>
        /// 指定された ID のファイルが存在するかを確認します。
        /// </summary>
        /// <param name="id">存在確認する ID。</param>
        /// <returns>存在する場合は成功結果。存在しない場合は失敗結果。</returns>
        public Task<Result> Exists(TFileId id)
        {
            try
            {
                var path = GetFilePath(id);

                if (File.Exists(path))
                {
                    return Task.FromResult(Result.Success);
                }
                else
                {
                    throw new FileNotFoundException("File not found.", path);
                }
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        /// <summary>
        /// ルート配下にある対象拡張子のファイル ID をすべて取得します。
        /// </summary>
        /// <returns>保存されているファイル ID の配列。失敗時は失敗結果。</returns>
        public Task<Result<TFileId[]>> GetAllIds()
        {
            try
            {
                var files = Directory.GetFiles(RootPath, $"*{Extention}", SearchOption.AllDirectories);
                var ids = files
                    .Select(e => Path.GetRelativePath(RootPath, e))
                    .Select(e => Path.ChangeExtension(e, null))
                    .Select(GetFileId)
                    .ToArray();

                return Task.FromResult<Result<TFileId[]>>(ids);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result<TFileId[]>>(e);
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
