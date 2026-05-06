using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Standard.Entities.Storages;

namespace InteractionFlow.Samples.Notepad.Entities.Datas
{
    internal class NotepadData(NotepadDataKey noteKey) : IKeyedMemoryValue<NotepadDataKey>
    {
        private string title = "New Note";
        private string text = string.Empty;
        private bool hasChenged = false;

        public NotepadData() : this(NotepadDataKey.Empty)
        {
        }

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

        public NotepadDataKey NoteKey { get; private set; } = noteKey;

        public bool TryInitialize(IFlowContext context, NotepadDataKey contextKey)
        {
            NoteKey = contextKey;
            return true;
        }
    }
}
