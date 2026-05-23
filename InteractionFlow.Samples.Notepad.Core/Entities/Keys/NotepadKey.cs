using InteractionFlow.Samples.Notepad.Core.Entities.Rules;
using System;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Keys
{
    public readonly record struct NotepadDataKey(string UserId, string NoteId)
    {
        public static NotepadDataKey CreateNew(NotepadUserKey userKey) => new(userKey.Id, Guid.NewGuid().ToString());

        public static NotepadDataKey Empty { get; } = new(new(string.Empty), string.Empty);

        public string UserId { get; } = UserId;
        public string NoteId { get; } = NoteId;

        public readonly bool IsEmpty => this == Empty;

        public bool IsValid => !IsEmpty && NotepadRule.IsValidID(UserId) && NotepadRule.IsValidID(NoteId);

        public NotepadUserKey UserKey => new(UserId);

        public FileInfo? GetNoteFile()
        {
            if (!IsValid)
                return null;

            var userKey = new NotepadUserKey(UserId);
            var userDirectory = userKey.GetUserDirectory();

            if (userDirectory == null)
                return null;

            return new(Path.Combine(userDirectory.FullName, NoteId + NotepadRule.Extention));
        }

        public static NotepadDataKey? CreateFromNoteFile(FileInfo noteFile)
        {
            var userKey = NotepadUserKey.CreateFromUserDirectory(noteFile.Directory!);

            if (userKey == null)
                return null;

            var noteId = Path.GetFileNameWithoutExtension(noteFile.Name);

            return new(userKey.Value.Id, noteId);
        }
    }
}
