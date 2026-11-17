using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class WindowSystemFlow(
    OpenWindow openWindow,
    PresentFrame presentFrame,
    HandleControlInput controlInput,
    RequestShutdown requestShutdown,
    CloseWindow closeWindow,
    GamePlaySystemFlow gamePlaySystem)
    : SystemFlow<IApplicationContext>(openWindow, presentFrame, controlInput, requestShutdown, closeWindow, gamePlaySystem)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IApplicationContext context)
    {
        var failures = new FailureCollector();
        var frameState = new FrameState();
        using var windowContext = new ViewContext(context, ContextInheritance.None)
            .WithT(frameState)
            .WithT(new WindowState());
        using var gameContext = new GameplayContext(context, ContextInheritance.Cancellation);
        Task<InteractionEnd>? gameTask = null;

        // 停止要求は提示・入力・再開待機を解除します。途中で失敗しても合流と Close を続けます。
        await RunWindowAsync()
            .ThenErrorAsync(failures.RecordAndContinue)
            .ThenAsync(RequestStopAsync)
            .ThenErrorAsync(failures.RecordAndContinue)
            .ThenAsync(CancelGame)
            .ThenErrorAsync(failures.RecordAndContinue)
            .ThenAsync(JoinGame)
            .ThenErrorAsync(failures.RecordAndContinue);

        var previous = failures.ToResult();
        try
        {
            using var closeContext = new ViewContext(windowContext, ContextInheritance.All)
                .WithT(new CleanupInfo(CleanupKind.CloseWindow, previous));
            return await closeWindow.ExecuteAsync(closeContext);
        }
        catch (Exception error) when (!previous.IsSuccess)
        {
            failures.Add(error);
            throw failures.ToAggregate();
        }

        async Task<Result> RunWindowAsync()
        {
            try
            {
                var opened = await openWindow.ExecuteAsync(windowContext);
                return await opened.Result.ThenAsync(RunFramesAsync);
            }
            catch (Exception error)
            {
                return failures.RecordAndContinue(error);
            }
        }

        async Task<Result> RunFramesAsync()
        {
            var running = gamePlaySystem.ExecuteAsync(gameContext);
            gameTask = running;
            using var controlContext = new GameplayContext(context, ContextInheritance.None);

            while (!running.IsCompleted && !windowContext.Lifetime.ShutdownRequested)
            {
                var frame = await presentFrame.ExecuteAsync(windowContext);
                if (frame.HasException || frameState.ShutdownRequested)
                    return frame.Result;

                if (frameState.PauseRequested || frameState.SkipRequested)
                {
                    var control = await controlInput.ExecuteAsync(controlContext);
                    if (control.HasException)
                        return control.Result;
                }
            }

            return Result.Success;
        }

        async Task<Result> RequestStopAsync()
        {
            try
            {
                using var shutdownContext = new ViewContext(windowContext, ContextInheritance.None);
                return (await requestShutdown.ExecuteAsync(shutdownContext)).Result;
            }
            catch (Exception error)
            {
                return failures.RecordAndContinue(error);
            }
        }

        Result CancelGame()
        {
            if (gameTask is null || gameTask.IsCompleted)
                return Result.Success;

            try { gameContext.Cancellation.Cancel(); }
            catch (Exception error) { return failures.RecordAndContinue(error); }
            return Result.Success;
        }

        Result JoinGame()
        {
            if (gameTask is null)
                return Result.Success;

            // 合流後も Close はウィンドウの所有スレッドで呼び出します。
            try { return gameTask.GetAwaiter().GetResult().Result; }
            catch (Exception error) { return failures.RecordAndContinue(error); }
        }
    }
}
