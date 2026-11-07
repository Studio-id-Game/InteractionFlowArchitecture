using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface INotepadDataPersistencePort : IPersistencePort<NotepadKey, NotepadData> { }
}
