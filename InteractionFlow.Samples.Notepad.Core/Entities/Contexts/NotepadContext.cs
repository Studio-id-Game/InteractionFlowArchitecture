using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Contexts
{
    public class NotepadContext(NotepadUserObject userObject) : FlowContext
    {
        public NotepadContext() : this(NotepadUserObject.Public)
        {
        }

        public NotepadUserObject User { get; } = userObject;

        public NotepadDataKey CurrentNotepadKey { get; set; } = NotepadDataKey.Empty;

        public override bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            value = default;

            if (User is T user)
            {
                value = user;
                return true;
            }

            if (User.NotepadUserKey is T userKey)
            {
                value = userKey;
                return true;
            }

            if (CurrentNotepadKey is T currentNotepadKey)
            {
                value = currentNotepadKey;
                return true;
            }

            return base.TryGet(out value);
        }
    }
}
