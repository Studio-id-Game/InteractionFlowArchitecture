using InteractionFlow.Samples.Notepad.Core.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Datas
{
    public class NotepadData(NotepadDataKey noteKey)
    {
        public NotepadDataKey NoteKey { get; } = noteKey;

        private string title = "New Note";
        private string text = string.Empty;
        private bool hasChenged = false;

        public bool HasChenged => hasChenged;

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    hasChenged = true;
                }
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    hasChenged = true;
                }
            }
        }

        public void ChangeSaved()
        {
            hasChenged = false;
        }

    }
}
