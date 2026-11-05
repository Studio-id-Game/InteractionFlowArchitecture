using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>標準の行編集を保持し、読み取り後にもキャンセル通知の猶予を設ける入力実装です。</summary>
    public sealed class ConsoleLineInput : ConsoleInputBase, IConsoleLineInputPort
    {
        /// <summary>読み取り後に 100ms のキャンセル受付猶予を設けます。</summary>
        public ConsoleLineInput() : this(TimeSpan.FromMilliseconds(100))
        {
        }

        /// <summary>読み取り後のキャンセル受付猶予を指定します。ゼロなら追加待機しません。</summary>
        /// <param name="cancellationGracePeriod">ゼロ以上、Int32.MaxValue ミリ秒以下の期間。</param>
        public ConsoleLineInput(TimeSpan cancellationGracePeriod)
        {
            if (cancellationGracePeriod < TimeSpan.Zero ||
                cancellationGracePeriod.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(cancellationGracePeriod));
            }

            CancellationGracePeriod = cancellationGracePeriod;
        }

        /// <summary>読み取り成功後、正常完了を確定する前にキャンセル通知を待つ期間。</summary>
        /// <remarks>端末イベントの最大通知遅延を保証する値ではありません。</remarks>
        public TimeSpan CancellationGracePeriod { get; }

        /// <inheritdoc/>
        public ValueTask<string?> ReadLineAsync(IFlowContext context) => ReadAsync(context, ReadLineCoreAsync);

        private async ValueTask<string?> ReadLineCoreAsync(CancellationToken token)
        {
            var progress = new ReadProgress();
            var cancellationRequested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = token.Register(() => cancellationRequested.TrySetResult(true));
            var readTask = Task.Run(() => progress.ReadLine(token));

            await Task.WhenAny(readTask, cancellationRequested.Task).ConfigureAwait(false);

            // トークンのコールバック内では表示しない。要求側のスレッドは通知だけで返す。
            var noticeFailure = cancellationRequested.Task.IsCompleted
                ? progress.ShowCancellationNotice()
                : null;

            string? text;
            try
            {
                // 案内表示が失敗しても、開始済みの読み取りを残して戻らない。
                text = await readTask.ConfigureAwait(false);
            }
            catch (Exception readFailure) when (noticeFailure != null)
            {
                throw new AggregateException(readFailure, noticeFailure);
            }

            if (noticeFailure != null)
                throw new AggregateException(new OperationCanceledException(token), noticeFailure);

            // ReadLine の戻りと CancelKeyPress の到着順は一致するとは限らない。
            // キャンセル済みならここで終了し、追加の猶予待機は行わない。
            token.ThrowIfCancellationRequested();
            await Task.Delay(CancellationGracePeriod, token).ConfigureAwait(false);

            // この確認を正常完了の確定点とする。以降の要求も Context 側には残る。
            token.ThrowIfCancellationRequested();
            return text;
        }

        private sealed class ReadProgress
        {
            private readonly object gate = new();
            private bool reading;

            internal string? ReadLine(CancellationToken token)
            {
                lock (gate)
                {
                    token.ThrowIfCancellationRequested();
                    reading = true;
                }

                try
                {
                    return Console.ReadLine();
                }
                finally
                {
                    lock (gate)
                    {
                        reading = false;
                    }
                }
            }

            internal Exception? ShowCancellationNotice()
            {
                lock (gate)
                {
                    // 読み取り前・完了後・受付猶予中には Enter を求めない。
                    if (!reading) return null;

                    try
                    {
                        lock (ConsoleSynchronization.OutputGate)
                        {
                            var message = Console.IsInputRedirected
                                ? "Cancellation requested. Waiting for the current input to finish..."
                                : "Cancellation requested. Press Enter to cancel input.";
                            Console.Error.WriteLine();
                            Console.Error.WriteLine(message);
                        }
                        return null;
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                }
            }
        }
    }
}
