namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    /// <summary>取得したキー、またはキーを取得しなかった理由を表します。</summary>
    public readonly struct ReadKeyValue
    {
        private enum ReadKeyStatus
        {
            Cancelled,
            Selected,
            NoNotes,
            InvalidId
        }

        private ReadKeyValue(ReadKeyStatus status, NotepadKey? key = null)
        {
            Status = status;
            Key = key;
        }

        private ReadKeyStatus Status { get; }
        public NotepadKey? Key { get; }

        /// <summary>キーを取得しなかった理由を表示用メッセージに変換します。</summary>
        /// <param name="cancelledMessage">呼び出し元の操作に対応した中断メッセージ。</param>
        public string GetMessage(string cancelledMessage) => Status switch
        {
            ReadKeyStatus.NoNotes => "No notes available.",
            ReadKeyStatus.InvalidId => "Invalid note ID. Use 1–64 letters, digits, underscores or hyphens.",
            _ => cancelledMessage
        };

        public static ReadKeyValue Selected(NotepadKey key) => new(ReadKeyStatus.Selected, key);
        public static ReadKeyValue Cancelled => default;
        public static ReadKeyValue NoNotes => new(ReadKeyStatus.NoNotes);
        public static ReadKeyValue InvalidId => new(ReadKeyStatus.InvalidId);
    }
}
