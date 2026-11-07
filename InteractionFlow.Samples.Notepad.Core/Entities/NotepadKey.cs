using System;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public readonly struct NotepadKey(string user, string note)
    {
        public string User { get; } = user;
        public string Note { get; } = note;

        public string RelativePath()
        {
            if ((User.Length != 0 && !NotepadRules.IsValidId(User)) || !NotepadRules.IsValidId(Note))
                throw new ArgumentException("Invalid user or note ID.");
            return NotepadRules.UserDirectory(User) + "/note-" + Note;
        }

        public static NotepadKey FromRelativePath(string path)
        {
            var parts = path.Replace('\\', '/').Split('/');
            if (parts.Length != 2 || !parts[1].StartsWith("note-", StringComparison.Ordinal))
                throw new FormatException("Invalid note path.");
            var user = parts[0] == "public" ? "" :
                parts[0].StartsWith("user-", StringComparison.Ordinal) ? parts[0][5..] : null;
            if (user == null || (parts[0] != "public" && !NotepadRules.IsValidId(user)))
                throw new FormatException("Invalid user directory.");
            var key = new NotepadKey(user, parts[1][5..]);
            key.RelativePath();
            return key;
        }
    }
}
