using System;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public static class NotepadRules
    {
        public const int MaxTitleLength = 200;
        public const int MaxTextLength = 1024 * 1024;

        public static bool IsValidId(string text)
        {
            if (text.Length is < 1 or > 64) return false;
            foreach (var c in text)
                if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-'))
                    return false;
            return true;
        }

        public static string NormalizeId(string text) => text.Trim().ToLowerInvariant();
        public static string UserDirectory(string user) => user.Length == 0 ? "public" : "user-" + user;
        public static string UserName(string user) => user.Length == 0 ? "Public" : user;
    }
}
