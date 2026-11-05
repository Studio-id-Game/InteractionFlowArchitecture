using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Externals.Operations;
using InteractionFlow.Standard.ConsoleSystem.Externals.Reactions;
using InteractionFlow.Standard.ConsoleSystem.Externals.SilentExternals;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Terminal = System.Console;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace InteractionFlow.Standard.Tests.ConsoleSystem
{
    // 標準実装の結合テスト。公開の標準ストリームを設定し、必ず元へ戻す。
    public sealed class ConsoleIntegrationTests
    {
        [Fact]
        public async Task LineReadsTextEmptyLineAndEof()
        {
            var original = Terminal.In;
            using var reader = new StringReader("hello\n\n");
            Terminal.SetIn(reader);
            try
            {
                var input = new ConsoleLineInput();
                var context = new FlowContext();
                Assert.Equal("hello", await input.ReadLineAsync(context));
                Assert.Equal("", await input.ReadLineAsync(context));
                Assert.Null(await input.ReadLineAsync(context));
            }
            finally
            {
                Terminal.SetIn(original);
            }
        }

        [Fact]
        public async Task PreCanceledInputDoesNotConsumeLine()
        {
            var original = Terminal.In;
            var originalError = Terminal.Error;
            using var reader = new StringReader("next\n");
            using var notices = new StringWriter();
            Terminal.SetIn(reader);
            Terminal.SetError(notices);
            try
            {
                var context = new FlowContext();
                context.Cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => new ConsoleLineInput().ReadLineAsync(context).AsTask());
                Assert.Equal("next", await new ConsoleLineInput().ReadLineAsync(new FlowContext()));
                Assert.Equal("", notices.ToString());
            }
            finally
            {
                Terminal.SetIn(original);
                Terminal.SetError(originalError);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task PendingLineShowsOneNoticeAndStillDrainsRead(bool failNotice, bool failRead)
        {
            var originalInput = Terminal.In;
            var originalError = Terminal.Error;
            using var reader = new PendingLineReader(failRead);
            using var notices = new CancellationNoticeWriter(failNotice);
            Terminal.SetIn(reader);
            Terminal.SetError(notices);
            var context = new FlowContext();
            var task = new ConsoleLineInput(TimeSpan.Zero).ReadLineAsync(context).AsTask();
            try
            {
                await reader.Started.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                context.Cancellation.Cancel();
                await notices.Written.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

                Assert.False(task.IsCompleted);
                context.Cancellation.Cancel();
                Assert.Equal(1, notices.NoticeCount);
                reader.Release();

                if (failNotice)
                {
                    var failure = await Assert.ThrowsAsync<AggregateException>(() => task.WaitAsync(
                        TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
                    Assert.Contains(failure.Flatten().InnerExceptions,
                        exception => exception is IOException && exception.Message == "notice failed");
                    if (failRead)
                        Assert.Contains(failure.Flatten().InnerExceptions,
                            exception => exception is IOException && exception.Message == "read failed");
                }
                else if (failRead)
                {
                    await Assert.ThrowsAsync<IOException>(() => task);
                }
                else
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
                }

                if (!failNotice)
                {
                    var expected = Terminal.IsInputRedirected
                        ? "Waiting for the current input to finish"
                        : "Press Enter to cancel input";
                    Assert.Contains(expected, notices.ToString());
                }
            }
            finally
            {
                reader.Release();
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (IOException) { }
                catch (AggregateException) { }
                finally
                {
                    Terminal.SetIn(originalInput);
                    Terminal.SetError(originalError);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CanceledLineDrainsReadAndReleasesInput(bool readFails)
        {
            var original = Terminal.In;
            using var reader = new PendingLineReader(readFails);
            Terminal.SetIn(reader);
            var context = new FlowContext();
            // 入力中のキャンセル・読み取り失敗では、この長い猶予を追加で待たない。
            var task = new ConsoleLineInput(TimeSpan.FromMinutes(1)).ReadLineAsync(context).AsTask();
            try
            {
                await reader.Started.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                context.Cancellation.Cancel();
                Assert.False(task.IsCompleted);
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => new ConsoleKeyInput().ReadKeyAsync(new FlowContext()).AsTask());

                reader.Release();
                if (readFails)
                    await Assert.ThrowsAsync<IOException>(() => task.WaitAsync(
                        TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
                else
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(
                        TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

                // 読み取り完了後に占有が解放され、次の入力を取得できる。
                using var next = new StringReader("next\n");
                Terminal.SetIn(next);
                Assert.Equal("next", await new ConsoleLineInput().ReadLineAsync(new FlowContext()));
            }
            finally
            {
                reader.Release();
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (IOException) { }
                finally
                {
                    Terminal.SetIn(original);
                }
            }
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("")]
        [InlineData(null)]
        public async Task CancellationAfterReadIsAcceptedDuringGracePeriod(string? line)
        {
            var original = Terminal.In;
            var originalError = Terminal.Error;
            using var reader = new CompletedLineReader(line);
            using var notices = new StringWriter();
            Terminal.SetIn(reader);
            Terminal.SetError(notices);
            var context = new FlowContext();
            var input = new ConsoleLineInput(TimeSpan.FromSeconds(2));
            var task = input.ReadLineAsync(context).AsTask();
            try
            {
                await reader.Returning.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                // 公開の標準入力が戻った後、遅れてキャンセル通知が届く条件を再現する。
                await Task.Delay(100, TestContext.Current.CancellationToken);
                Assert.False(task.IsCompleted);
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => new ConsoleKeyInput().ReadKeyAsync(new FlowContext()).AsTask());

                context.Cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(
                    TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
                Assert.Equal("", notices.ToString());
            }
            finally
            {
                context.Cancellation.Cancel();
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    Terminal.SetIn(original);
                    Terminal.SetError(originalError);
                }
            }
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("")]
        [InlineData(null)]
        public async Task SuccessfulReadRemainsCancelableUntilGraceHasElapsed(string? line)
        {
            var original = Terminal.In;
            using var reader = new CompletedLineReader(line);
            Terminal.SetIn(reader);
            var input = new ConsoleLineInput(TimeSpan.FromMilliseconds(200));
            var task = input.ReadLineAsync(new FlowContext()).AsTask();
            try
            {
                await reader.Returning.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.False(task.IsCompleted);
                Assert.Equal(line, await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            }
            finally
            {
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                }
                finally
                {
                    Terminal.SetIn(original);
                }
            }
        }

        [Fact]
        public async Task CancellationAfterCompletionIsRetainedForNextOperation()
        {
            var original = Terminal.In;
            using var reader = new StringReader("first\nnext\n");
            Terminal.SetIn(reader);
            try
            {
                var context = new FlowContext();
                var input = new ConsoleLineInput(TimeSpan.Zero);
                Assert.Equal("first", await input.ReadLineAsync(context));
                context.Cancellation.Cancel();
                Assert.True(context.Cancellation.IsCancellationRequested);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => input.ReadLineAsync(context).AsTask());
                var reset = await context.Cancellation.WaitAndResetAsync();
                Assert.True(reset.Try(out _));
                Assert.Equal("next", await input.ReadLineAsync(context));
            }
            finally
            {
                Terminal.SetIn(original);
            }
        }

        [Fact]
        public void GracePeriodHasFiniteNonNegativeBounds()
        {
            Assert.Equal(TimeSpan.FromMilliseconds(100), new ConsoleLineInput().CancellationGracePeriod);
            Assert.Equal(TimeSpan.Zero, new ConsoleLineInput(TimeSpan.Zero).CancellationGracePeriod);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleLineInput(TimeSpan.FromMilliseconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleLineInput(TimeSpan.MaxValue));
        }

        [Fact]
        public async Task WritesPreserveOrderAndContinueAfterCancellation()
        {
            var original = Terminal.Out;
            using var writer = new StringWriter();
            Terminal.SetOut(writer);
            try
            {
                var context = new FlowContext();
                context.Cancellation.Cancel();
                var output = new ConsoleOutput();
                Assert.False((await output.WriteAsync(context, "first")).HasException);
                Assert.False((await output.WriteLineAsync(context, "second")).HasException);
                Assert.Equal("firstsecond" + writer.NewLine, writer.ToString());
            }
            finally
            {
                Terminal.SetOut(original);
            }
        }

        [Fact]
        public async Task ConcurrentOutputProducesWholeLines()
        {
            var original = Terminal.Out;
            using var writer = new StringWriter();
            Terminal.SetOut(writer);
            try
            {
                await Task.WhenAll(Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
                {
                    var end = await new ConsoleOutput().WriteLineAsync(new FlowContext(), $"line-{i}");
                    Assert.False(end.HasException);
                }, TestContext.Current.CancellationToken)));

                var lines = writer.ToString().Split(writer.NewLine, StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(40, lines.Distinct().Count());
                Assert.All(lines, line => Assert.StartsWith("line-", line));
            }
            finally
            {
                Terminal.SetOut(original);
            }
        }

        [Fact]
        public async Task WriteFailureBecomesReactionFailure()
        {
            var original = Terminal.Out;
            using var writer = new FailingWriter();
            Terminal.SetOut(writer);
            try
            {
                var end = await new ConsoleOutput().WriteAsync(new FlowContext(), "text");
                Assert.IsType<IOException>(end.Exception);
            }
            finally
            {
                Terminal.SetOut(original);
            }
        }

        [Fact]
        public async Task NotificationRetainsOriginalAndOutputFailures()
        {
            var original = Terminal.Error;
            using var writer = new FailingWriter();
            Terminal.SetError(writer);
            try
            {
                var error = new InvalidOperationException("original");
                var handler = new ConsoleExceptionHandling();
                var end = await handler.HandleExceptionAsync(new FlowContext(), error);
                Assert.Contains(error, Assert.IsType<AggregateException>(end.Exception).Flatten().InnerExceptions);

                handler.ThrowException = true;
                Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(
                    () => handler.HandleExceptionAsync(new FlowContext(), error).AsTask()));

                var context = new FlowContext();
                context.Cancellation.AddCancelableTask(Task.FromException(new IOException("task failed")));
                context.Cancellation.Cancel();
                end = await new ConsoleCancellationHandling()
                    .HandleCancellationAsync(context, new OperationCanceledException());
                Assert.Contains("task failed", end.Exception!.ToString());
                Assert.Contains("write failed", end.Exception.ToString());
            }
            finally
            {
                Terminal.SetError(original);
            }
        }

        [Fact]
        public async Task SuccessfulCancellationResetsContextAndNotifies()
        {
            var original = Terminal.Error;
            using var writer = new StringWriter();
            Terminal.SetError(writer);
            try
            {
                var context = new FlowContext();
                context.Cancellation.Cancel();
                var end = await new ConsoleCancellationHandling()
                    .HandleCancellationAsync(context, new OperationCanceledException());
                Assert.False(end.HasException);
                Assert.False(context.Cancellation.IsCancellationRequested);
                Assert.Equal("Operation canceled." + writer.NewLine, writer.ToString());
            }
            finally
            {
                Terminal.SetError(original);
            }
        }

        [Fact]
        public void BindingSupportsExclusiveOwnershipResetAndDispose()
        {
            using var first = new ConsoleCancellationBinding();
            using var second = new ConsoleCancellationBinding();
            var context = new FlowContext();
            first.Bind(context);
            first.Bind(context);
            Assert.Throws<InvalidOperationException>(() => first.Bind(new FlowContext()));
            Assert.Throws<InvalidOperationException>(() => second.Bind(context));
            first.ForceResetMemoryState();
            second.Bind(context);
            second.Dispose();
            second.Unbind();
            Assert.Throws<ObjectDisposedException>(() => second.Bind(context));
            first.Bind(new FlowContext());
        }

        [Fact]
        public void ProfileIsPassiveAndScopeDisposesBinding()
        {
            var builder = new ScopeBuilder();
            builder.Apply(ConsoleProfile.Default);
            using var scope = builder.BuildScope();
            var input = Assert.IsType<ConsoleLineInput>(scope.GetService(typeof(IConsoleLineInputPort)));
            Assert.Equal(TimeSpan.FromMilliseconds(100), input.CancellationGracePeriod);
            var binding = Assert.IsType<ConsoleCancellationBinding>(
                scope.GetService(typeof(IConsoleCancellationBindingPort)));
            using (var other = new ConsoleCancellationBinding()) other.Bind(new FlowContext());

            binding.Bind(new FlowContext());
            try
            {
                scope.Dispose();
                Assert.Throws<ObjectDisposedException>(() => binding.Bind(new FlowContext()));
            }
            finally
            {
                binding.Dispose();
            }

            using var next = new ConsoleCancellationBinding();
            next.Bind(new FlowContext());
        }

        [Fact]
        public void ValuesRejectInvalidCoordinatesAndColors()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsolePosition(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleTextStyle((ConsoleColor)16));
            Assert.Null(default(ConsoleTextStyle).Foreground);
        }

        // 公開の標準入力に接続する Reader。Console 実装の内部型には依存しない。
        private sealed class CompletedLineReader(string? line) : StringReader("")
        {
            private readonly TaskCompletionSource returning = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task Returning => returning.Task;

            public override string? ReadLine()
            {
                returning.TrySetResult();
                return line;
            }
        }

        private sealed class PendingLineReader(bool fail) : StringReader("")
        {
            private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly ManualResetEventSlim released = new();

            public Task Started => started.Task;
            public void Release() => released.Set();

            public override string? ReadLine()
            {
                started.TrySetResult();
                if (!released.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
                    throw new TimeoutException();
                if (fail) throw new IOException("read failed");
                return "discarded";
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) released.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class FailingWriter : StringWriter
        {
            public override void Write(string? value) => throw new IOException("write failed");
            public override void WriteLine(string? value) => throw new IOException("write failed");
        }

        private sealed class CancellationNoticeWriter(bool fail) : StringWriter
        {
            private readonly TaskCompletionSource written = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int noticeCount;

            public Task Written => written.Task;
            public int NoticeCount => Volatile.Read(ref noticeCount);

            public override void WriteLine(string? value)
            {
                if (value?.StartsWith("Cancellation requested.", StringComparison.Ordinal) == true)
                {
                    Interlocked.Increment(ref noticeCount);
                    written.TrySetResult();
                    if (fail) throw new IOException("notice failed");
                }

                base.WriteLine(value);
            }
        }
    }
}
