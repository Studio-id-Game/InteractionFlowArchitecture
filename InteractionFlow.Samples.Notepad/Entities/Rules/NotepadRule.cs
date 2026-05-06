using System;
using System.Collections.Generic;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Entities.Rules
{

    internal static class NotepadRule
    {
        private static HashSet<char> InvalidChars => [.. Path.GetInvalidFileNameChars(), .. Path.GetInvalidPathChars()];

        public static DirectoryInfo RootDirectoryInfo => new(Path.Combine(Environment.CurrentDirectory, "Notes"));

        public static string PublicUserName => "Public";

        public static string Extention => ".ifnote";

        public static bool IsValidID(string id)
        {
            var userId = id;
            for (var i = 0; i < userId.Length; i++)
            {
                var c = id[i];
                if (InvalidChars.Contains(c))
                    return false;
            }

            return true;
        }
    }
}
