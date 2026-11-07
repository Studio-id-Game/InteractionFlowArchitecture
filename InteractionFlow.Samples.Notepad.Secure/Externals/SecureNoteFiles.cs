using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class SecureNoteFiles(INotepadDataSerializerPort serializer) : NotepadDataFilePersistence(serializer)
    {
        public override string GetCurrentRootPath() => NotepadPaths.Root("secure");
    }
}
