using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface INotepadLoginOperation : IOperationPort
    {
        Task<Result> LoginAsync(NotepadContext context);
    }
}
