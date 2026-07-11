using InteractionFlow.Samples.Notepad.Core.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Contexts
{
    public class NotepadUserObject(NotepadUserKey userKey)
    {
        public NotepadUserObject() : this(NotepadUserKey.Public)
        {

        }

        public static NotepadUserObject Public => new();

        public NotepadUserKey NotepadUserKey { get; } = userKey;

        public string Id => NotepadUserKey.Id;

        public string Name => NotepadUserKey.Name;
    }
}
