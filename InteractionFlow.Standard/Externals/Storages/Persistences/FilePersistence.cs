using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Persistences
{

    public abstract class FilePersistence<TFileId, TValue>(ISerializerPort<Stream, TValue> serializer) : IFilePersistencePort<TFileId, TValue>
    {
        public virtual string RootPath => Environment.CurrentDirectory;

        public virtual string Extention => ".bin";

        public abstract TFileId GetFileId(string fileName);

        public abstract string GetFileName(TFileId fileID);

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

                return await serializer.Deserialize(stream, oldValue);
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

                var streamResult = await serializer.Serialize(value, stream);

                if (!streamResult)
                {
                    throw streamResult.Exception!;
                }

                var stream2 = streamResult.Value!;

                await stream.FlushAsync();

                if (stream2 != stream)
                {
                    await stream2.FlushAsync();
                    await stream2.DisposeAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public string GetFilePath(TFileId id)
        {
            return Path.Combine(RootPath, GetFileName(id)) + Extention;
        }

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

        public Task<Result> Delete(TFileId id)
        {
            try
            {
                var path = GetFilePath(id);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return Task.FromResult<Result>(true);
            }
            catch (Exception e)
            {
                return Task.FromResult<Result>(e);
            }
        }

        public Task<Result> Exist(TFileId id)
        {
            try
            {
                var path = GetFilePath(id);

                if (File.Exists(path))
                {
                    return Task.FromResult<Result>(true);
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

        public Task<Result<TFileId[]>> GetAllId()
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

        protected static void CreateDirectories(string root, string target)
        {
            DirectoryUtility.CreateDirectories(root, target);
        }
    }
}
