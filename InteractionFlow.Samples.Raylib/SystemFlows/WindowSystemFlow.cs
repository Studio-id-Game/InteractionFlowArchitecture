using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Interactions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.SystemFlows;

public sealed class WindowSystemFlow(
    OpenWindow open,
    PresentFrame frame,
    CloseWindow close,
    CounterSystemFlow counter)
    : SystemFlow<CounterFlowContext>(open, frame, close, counter)
{
    // async/await でスレッドを移さず、Open〜Close を呼び出し元のメインスレッドで実行します。
    protected override Task<InteractionEnd> ExecuteCoreAsync(CounterFlowContext context)
    {
        var state = context.State;
        var frames = new FrameState();

        // 親あり Context は Cancellation を共有するため、独立したルートを用います。
        using var frameContext = context.CreateIndependentContext();
        frameContext.With(frames)
            .With(new WindowState());

        var failures = new List<Exception>();

        RunSession(context, frameContext, state, frames, failures);

        using var shutdown = new CounterFlowContext(frameContext);

        return Task.FromResult(CloseWindow(shutdown, failures));
    }

    private void RunSession(
        CounterFlowContext context,
        CounterFlowContext frameContext,
        ICounterStateAccess state,
        FrameState frames,
        List<Exception> failures)
    {
        Task<InteractionEnd>? execution = null;

        try
        {
            var opened = open.ExecuteAsync(frameContext).GetAwaiter().GetResult();
            AddFailure(opened.Result, failures);

            if (!opened.HasException)
            {
                execution = Task.Run(() => counter.ExecuteAsync(context));
                PresentFrames(context, frameContext, frames, execution, failures);
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            StopCounter(context, state, execution, failures);
        }
    }

    private void PresentFrames(
        CounterFlowContext context,
        CounterFlowContext frameContext,
        FrameState frames,
        Task<InteractionEnd> execution,
        List<Exception> failures)
    {
        while (!execution.IsCompleted)
        {
            var presented = frame.ExecuteAsync(frameContext).GetAwaiter().GetResult();
            AddFailure(presented.Result, failures);

            if (presented.HasException || frames.CloseRequested)
                break;

            if (frames.CancelRequested)
                context.Cancellation.Cancel();
        }
    }

    private static void StopCounter(
        CounterFlowContext context,
        ICounterStateAccess state,
        Task<InteractionEnd>? execution,
        List<Exception> failures)
    {
        // 異常終了も含む寿命の終了意図。キャンセルのリセットとは独立して保持します。
        state.RequestExit();

        if (execution is null)
            return;

        try
        {
            if (!execution.IsCompleted)
                context.Cancellation.Cancel();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }

        // worker はフレームの完了を待たないため、循環待機にはなりません。
        try
        {
            AddFailure(execution.GetAwaiter().GetResult().Result, failures);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private InteractionEnd CloseWindow(CounterFlowContext context, List<Exception> failures)
    {
        Result outcome = failures.Count switch
        {
            0 => Result.Success,
            1 => failures[0],
            _ => new AggregateException(failures)
        };

        context.With(outcome);

        try
        {
            return close.ExecuteAsync(context).GetAwaiter().GetResult();
        }
        catch (Exception error) when (failures.Count > 0)
        {
            failures.Add(error);
            throw new AggregateException(failures);
        }
    }

    private static void AddFailure(Result result, List<Exception> failures)
    {
        if (!result.Try(out var error))
            failures.Add(error);
    }
}
