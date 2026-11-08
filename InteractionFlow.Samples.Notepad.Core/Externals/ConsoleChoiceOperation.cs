using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    public sealed class ConsoleChoiceOperation(IConsoleLineInputPort lines, IConsoleKeyInputPort keys,
        IConsoleOutputPort output, IConsoleScreenPort screen, IConsoleEnvironmentPort environment)
        : Operation(lines, keys, output, screen, environment), IConsoleChoiceOperation
    {
        public override void ForceResetMemoryState() { }

        public async Task<Result<ChoiceValue>> SelectAsync(NotepadContext context, string title, string[] choices, string[]? labels = null)
        {
            if (labels != null && labels.Length != choices.Length)
                throw new ArgumentException("Choice labels must match the number of choices.", nameof(labels));
            if (environment.IsInputRedirected || environment.IsOutputRedirected)
                return await SelectLineAsync(context, title, choices);
            if (choices.Length == 0) return new ChoiceValue(null);
            var frame = new ConsoleFrame(output, screen, environment);
            var selected = 0;
            while (true)
            {
                var resized = await frame.ResizeAsync(context);
                if (!resized.Try(out var resizeError)) return resizeError;
                var capacity = frame.Height - 3;
                var first = Math.Max(0, selected - capacity + 1);
                var rows = new List<string> { title, "Enter:OK Esc:cancel | Arrows:select", "" };
                for (var i = first; i < Math.Min(choices.Length, first + capacity); i++)
                    rows.Add($"{(i == selected ? ">" : " ")} [{i + 1}] {(labels == null ? choices[i] : labels[i])}");
                var rendered = await frame.RenderAsync(context, rows, 0, 3 + selected - first);
                if (!rendered.Try(out var renderError)) return renderError;
                var key = await keys.ReadKeyAsync(context);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: case ConsoleKey.LeftArrow:
                        selected = Math.Max(0, selected - 1); break;
                    case ConsoleKey.DownArrow: case ConsoleKey.RightArrow:
                        selected = Math.Min(choices.Length - 1, selected + 1); break;
                    case ConsoleKey.Home: selected = 0; break;
                    case ConsoleKey.End: selected = choices.Length - 1; break;
                    case ConsoleKey.PageUp: selected = Math.Max(0, selected - capacity); break;
                    case ConsoleKey.PageDown: selected = Math.Min(choices.Length - 1, selected + capacity); break;
                    case ConsoleKey.Enter:
                        return (await frame.ClearAsync(context)).Then(() => new ChoiceValue(selected).AsResult());
                    case ConsoleKey.Escape:
                        return (await frame.ClearAsync(context)).Then(() => new ChoiceValue(null).AsResult());
                    default:
                        var digit = key.KeyChar - '1';
                        if (key.KeyChar is >= '1' and <= '9' && digit < choices.Length) selected = digit;
                        break;
                }
            }
        }

        private async Task<Result<ChoiceValue>> SelectLineAsync(NotepadContext context, string title, string[] choices)
        {
            var menu = title + "\n";
            for (var i = 0; i < choices.Length; i++) menu += $"[{i + 1}] {choices[i]}\n";
            var end = await output.WriteLineAsync(context, menu + "Number or name (0/blank cancels):");
            if (!end.Result.Try(out var error)) return error;
            var text = await lines.ReadLineAsync(context);
            if (text == null) context.ExitRequested = true;
            if (string.IsNullOrWhiteSpace(text) || text.Trim() == "0") return new ChoiceValue(null);
            text = text.Trim();
            for (var i = 0; i < choices.Length; i++)
                if (text.Equals(choices[i], StringComparison.OrdinalIgnoreCase)) return new ChoiceValue(i);
            if (int.TryParse(text, out var number) && number > 0 && number <= choices.Length)
                return new ChoiceValue(number - 1);
            return new ChoiceValue(-1); // Invalid selection is distinct from an explicit cancellation.
        }
    }
}
