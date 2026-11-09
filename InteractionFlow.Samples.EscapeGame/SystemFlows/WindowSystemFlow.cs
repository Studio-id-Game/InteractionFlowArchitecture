using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class WindowSystemFlow(
    OpenWindow open,
    PresentFrame frame,
    ChangeControl control,
    RequestShutdown stop,
    CloseWindow close,
    GameSystemFlow game)
    : SystemFlow<EscapeApplicationContext>(open, frame, control, stop, close, game)
{
    protected override Task<InteractionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        using var frames = context.CreateIndependentContext();
        var input = new FrameState();
        frames.With(input).With(new WindowState());
        var failures = new FailureCollector();
        Task<InteractionEnd>? execution = null;

        try
        {
            var opened = open.ExecuteAsync(frames).GetAwaiter().GetResult();
            failures.Add(opened.Result);
            if (!opened.HasException)
            {
                execution = Task.Run(() => game.ExecuteAsync(context));
                PresentFrames(frames, input, execution, failures);
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            StopAndJoin(context, frames, execution, failures);
        }

        // 閉鎖処理まで呼び出し元スレッドで完了させます。ここで await しません。
        using var shutdown = new EscapeApplicationContext(frames);
        shutdown.With(new Finishing(FinishingKind.CloseWindow, failures.ToResult()));
        try
        {
            return Task.FromResult(close.ExecuteAsync(shutdown).GetAwaiter().GetResult());
        }
        catch (Exception error) when (failures.HasFailure)
        {
            failures.Add(error);
            throw failures.ToAggregate();
        }
    }

    private void PresentFrames(
        EscapeApplicationContext frames,
        FrameState input,
        Task<InteractionEnd> execution,
        FailureCollector failures)
    {
        while (!execution.IsCompleted && !frames.Lifetime.ShutdownRequested)
        {
            var shown = frame.ExecuteAsync(frames).GetAwaiter().GetResult();
            failures.Add(shown.Result);
            if (shown.HasException || input.ShutdownRequested)
                break;

            // ゲーム入力/提示待機から独立した制御経路です。通常入力は同フレームで配送しません。
            if (input.PauseRequested || input.SkipRequested)
            {
                var changed = control.ExecuteAsync(frames).GetAwaiter().GetResult();
                failures.Add(changed.Result);
                if (changed.HasException)
                    break;
            }
        }
    }

    private void StopAndJoin(
        EscapeApplicationContext context,
        EscapeApplicationContext frames,
        Task<InteractionEnd>? execution,
        FailureCollector failures)
    {
        // Stop は提示・入力・再開待機を解除します。失敗しても cancel/join/close を続けます。
        using var stopping = frames.CreateIndependentContext();
        try { failures.Add(stop.ExecuteAsync(stopping).GetAwaiter().GetResult().Result); }
        catch (Exception error) { failures.Add(error); }

        if (execution is null)
            return;

        try { if (!execution.IsCompleted) context.Cancellation.Cancel(); }
        catch (Exception error) { failures.Add(error); }

        try { failures.Add(execution.GetAwaiter().GetResult().Result); }
        catch (Exception error) { failures.Add(error); }
    }

}
