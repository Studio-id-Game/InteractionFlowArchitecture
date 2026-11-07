using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using InteractionFlow.Standard.FileSystem.Externals;
using System;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class CredentialFiles(ICredentialSerializerPort serializer)
        : FilePersistence<string, CredentialData>(serializer, ".account"), ICredentialPersistencePort
    {
        public override string GetCurrentRootPath() => NotepadPaths.Root("secure-accounts");
        protected override string GetRelativePathWithoutExtension(string id)
        {
            if (id.Length != 0 && !NotepadRules.IsValidId(id)) throw new ArgumentException("Invalid user ID.");
            return NotepadRules.UserDirectory(id);
        }
        protected override string GetIdFromRelativePathWithoutExtension(string path)
        {
            if (path == "public") return "";
            if (path.StartsWith("user-", StringComparison.Ordinal) && NotepadRules.IsValidId(path.Substring(5)))
                return path.Substring(5);
            throw new FormatException("Invalid account path.");
        }
    }
}
