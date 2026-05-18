using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.StoragePorts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Storages
{
    public abstract class DirectoryStorageModifiable<TValue, TStorage>(TStorage cacheStorage)
        : ExternalStorageModifiable<TValue, TStorage>(cacheStorage)
        where TStorage : IStoragePortModifiable<TValue>
    {
        protected sealed override async Task<Result<TValue>> LoadFromPersistentCoreAsync(IFlowContext context)
        {
            var directory = GetDirectoryInfo(context);

            if (directory == null)
                return new InvalidOperationException($"Can not get DirectoryInfo.");

            var valueResult = await ReadDirectoryAsync(context, directory);

            if (!valueResult)
                return valueResult;

            return ValidateNormalize(valueResult.Value!);
        }

        protected sealed override async Task<Result> SaveToPersistentCoreAsync(IFlowContext context, TValue? value)
        {
            if (value == null)
                return new InvalidOperationException($"Value is null.");

            var valueResult = ValidateNormalize(value);
            if (!valueResult)
                return valueResult.AsResult;

            var file = GetDirectoryInfo(context);

            if (file == null)
                return new InvalidOperationException($"Can not get DirectoryInfo.");

            return await WriteDirectoryAsync(context, file, value);
        }

        protected abstract DirectoryInfo? GetDirectoryInfo(IFlowContext context);

        protected abstract Task<Result<TValue>> ReadDirectoryAsync(IFlowContext context, DirectoryInfo directory);

        protected abstract Task<Result> WriteDirectoryAsync(IFlowContext context, DirectoryInfo directory, TValue value);

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
