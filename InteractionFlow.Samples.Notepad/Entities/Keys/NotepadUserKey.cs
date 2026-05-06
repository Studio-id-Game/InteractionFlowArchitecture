using InteractionFlow.Samples.Notepad.Entities.Rules;
using System;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Entities.Keys
{
    internal readonly record struct NotepadUserKey(string Id)
    {
        public static NotepadUserKey Public => new("");

        public bool IsPublic => string.IsNullOrWhiteSpace(Id);

        public bool IsValid => IsPublic || NotepadRule.IsValidID(Id);

        public string Name => IsPublic ? NotepadRule.PublicUserName : Id;

        public DirectoryInfo? GetUserDirectory()
        {
            if (!IsValid)
                return null;

            return new(Path.Combine(NotepadRule.RootDirectoryInfo.FullName, Name));
        }

        public static NotepadUserKey? CreateFromUserDirectory(DirectoryInfo userDirectory)
        {
            var targetPath = userDirectory.FullName;
            var rootPath = NotepadRule.RootDirectoryInfo.FullName;

            if (!targetPath.StartsWith(rootPath))
            {
                return null;
            }

            var name = Path.GetRelativePath(rootPath, targetPath).Trim(Path.PathSeparator);

            if (name.Equals(NotepadRule.PublicUserName, StringComparison.Ordinal))
            {
                return Public;
            }
            else
            {
                return new NotepadUserKey(name);
            }
        }
    }
}
