namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public sealed class NotepadData(string title, string text)
    {
        public string Title { get; } = title;
        public string Text { get; } = text;
    }
}
