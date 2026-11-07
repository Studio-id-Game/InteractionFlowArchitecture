using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.FileSystem.Externals;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    public class NotepadDataFilePersistence(
        INotepadDataSerializerPort serializer, params IDependencyNode[] dependency)
        : FilePersistence<NotepadKey, NotepadData>(serializer, ".note", dependency), INotepadDataPersistencePort
    {
        public override string GetCurrentRootPath() => NotepadPaths.Root("plain");
        protected override string GetRelativePathWithoutExtension(NotepadKey id) => id.RelativePath();
        protected override NotepadKey GetIdFromRelativePathWithoutExtension(string path)
            => NotepadKey.FromRelativePath(path);
    }
}
