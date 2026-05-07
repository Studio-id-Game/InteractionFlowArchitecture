using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Core.Storages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Storages
{
    public abstract class FileStorageModifiable<TValue, TStorage>(TStorage cacheStorage)
        : ExternalStorageModifiable<TValue, TStorage>(cacheStorage)
        where TStorage : IStoragePortModifiable<TValue>
    {
        protected override async Task<Result<TValue>> LoadFromPersistentCore(IFlowContext context)
        {
            var file = GetFileInfo(context);

            if (file == null)
                return new InvalidOperationException($"Can not get FileInfo.");

            if (!file.Exists)
            {
                return new FileNotFoundException(file.FullName);
            }

            var valueResult = await ReadFileAsync(context, file);

            if (!valueResult)
                return valueResult;

            return ValidateNormalize(valueResult.Value!);
        }

        protected override async Task<Result> SaveToPersistentCore(IFlowContext context, TValue value)
        {
            var valueResult = ValidateNormalize(value);
            if (!valueResult)
                return valueResult.AsResult;

            var file = GetFileInfo(context);

            if (file == null)
                return new InvalidOperationException($"Can not get FileInfo.");

            return await WriteFileAsync(context, file, value);
        }

        protected abstract FileInfo? GetFileInfo(IFlowContext context);

        protected abstract Task<Result<TValue>> ReadFileAsync(IFlowContext context, FileInfo file);

        protected abstract Task<Result> WriteFileAsync(IFlowContext context, FileInfo file, TValue value);

        protected virtual Result<TValue> ValidateNormalize(TValue value)
        {
            return value;
        }

        protected static void MakeDirectory(DirectoryInfo? dir)
        {
            List<DirectoryInfo> mkDir = [];
            while (dir != null && !dir.Exists)
            {
                mkDir.Insert(0, dir);
                dir = dir.Parent;
            }

            foreach (var d in mkDir)
            {
                d.Create();
            }
        }
    }
}
