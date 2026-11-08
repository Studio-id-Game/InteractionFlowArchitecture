using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Externals;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Externals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Notepad.Tests
{
    public sealed class CursorInputTests
    {
        [Fact]
        public void SplittingAndJoiningBodyLinesDoesNotMergeTheTitle()
        {
            var draft = new NoteDraft(new NotepadData("Title", "abc\ndef"));
            draft.NewLine(); // From title to the first body line.
            draft.Right(); draft.NewLine();
            Assert.Equal("a\nbc\ndef", draft.ToNote().Text);
            draft.Backspace();
            Assert.Equal("abc\ndef", draft.ToNote().Text);
            draft.Home(); draft.Backspace();
            Assert.Equal("Title", draft.ToNote().Title);
            Assert.Equal("abc\ndef", draft.ToNote().Text);
            draft.End(); draft.Delete();
            Assert.Equal("abcdef", draft.ToNote().Text);
        }

        [Fact]
        public void GraphemeNavigationAndDeletionKeepJapaneseEmojiAndCombiningTextWhole()
        {
            var draft = new NoteDraft(new NotepadData("日本🐦e\u0301", ""));
            draft.End(); draft.Left();
            Assert.Equal(4, draft.Column);
            draft.Backspace();
            Assert.Equal("日本e\u0301", draft.ToNote().Title);
            draft.Delete();
            Assert.Equal("日本", draft.ToNote().Title);
            draft.Home(); draft.Right(); draft.Delete();
            Assert.Equal("日", draft.ToNote().Title);
        }

        [Fact]
        public void VerticalNavigationPreservesDisplayColumnAcrossCjkAndShortLines()
        {
            var draft = new NoteDraft(new NotepadData("日a", "abcdef\nx\nabcdef"));
            draft.End(); // Three display cells, two UTF-16 characters.
            draft.Vertical(1);
            Assert.Equal(3, draft.Column);
            draft.Vertical(1);
            Assert.Equal(1, draft.Column);
            draft.Vertical(1);
            Assert.Equal(3, draft.Column);
        }

        [Fact]
        public void TitleAndBodySizeLimitsKeepTheDraftUnchangedWhenExceeded()
        {
            var draft = new NoteDraft(new NotepadData(new string('t', 200),
                new string('b', NotepadRules.MaxTextLength)));
            Assert.False(draft.Insert("x"));
            draft.NewLine();
            Assert.False(draft.Insert("x"));
            Assert.False(draft.NewLine());
            Assert.Equal(NotepadRules.MaxTextLength, draft.ToNote().Text.Length);
        }

        [Fact]
        public async Task ArrowSelectionUsesKeysAndEscDoesNotRequestApplicationExit()
        {
            var io = new Terminal(ConsoleKey.DownArrow, ConsoleKey.Enter);
            var operation = new ConsoleChoiceOperation(io, io, io, io, io);
            var context = new NotepadContext();
            Assert.Equal(1, await operation.SelectAsync(context, "Choose", ["First", "Second"],
                ["First: first title", "Second: second title"]));
            Assert.Equal(0, io.LineReadCount);
            Assert.Contains(io.Frames, text => text.Contains("> [2] Second: second title"));
            Assert.True(io.ClearCount >= 2);
            io.Keys.Enqueue(Key(ConsoleKey.Escape));
            Assert.Null(await operation.SelectAsync(context, "Choose", ["First"]));
            Assert.False(context.ExitRequested);
        }

        [Fact]
        public async Task CursorEditorModifiesPreloadedTextAndSavesWithF2()
        {
            var io = new Terminal(ConsoleKey.End, ConsoleKey.Backspace);
            io.Keys.Enqueue(new ConsoleKeyInfo('!', ConsoleKey.D1, false, false, false));
            io.Keys.Enqueue(Key(ConsoleKey.DownArrow));
            io.Keys.Enqueue(Key(ConsoleKey.Home));
            io.Keys.Enqueue(Key(ConsoleKey.Delete));
            io.Keys.Enqueue(Key(ConsoleKey.Enter));
            io.Keys.Enqueue(Key(ConsoleKey.F2));
            var editor = new ConsoleNoteEditor(io, io, io, io, io);
            var note = await editor.EditAsync(new NotepadContext(), new NotepadData("Title", "Body"));
            Assert.NotNull(note);
            Assert.Equal("Titl!", note.Title);
            Assert.Equal("\nody", note.Text);
            Assert.Equal(0, io.LineReadCount);
            Assert.Contains(io.Frames, frame => frame.Contains("T | Title"));
        }

        [Fact]
        public async Task EscapeDiscardsDraftAndDoesNotMutateOriginal()
        {
            var io = new Terminal();
            io.Keys.Enqueue(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
            io.Keys.Enqueue(Key(ConsoleKey.Escape));
            var original = new NotepadData("Original", "Body");
            var editor = new ConsoleNoteEditor(io, io, io, io, io);
            Assert.Null(await editor.EditAsync(new NotepadContext(), original));
            Assert.Equal("Original", original.Title);
        }

        [Fact]
        public async Task CjkAndEmojiCursorUsesDisplayCellsAndSupportsSurrogateKeyPairs()
        {
            var io = new Terminal(ConsoleKey.End);
            io.Keys.Enqueue(new ConsoleKeyInfo('\ud83d', ConsoleKey.NoName, false, false, false));
            io.Keys.Enqueue(new ConsoleKeyInfo('\udc26', ConsoleKey.NoName, false, false, false));
            io.Keys.Enqueue(Key(ConsoleKey.F2));
            var editor = new ConsoleNoteEditor(io, io, io, io, io);
            var note = await editor.EditAsync(new NotepadContext(), new NotepadData("日", ""));
            Assert.Equal("日🐦", note!.Title);
            Assert.Contains(io.Positions, position => position.Left == 8 && position.Top == 2);
        }

        [Fact]
        public async Task ScrollingKeepsCursorInTheViewportAndCtrlSSaves()
        {
            var io = new Terminal(ConsoleKey.End);
            io.Keys.Clear();
            io.Keys.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, true));
            io.Keys.Enqueue(new ConsoleKeyInfo('s', ConsoleKey.S, false, false, true));
            var editor = new ConsoleNoteEditor(io, io, io, io, io);
            var original = new NotepadData("Title",
                string.Join("\n", Enumerable.Range(0, 100).Select(_ => new string('x', 200))));
            var result = await editor.EditAsync(new NotepadContext(), original);
            Assert.Equal(original.Text, result!.Text);
            Assert.All(io.Positions, position =>
            {
                Assert.InRange(position.Left, 0, 78);
                Assert.InRange(position.Top, 0, 22);
            });
        }

        [Fact]
        public async Task ScreenFailureStopsBeforeAnyKeyIsRead()
        {
            var failure = new IOException("Screen unavailable");
            var io = new Terminal { ScreenFailure = failure };
            var editor = new ConsoleNoteEditor(io, io, io, io, io);
            var actual = await Assert.ThrowsAsync<IOException>(() => editor.EditAsync(new NotepadContext(), null));
            Assert.Same(failure, actual);
            Assert.Equal(0, io.KeyReadCount);
        }

        [Fact]
        public async Task ListRemainsVisibleUntilAKeyIsPressed()
        {
            var io = new Terminal(ConsoleKey.Enter);
            using var binding = new ConsoleCancellationBinding();
            var list = new NoteListView(io, binding, new ConsoleExceptionHandling(),
                new ConsoleCancellationHandling(), new EmptyNoteFiles(), io, io);
            var end = await list.ExecuteAsync(new NotepadContext { User = "alice" });
            Assert.False(end.HasException);
            Assert.Equal(1, io.KeyReadCount);
            Assert.Contains(io.Frames, text => text.Contains("Press any key to return"));
        }

        private sealed class EmptyNoteFiles() : NotepadDataFilePersistence(new NotepadDataSimpleSerializer())
        {
            private readonly string root = Path.Combine(Path.GetTempPath(), "Notepad-empty-" + Guid.NewGuid());
            public override string GetCurrentRootPath() => root;
        }

        private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

        private sealed class Terminal(params ConsoleKey[] keys) : Reaction,
            IConsoleLineInputPort, IConsoleKeyInputPort, IConsoleOutputPort, IConsoleScreenPort, IConsoleEnvironmentPort
        {
            public Queue<ConsoleKeyInfo> Keys { get; } = new(keys.Select(Key));
            public List<string> Frames { get; } = [];
            public List<ConsolePosition> Positions { get; } = [];
            public int LineReadCount { get; private set; }
            public int KeyReadCount { get; private set; }
            public int ClearCount { get; private set; }
            public Exception? ScreenFailure { get; set; }
            public Result<(int Width, int Height)> GetWindowSize() => (80, 24);
            public bool IsInputRedirected => false;
            public bool IsOutputRedirected => false;
            public Result<ConsolePosition> GetCursorPosition() => new ConsolePosition(0, 0);
            public override void ForceResetMemoryState() { }
            public ValueTask<string?> ReadLineAsync(IFlowContext context)
            {
                LineReadCount++;
                throw new InvalidOperationException("Interactive mode must not read lines.");
            }
            public ValueTask<ConsoleKeyInfo> ReadKeyAsync(IFlowContext context)
            {
                KeyReadCount++;
                if (Keys.Count == 0) throw new InvalidOperationException("No scripted key remains.");
                return new(Keys.Dequeue());
            }
            public ValueTask<ReactionEnd> WriteAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
            {
                Frames.Add(text);
                return new(GetEnd());
            }
            public ValueTask<ReactionEnd> WriteLineAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
                => WriteAsync(context, text, style);
            public ValueTask<ReactionEnd> ClearAsync(IFlowContext context)
            {
                ClearCount++;
                return new(GetEnd(ScreenFailure));
            }
            public ValueTask<ReactionEnd> SetCursorPositionAsync(IFlowContext context, ConsolePosition position)
            {
                Positions.Add(position);
                return new(GetEnd(ScreenFailure));
            }
        }
    }
}
