using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface INoteEditorOperation : IOperationPort
    {
        // Returned failures are preserved; thrown exceptions and Task cancellation propagate.
        Task<Result<NoteEditValue>> EditAsync(NotepadContext context, NotepadData? original);
    }
}
