namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    /// <summary>編集結果です。Note が null の場合は下書きの破棄を表します。</summary>
    public readonly struct NoteEditValue(NotepadData? note)
    {
        public NotepadData? Note { get; } = note;
    }
}
