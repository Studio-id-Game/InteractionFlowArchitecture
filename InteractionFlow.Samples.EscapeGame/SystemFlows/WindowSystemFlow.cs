using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System;
using System.Collections.Generic;
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
        var failures = new List<Exception>();
        Task<InteractionEnd>? execution = null;

        try
        {
            var opened = open.ExecuteAsync(frames).GetAwaiter().GetResult();
            Add(opened.Result, failures);
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
        shutdown.With(new Finishing(FinishingKind.CloseWindow, ToResult(failures)));
        try
        {
            return Task.FromResult(close.ExecuteAsync(shutdown).GetAwaiter().GetResult());
        }
        catch (Exception error) when (failures.Count > 0)
        {
            failures.Add(error);
            throw new AggregateException(failures);
        }
    }

    private void PresentFrames(
        EscapeApplicationContext frames,
        FrameState input,
        Task<InteractionEnd> execution,
        List<Exception> failures)
    {
        while (!execution.IsCompleted && !frames.World.ShutdownRequested)
        {
            var shown = frame.ExecuteAsync(frames).GetAwaiter().GetResult();
            Add(shown.Result, failures);
            if (shown.HasException || input.ShutdownRequested)
                break;

            // ゲーム入力/提示待機から独立した制御経路です。通常入力は同フレームで配送しません。
            if (input.PauseRequested || input.SkipRequested)
            {
                var changed = control.ExecuteAsync(frames).GetAwaiter().GetResult();
                Add(changed.Result, failures);
                if (changed.HasException)
                    break;
            }
        }
    }

    private void StopAndJoin(
        EscapeApplicationContext context,
        EscapeApplicationContext frames,
        Task<InteractionEnd>? execution,
        List<Exception> failures)
    {
        // Stop は提示・入力・再開待機を解除します。失敗しても cancel/join/close を続けます。
        using var stopping = frames.CreateIndependentContext();
        try { Add(stop.ExecuteAsync(stopping).GetAwaiter().GetResult().Result, failures); }
        catch (Exception error) { failures.Add(error); }

        if (execution is null)
            return;

        try { if (!execution.IsCompleted) context.Cancellation.Cancel(); }
        catch (Exception error) { failures.Add(error); }

        try { Add(execution.GetAwaiter().GetResult().Result, failures); }
        catch (Exception error) { failures.Add(error); }
    }

    private static void Add(Result result, List<Exception> failures)
    {
        if (!result.Try(out var error))
            failures.Add(error);
    }

    private static Result ToResult(List<Exception> failures)
        => failures.Count switch
        {
            0 => Result.Success,
            1 => failures[0],
            _ => new AggregateException(failures)
        };
}
