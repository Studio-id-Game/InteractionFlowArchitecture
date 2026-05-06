using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Standard.Entities.Storages;
using System.Collections;
using System.Collections.Generic;

namespace InteractionFlow.Samples.Notepad.Entities.Datas
{
    internal class NotepadUserData : IKeyedMemoryValue<NotepadUserKey>, IEnumerable<NotepadDataKey>
    {
        public NotepadUserKey UserId { get; private set; } = NotepadUserKey.Public;

        private HashSet<NotepadDataKey> Notes { get; } = [];

        public bool TryInitialize(IFlowContext context, NotepadUserKey contextKey)
        {
            UserId = contextKey;
            return true;
        }

        public bool Add(NotepadDataKey notepad)
        {
            return Notes.Add(notepad);
        }

        public bool Remove(NotepadDataKey notepad)
        {
            return Notes.Remove(notepad);
        }

        public IEnumerator<NotepadDataKey> GetEnumerator()
        {
            return Notes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Notes.GetEnumerator();
        }
    }
}
