using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    internal sealed class ConsoleFrame(
        IConsoleOutputPort output, IConsoleScreenPort screen, IConsoleEnvironmentPort terminal)
    {
        private int width;
        private int height;
        public int Width => width;
        public int Height => height;

        public async Task<Result> ResizeAsync(IFlowContext context)
        {
            var size = terminal.GetWindowSize();
            if (!size.Try(out var dimensions, out var error)) return error;
            var (newWidth, newHeight) = dimensions;
            if (newWidth < 24 || newHeight < 8)
                return new InvalidOperationException("Resize the terminal to at least 24 columns and 8 rows.");
            newWidth--; // Do not trigger wrapping at the right edge.
            newHeight--; // Do not trigger scrolling at the bottom edge.
            if (newWidth == width && newHeight == height) return Result.Success;
            width = newWidth;
            height = newHeight;
            return await ClearAsync(context);
        }

        public async Task<Result> ClearAsync(IFlowContext context)
        {
            var end = await screen.ClearAsync(context);
            return end.Result;
        }

        public async Task<Result> RenderAsync(IFlowContext context, IReadOnlyList<string> rows, int left, int top)
        {
            var moved = await screen.SetCursorPositionAsync(context, new ConsolePosition(0, 0));
            if (!moved.Result.Try(out var moveError)) return moveError;
            var frame = new StringBuilder();
            for (var row = 0; row < height; row++)
            {
                if (row != 0) frame.Append(Environment.NewLine);
                frame.Append(ConsoleCells.Slice(row < rows.Count ? rows[row] : "", 0, width));
            }
            var written = await output.WriteAsync(context, frame.ToString());
            if (!written.Result.Try(out var writeError)) return writeError;
            moved = await screen.SetCursorPositionAsync(context,
                new ConsolePosition(Math.Max(0, Math.Min(width - 1, left)), Math.Max(0, Math.Min(height - 1, top))));
            return moved.Result;
        }
    }
}
