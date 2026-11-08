using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Externals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Notepad.Tests
{
    public sealed class InteractionBoundaryTests
    {
        [Fact]
        public async Task CancelDuringEditDiscardsDraftAndReleasesBinding()
        {
            var input = new ScriptedInput("note", "New title", "Unsaved text", "#cancel");
            var output = new RecordingOutput();
            var binding = new RecordingBinding();
            var notes = new MemoryNotes();
            var context = new NotepadContext { User = "alice" };
            var interaction = new NoteEdit(input, output, binding, new Errors(), new Cancellations(), notes,
                Editor(input, output), Choices(input, output));
            var end = await interaction.ExecuteAsync(context);
            Assert.False(end.HasException);
            Assert.False(context.Cancellation.IsCancellationRequested);
            Assert.Equal(0, notes.SaveCount);
            Assert.Equal("Original", notes.Note.Title);
            Assert.False(binding.Connected);
            Assert.Equal(1, binding.UnbindCount);
        }

        [Fact]
        public async Task CancelAtMenuRecordsExitEvenWhenCancellationWasHandledSuccessfully()
        {
            var context = new NotepadContext { User = "alice" };
            var binding = new RecordingBinding();
            var input = new ScriptedInput("#cancel");
            var output = new RecordingOutput();
            var menu = new SelectUserAction(output, binding,
                new Errors(), new Cancellations(), Choices(input, output));
            var end = await menu.ExecuteAsync(context);
            Assert.False(end.HasException);
            Assert.True(context.ExitRequested);
            Assert.False(binding.Connected);
        }

        [Fact]
        public async Task FailedPromptStopsBeforeReadingInputOrSavingAndReleasesBinding()
        {
            var input = new ScriptedInput("note", "New title");
            var failure = new IOException("Output unavailable");
            var output = new RecordingOutput { Failure = failure };
            var binding = new RecordingBinding();
            var notes = new MemoryNotes();
            var edit = new NoteEdit(input, output, binding, new Errors(), new Cancellations(), notes,
                Editor(input, output), Choices(input, output));
            var end = await edit.ExecuteAsync(new NotepadContext { User = "alice" });
            Assert.Same(failure, end.Exception);
            Assert.Equal(0, input.ReadCount);
            Assert.Equal(0, notes.SaveCount);
            Assert.False(binding.Connected);
        }

        [Fact]
        public async Task SaveFailureDoesNotReplaceTheLoadedValue()
        {
            var notes = new MemoryNotes { SaveFailure = new IOException("Disk full") };
            var output = new RecordingOutput();
            var input = new ScriptedInput("note", "New title", "New body", ".");
            var edit = new NoteEdit(input, output, new RecordingBinding(), new Errors(), new Cancellations(), notes,
                Editor(input, output), Choices(input, output));
            var end = await edit.ExecuteAsync(new NotepadContext { User = "alice" });
            Assert.False(end.HasException);
            Assert.Equal("Original", notes.Note.Title);
            Assert.Contains(output.Lines, text => text.Contains("Save failed"));
        }

        [Theory]
        [InlineData("Login")]
        [InlineData("SelectUserAction")]
        [InlineData("NoteCreate")]
        [InlineData("NoteEdit")]
        [InlineData("NoteDelete")]
        [InlineData("NoteListView")]
        public async Task MissingRequiredContextReachesExceptionPortBeforeInputOrBinding(string name)
        {
            var input = new ScriptedInput("ignored");
            var output = new RecordingOutput();
            var binding = new RecordingBinding();
            var notes = new MemoryNotes();
            IInteraction interaction = name switch
            {
                "Login" => new Login(output, binding, new Errors(), new Cancellations(), new UnusedLogin()),
                "SelectUserAction" => new SelectUserAction(output, binding,
                    new Errors(), new Cancellations(), Choices(input, output)),
                "NoteCreate" => new NoteCreate(input, output, binding, new Errors(), new Cancellations(), notes,
                    Editor(input, output), Choices(input, output)),
                "NoteEdit" => new NoteEdit(input, output, binding, new Errors(), new Cancellations(), notes,
                    Editor(input, output), Choices(input, output)),
                "NoteDelete" => new NoteDelete(output, binding,
                    new Errors(), new Cancellations(), notes, Choices(input, output)),
                "NoteListView" => new NoteListView(output, binding, new Errors(), new Cancellations(), notes,
                    new ConsoleKeyInput(), new RedirectedEnvironment()),
                _ => throw new ArgumentException("Unknown interaction", nameof(name))
            };

            var end = await interaction.ExecuteAsync(new FlowContext());

            var error = Assert.IsType<RequiredContextValueNotFoundException>(end.Exception);
            Assert.Equal(typeof(NotepadContext), error.RequiredType);
            Assert.Equal(name + ".cs", Path.GetFileName(error.FilePath));
            Assert.Equal("ExecuteCoreAsync", error.MemberName);
            Assert.Equal(0, input.ReadCount);
            Assert.Empty(output.Lines);
            Assert.False(binding.Connected);
            Assert.Equal(0, binding.UnbindCount);
        }

        [Fact]
        public async Task EofDuringCreateRequestsExitAndReleasesBindingWithoutSaving()
        {
            var input = new ScriptedInput();
            var output = new RecordingOutput();
            var binding = new RecordingBinding();
            var notes = new MemoryNotes();
            var context = new NotepadContext { User = "alice" };
            var create = new NoteCreate(input, output, binding, new Errors(), new Cancellations(), notes,
                Editor(input, output), Choices(input, output));

            var end = await create.ExecuteAsync(context);

            Assert.False(end.HasException);
            Assert.True(context.ExitRequested);
            Assert.Equal(0, notes.SaveCount);
            Assert.False(binding.Connected);
            Assert.Equal(1, binding.UnbindCount);
        }

        private sealed class UnusedLogin : Operation, INotepadLoginOperation
        {
            public override void ForceResetMemoryState() { }
            public Task<Result> LoginAsync(NotepadContext context)
                => throw new InvalidOperationException("Login must not run without the required context.");
        }

        private static ConsoleNoteEditor Editor(IConsoleLineInputPort input, IConsoleOutputPort output)
            => new(input, new ConsoleKeyInput(), output,
                new ConsoleScreen(), new RedirectedEnvironment());
        private static ConsoleChoiceOperation Choices(IConsoleLineInputPort input, IConsoleOutputPort output)
            => new(input, new ConsoleKeyInput(), output,
                new ConsoleScreen(), new RedirectedEnvironment());

        private sealed class RedirectedEnvironment : SilentExternal, IConsoleEnvironmentPort
        {
            public Result<(int Width, int Height)> GetWindowSize() => (80, 24);
            public bool IsInputRedirected => true;
            public bool IsOutputRedirected => true;
            public Result<ConsolePosition> GetCursorPosition() => new ConsolePosition(0, 0);
            public override void ForceResetMemoryState() { }
        }

        private sealed class ScriptedInput(params string[] lines) : Operation, IConsoleLineInputPort
        {
            private readonly Queue<string> queue = new(lines);
            public int ReadCount { get; private set; }
            public override void ForceResetMemoryState() { }
            public async ValueTask<string?> ReadLineAsync(IFlowContext context)
            {
                await Task.Yield();
                ReadCount++;
                if (queue.Count == 0) return null;
                var line = queue.Dequeue();
                if (line == "#cancel")
                {
                    context.Cancellation.Cancel();
                    context.Cancellation.GetToken().ThrowIfCancellationRequested();
                }
                return line;
            }
        }

        private sealed class RecordingOutput : Reaction, IConsoleOutputPort
        {
            public Exception? Failure { get; set; }
            public List<string> Lines { get; } = [];
            public override void ForceResetMemoryState() { }
            public ValueTask<ReactionEnd> WriteAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
            {
                Lines.Add(text);
                return new(GetEnd(Failure));
            }
            public ValueTask<ReactionEnd> WriteLineAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
                => WriteAsync(context, text, style);
        }

        private sealed class RecordingBinding : SilentExternal, IConsoleCancellationBindingPort
        {
            public bool Connected { get; private set; }
            public int UnbindCount { get; private set; }
            public IDisposable Bind(IFlowContext context)
            {
                Assert.False(Connected);
                Connected = true;
                return new BindingScope(this);
            }
            private sealed class BindingScope(RecordingBinding owner) : IDisposable
            {
                private bool disposed;
                public void Dispose()
                {
                    if (disposed) return;
                    owner.Connected = false;
                    owner.UnbindCount++;
                    disposed = true;
                }
            }
            public override void ForceResetMemoryState() => Connected = false;
        }

        private sealed class Errors : ExceptionHandling
        {
            public override void ForceResetMemoryState() { }
            protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(
                IFlowContext context, Exception exception)
                => new(GetEnd(exception));
        }

        private sealed class Cancellations : CancellationHandling
        {
            public override void ForceResetMemoryState() { }
            protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
                IFlowContext context, OperationCanceledException exception, Result waitAndResetResult)
                => new(GetEnd(waitAndResetResult.Try(out var error) ? null : error));
        }

        private sealed class MemoryNotes : INotepadDataPersistencePort
        {
            public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;
            public NotepadData Note { get; private set; } = new("Original", "Original body");
            public int SaveCount { get; private set; }
            public Exception? SaveFailure { get; set; }
            public Task<Result<NotepadData>> Load(NotepadKey id, Result<NotepadData> oldValue)
                => Note.AsResult().StartAsync();
            public Task<Result> Save(NotepadKey id, Result<NotepadData> value)
            {
                SaveCount++;
                if (SaveFailure != null) return ((Result)SaveFailure).StartAsync();
                return value.OnSuccess(note => Note = note).WithoutValue.StartAsync();
            }
            public Task<Result> Delete(NotepadKey id) => Result.Success.StartAsync();
            public Task<Result> Exists(NotepadKey id) => Result.Success.StartAsync();
            public Task<Result<NotepadKey[]>> GetAllIds()
                => new[] { new NotepadKey("alice", "note") }.AsResult().StartAsync();
        }
    }
}
