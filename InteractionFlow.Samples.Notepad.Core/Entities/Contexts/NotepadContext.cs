using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using System;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Contexts
{
    public class NotepadContext(NotepadUserObject userObject) : FlowContext(userObject)
    {
        public NotepadContext() : this(NotepadUserObject.Public)
        {
        }

        public new NotepadUserObject User { get; } = userObject;

        public NotepadDataKey CurrentNotepadKey { get; set; } = NotepadDataKey.Empty;

        public override bool TrySet<T>(T? value) where T : default
        {
            if (value is NotepadDataKey notepadDataKey)
            {
                CurrentNotepadKey = notepadDataKey;
                return true;
            }

            return base.TrySet(value);
        }

        public override bool TrySet<T>(Func<T> select)
        {
            if (select is Func<NotepadDataKey> notepadDataKey)
            {
                CurrentNotepadKey = notepadDataKey();
                return true;
            }

            return base.TrySet(select);
        }

        public override bool TryGet<T>(out T? value) where T : default
        {
            value = default;

            if (TryCast(User, ref value))
                return true;

            if (TryCast(User.NotepadUserKey, ref value))
                return true;

            if (TryCast(CurrentNotepadKey, ref value))
                return true;

            return base.TryGet(out value);
        }

        private static bool TryCast<T2, T>(T2? newValue, ref T? value)
        {
            if (newValue != null && newValue is T newValueT)
            {
                value = newValueT;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
