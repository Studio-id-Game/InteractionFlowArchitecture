using InteractionFlow.Core.Entities.Contexts;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public sealed class NotepadContext : FlowContext
    {
        public string? User { get; set; }
        public bool ExitRequested { get; set; }
        public NotepadAction Action { get; set; }
        /// <summary>基底の追加処理を使い、チェーン後も NotepadContext として扱えるようにします。</summary>
        public new NotepadContext With<T>(T value)
        {
            base.With(value);
            return this;
        }

        protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
        {
            if (this is T self) { value = self; return true; }
            return base.TryGetCore(out value);
        }
    }

    public enum NotepadAction { None, Edit, Create, Delete, Login, List }
}
