using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface INoteEditorOperation : IOperationPort
    {
        Task<NotepadData?> EditAsync(NotepadContext context, NotepadData? original);
    }
}
