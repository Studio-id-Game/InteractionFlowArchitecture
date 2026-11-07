using InteractionFlow.Core.Entities.Contexts;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public sealed class NotepadContext : FlowContext
    {
        public string? User { get; set; }
        public bool ExitRequested { get; set; }
        public NotepadAction Action { get; set; }
        public override bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            if (this is T self) { value = self; return true; }
            return base.TryGet(out value);
        }
    }

    public enum NotepadAction { None, Edit, Create, Delete, Login, List }
}
