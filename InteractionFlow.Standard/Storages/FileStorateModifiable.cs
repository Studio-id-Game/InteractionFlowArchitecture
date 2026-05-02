using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Core.Storages;
using InteractionFlow.Standard.Entities.Storages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Storages
{
    public class FileStorateModifiable<TValue, TStorage> : ExternalStorageModifiable<TValue, TStorage>
        where TStorage : IStoragePortModifiable<TValue>, new()
        where TValue : IFileStorageValue, new()
    {
        public string BasePath { get; protected set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        protected override async Task<Result<TValue>> LoadFromPersistentCore(IFlowContext context)
        {
            try
            {
                if (!TryGetOrInitialize(context, out var value))
                {
                    return new InvalidOperationException();
                }

                var path = $"{Path.Join(BasePath, value!.FileName)}.{value.Extension}";
                var file = new FileInfo(path);


                using (var fileStream = file.OpenRead())
                {
                    await value.ReadFile(context, fileStream);
                }

                return new Result<TValue>(value);
            }
            catch (Exception e)
            {
                return e;
            }
        }

        private bool TryGetOrInitialize(IFlowContext context, out TValue? value)
        {
            if (TryGet(context, out value))
            {
                return true;
            }

            value = new();
            return value.TryInitialize(context);
        }

        protected override async Task<Result> SaveToPersistentCore(IFlowContext context, TValue value)
        {
            try
            {
                var path = $"{Path.Join(BasePath, value!.FileName)}.{value.Extension}";
                var file = new FileInfo(path);

                using var fileStream = file.OpenWrite();
                await value.WriteFile(context, fileStream);

                return true;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
