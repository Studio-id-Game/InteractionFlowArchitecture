using InteractionFlow.Core.Entities.Contexts;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Entities.Storages
{
    public interface IFileStorageValue
    {
        public string FileName { get; }

        public string Extension { get; }

        public Task ReadFile(IFlowContext context, FileStream stream);

        public Task WriteFile(IFlowContext context, FileStream stream);

        bool TryInitialize(IFlowContext context);
    }
}
