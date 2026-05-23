using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Contexts
{
    public class NotepadUserObject(NotepadUserKey userKey) : UserObject(userKey.Id)
    {
        public NotepadUserObject() : this(NotepadUserKey.Public)
        {

        }

        public static NotepadUserObject Public => new();

        public NotepadUserKey NotepadUserKey { get; } = userKey;
    }
}
