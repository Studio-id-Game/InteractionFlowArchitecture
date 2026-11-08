using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    public sealed class ConsoleNoteEditor(IConsoleLineInputPort lines, IConsoleKeyInputPort keys,
        IConsoleOutputPort output, IConsoleScreenPort screen, IConsoleEnvironmentPort environment)
        : Operation(lines, keys, output, screen, environment), INoteEditorOperation
    {
        public override void ForceResetMemoryState() { }

        public async Task<NotepadData?> EditAsync(NotepadContext context, NotepadData? original)
        {
            if (environment.IsInputRedirected || environment.IsOutputRedirected)
                return await EditLinesAsync(context, original);
            var draft = new NoteDraft(original);
            var frame = new ConsoleFrame(output, screen, environment);
            var top = 0;
            var left = 0;
            var status = "";
            char? highSurrogate = null;
            while (true)
            {
                await frame.ResizeAsync(context);
                var capacity = frame.Height - 3;
                var contentWidth = frame.Width - 4;
                if (draft.Row < top) top = draft.Row;
                if (draft.Row >= top + capacity) top = draft.Row - capacity + 1;
                var caret = ConsoleCells.Width(draft.Lines[draft.Row][..draft.Column]);
                if (caret < left) left = caret;
                if (caret >= left + contentWidth) left = caret - contentWidth + 1;
                var rows = new List<string>
                {
                    "# Note editor — title on first row, body below",
                    "F2:save Esc:discard | Ctrl+S:save | Arrows/Home/End:move Enter:newline"
                };
                for (var i = top; i < top + capacity; i++)
                    rows.Add(i >= draft.Lines.Count ? "" : (i == 0 ? "T | " : "  | ") +
                        ConsoleCells.Slice(draft.Lines[i], left, contentWidth));
                rows.Add($"Row {draft.Row + 1}, column {caret + 1}  {status}");
                await frame.RenderAsync(context, rows, 4 + caret - left, 2 + draft.Row - top);
                var key = await keys.ReadKeyAsync(context);
                if (key.Key == ConsoleKey.Escape)
                {
                    await frame.ClearAsync(context);
                    return null;
                }
                if (key.Key == ConsoleKey.F2
                    || key.Key == ConsoleKey.S && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    context.Cancellation.GetToken().ThrowIfCancellationRequested();
                    await frame.ClearAsync(context);
                    return draft.ToNote();
                }
                status = "";
                var control = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                // ReadKey can deliver an emoji as two UTF-16 key characters; only insert the complete scalar.
                if (char.IsHighSurrogate(key.KeyChar)) { highSurrogate = key.KeyChar; continue; }
                if (char.IsLowSurrogate(key.KeyChar))
                {
                    if (highSurrogate.HasValue && !draft.Insert(new string([highSurrogate.Value, key.KeyChar])))
                        status = "Size limit reached.";
                    highSurrogate = null;
                    continue;
                }
                highSurrogate = null;
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow: draft.Left(); break;
                    case ConsoleKey.RightArrow: draft.Right(); break;
                    case ConsoleKey.UpArrow: draft.Vertical(-1); break;
                    case ConsoleKey.DownArrow: draft.Vertical(1); break;
                    case ConsoleKey.PageUp: draft.Vertical(-capacity); break;
                    case ConsoleKey.PageDown: draft.Vertical(capacity); break;
                    case ConsoleKey.Home: draft.Home(control); break;
                    case ConsoleKey.End: draft.End(control); break;
                    case ConsoleKey.Backspace: draft.Backspace(); break;
                    case ConsoleKey.Delete: draft.Delete(); break;
                    case ConsoleKey.Enter: if (!draft.NewLine()) status = "Size limit reached."; break;
                    case ConsoleKey.Tab: if (!draft.Insert("    ")) status = "Size limit reached."; break;
                    default:
                        if (!control && !char.IsControl(key.KeyChar) && !draft.Insert(key.KeyChar.ToString()))
                            status = "Size limit reached.";
                        break;
                }
            }
        }

        private async Task<NotepadData?> EditLinesAsync(NotepadContext context, NotepadData? oldNote)
        {
            if (oldNote != null)
                await WriteAsync(context, $"Current title: {oldNote.Title}\nCurrent body:\n{oldNote.Text}");
            var title = await ReadAsync(context, oldNote == null
                ? "Title (/cancel cancels):" : "New title (blank keeps current, /cancel cancels):");
            if (title == null || title == "/cancel") return null;
            if (title.Length == 0 && oldNote != null) title = oldNote.Title;
            if (title.Length > NotepadRules.MaxTitleLength)
            {
                await WriteAsync(context, "Title is too long (maximum 200 characters). Nothing saved.");
                return null;
            }
            await WriteAsync(context,
                "Body: '.' saves; '..' inserts '.'; /cancel discards. /keep on the first line keeps the old body.");
            var body = new StringBuilder();
            var first = true;
            while (true)
            {
                var line = await lines.ReadLineAsync(context);
                if (line == null) { context.ExitRequested = true; return null; }
                if (line == "/cancel") return null;
                if (first && line == "/keep" && oldNote != null) return new NotepadData(title, oldNote.Text);
                if (line == ".") return new NotepadData(title, body.ToString());
                if (line == "..") line = ".";
                if (!first) body.Append('\n');
                body.Append(line);
                first = false;
                if (body.Length > NotepadRules.MaxTextLength)
                {
                    await WriteAsync(context, "Body is too long (maximum 1 Mi characters). Nothing saved.");
                    return null;
                }
            }
        }

        private async Task WriteAsync(NotepadContext context, string text)
        {
            var end = await output.WriteLineAsync(context, text);
            if (end.HasException) throw end.Exception!;
        }

        private async Task<string?> ReadAsync(NotepadContext context, string prompt)
        {
            await WriteAsync(context, prompt);
            var text = await lines.ReadLineAsync(context);
            if (text == null) context.ExitRequested = true;
            return text;
        }
    }
}
