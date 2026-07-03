using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using System.Collections;
using System.Collections.Generic;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Datas
{
    public class NotepadUserData(NotepadUserKey notepadUserKey) : IEnumerable<NotepadDataKey>
    {
        public NotepadUserKey UserId { get; } = notepadUserKey;

        private HashSet<NotepadDataKey> Notes { get; } = [];

        public bool Contains(NotepadDataKey notepad)
        {
            return Notes.Contains(notepad);
        }

        public bool Add(NotepadDataKey notepad)
        {
            return Notes.Add(notepad);
        }

        public bool Remove(NotepadDataKey notepad)
        {
            return Notes.Remove(notepad);
        }

        public bool Clear()
        {
            if (Notes.Count == 0)
            {
                return false;
            }
            else
            {
                Notes.Clear();
                return true;
            }
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
