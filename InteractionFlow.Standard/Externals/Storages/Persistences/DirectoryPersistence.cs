using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Persistences
{
    public abstract class DirectoryPersistence<TDirectoryId, TValue> : IDirectoryPersistencePort<TDirectoryId, TValue>
    {
        public DirectoryPersistence()
        {
            CreateDirectories(Environment.CurrentDirectory, RootPath);
        }

        public virtual string RootPath => Environment.CurrentDirectory;

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

        public Task<Result> Exist(TDirectoryId id)
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

        public abstract TDirectoryId GetDirectoryId(string directoryName);

        public abstract string GetDirectoryName(TDirectoryId id);

        protected abstract Task<Result<TValue>> LoadDirectory(string path, TDirectoryId id, Result<TValue> oldValue);

        protected abstract Task<Result> SaveDirectory(string path, TDirectoryId id, Result<TValue> value);

        public string GetDirectoryPath(TDirectoryId id)
        {
            return Path.Combine(RootPath, GetDirectoryName(id));
        }

        public Task<Result<TValue>> Load(TDirectoryId id, Result<TValue> oldValue)
        {
            var path = GetDirectoryPath(id);
            CreateDirectories(RootPath, path);
            return LoadDirectory(path, id, oldValue);
        }

        public Task<Result> Save(TDirectoryId id, Result<TValue> value)
        {
            var path = GetDirectoryPath(id);
            CreateDirectories(RootPath, path);
            return SaveDirectory(path, id, value);
        }

        public virtual Task<Result<TDirectoryId[]>> GetAllId()
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

        protected static void CreateDirectories(string root, string target)
        {
            DirectoryUtility.CreateDirectories(root, target);
        }
    }
}
