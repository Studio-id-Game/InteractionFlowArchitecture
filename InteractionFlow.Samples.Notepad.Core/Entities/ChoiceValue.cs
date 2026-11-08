namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    /// <summary>選択結果です。null は中断、-1 は不正入力、0 以上は選択位置です。</summary>
    public readonly struct ChoiceValue(int? index)
    {
        public int? Index { get; } = index;
    }
}
