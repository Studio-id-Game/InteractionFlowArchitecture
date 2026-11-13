using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
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
    private class RunData : IDisposable
    {
        private readonly IApplicationContext context;
        private readonly FailureCollector failures;
        private readonly WindowState windowState;

        public FrameState FrameState { get; }

        public ViewContext WindowContext { get; }

        public GameplayContext GameContext { get; }

        public RunData(IApplicationContext context)
        {
            this.context = context;

            failures = new();
            FrameState = new FrameState();
            windowState = new WindowState();

            WindowContext = new ViewContext(context, ContextInheritance.None)
                .WithT(FrameState)
                .WithT(windowState);
            GameContext = new GameplayContext(context, ContextInheritance.Cancellation);
        }

        public void Dispose()
        {
            WindowContext.Dispose();
            GameContext.Dispose();
        }

        public GameplayContext CreateGameplayContext()
        {
            return new GameplayContext(context, ContextInheritance.None);
        }

        public ViewContext CreateShutdownContext()
        {
            return new ViewContext(WindowContext, ContextInheritance.None); ;
        }

        public ViewContext CreateCloseContext()
        {
            var cleanupInfo = new CleanupInfo(CleanupKind.CloseWindow, failures.ToResult());
            return new ViewContext(WindowContext, ContextInheritance.All)
                .WithT(cleanupInfo);
        }

        private Task<InteractionEnd>? gameTask;

        private InteractionEnd? end;

        public Task<InteractionEnd>? GameTask => gameTask;

        private Result Result => end == null ? Result.Success : end.Result;

        public bool HasException => !Result.Try(out _);

        public InteractionEnd End => end!;

        public RunData SetGameTask(Task<InteractionEnd> value)
        {
            gameTask = value;
            return this;
        }

        public RunData SetEnd(InteractionEnd value)
        {
            end = value;
            return this;
        }

        public Result<RunData> AsEndResult()
        {
            return Result.Try(out var e) ? this : e;
        }

        public RunData CatchError(Exception e)
        {
            failures.Add(e);
            return this;
        }

        public Exception AddError(Exception e)
        {
            return failures.Add(e);
        }

        public void AddEndError()
        {
            failures.Add(End.Result);
        }

        public bool HasError => failures.HasFailure;

        public AggregateException GetErrorException()
        {
            return failures.ToAggregate();
        }
    }

    protected override async Task<InteractionEnd> ExecuteCoreAsync(IApplicationContext context)
    {
        using var data = new RunData(context);
        return await data.AsResultAsync()
            .ThenAsync(StartGameAsync)
            .ThenAsync(StopGameAsync)
            .ThenAsync(CloseWindowAsync)
            .ResolveAsync(async data => data.End, async e => throw e);
    }

    private async Task<Result<RunData>> StartGameAsync(RunData data)
    {
        var inputState = data.FrameState;
        var windowContext = data.WindowContext;
        var gameContext = data.GameContext;

        return await data
            .AsResultAsync()
            .ThenAsync(OpenWindow)
            .ThenAsync(RunGame)
            .ThenAsync(FrameLoop)
            .ThenErrorAsync(_ => data.AsResultAsync());

        async Task<Result<RunData>> OpenWindow(RunData data)
        {
            try
            {
                return data
                    .SetEnd(await openWindow.ExecuteAsync(windowContext))
                    .AsEndResult()
                    .ThenError(e => data.AddError(e));
            }
            catch (Exception e)
            {
                return data.AddError(e);
            }
        }

        async Task<Result<RunData>> RunGame(RunData data)
        {
            try
            {
                return data
                    .SetGameTask(gamePlaySystem.ExecuteAsync(gameContext))
                    .AsEndResult();
            }
            catch (Exception e)
            {
                return data.AddError(e);
            }
        }

        async Task<Result<RunData>> FrameLoop(RunData data)
        {
            try
            {
                using var controlContext = data.CreateGameplayContext();

                var gameTask = data.GameTask!;
                while (!gameTask.IsCompleted && !windowContext.Lifetime.ShutdownRequested)
                {
                    data.SetEnd(await presentFrame.ExecuteAsync(windowContext));

                    if (data.HasException)
                        return data.AsEndResult().ThenError(e => data.AddError(e));

                    if (inputState.ShutdownRequested)
                        return data.AsEndResult();

                    // ゲーム入力/提示待機から独立した制御経路です。通常入力は同フレームで配送しません。
                    if (!inputState.PauseRequested && !inputState.SkipRequested)
                        continue;

                    data.SetEnd(await controlInput.ExecuteAsync(controlContext));

                    if (data.HasException)
                        return data.AsEndResult().ThenError(e => data.AddError(e));
                }

                return data.AsEndResult();
            }
            catch (Exception e)
            {
                return data.AddError(e);
            }
        }
    }

    private async Task<Result<RunData>> StopGameAsync(RunData data)
    {
        // Stop は提示・入力・再開待機を解除します。失敗しても cancel/join/close を続けます。

        var gameContext = data.GameContext;

        return await data
            .AsResultAsync()
            .ThenAsync(ShutdownRequest)
            .ThenAsync(GameCancel)
            .ThenAsync(WaitGameEnd)
            .ThenErrorAsync(async e => data.CatchError(e));

        async Task<Result<RunData>> ShutdownRequest(RunData data)
        {
            try
            {
                using var shutdownContext = data.CreateShutdownContext();

                return data.SetEnd(await requestShutdown.ExecuteAsync(shutdownContext))
                    .AsEndResult()
                    .ThenError(e => data.CatchError(e));
            }
            catch (Exception e)
            {
                return data.CatchError(e);
            }
        }

        async Task<Result<RunData>> GameCancel(RunData data)
        {
            var gameTask = data.GameTask;

            if (gameTask != null && !gameTask.IsCompleted)
            {
                try { gameContext.Cancellation.Cancel(); }
                catch (Exception e) { data.AddError(e); }
            }

            return data.AsResult();
        }

        async Task<Result<RunData>> WaitGameEnd(RunData data)
        {
            var gameTask = data.GameTask;

            if (gameTask != null)
            {
                try
                {
                    data.SetEnd(gameTask.GetAwaiter().GetResult());
                    data.AddEndError();
                }
                catch (Exception e)
                {
                    data.AddError(e);
                }
            }

            return data.AsResult();
        }
    }

    private async Task<Result<RunData>> CloseWindowAsync(RunData data)
    {
        try
        {
            using var closeContext = data.CreateCloseContext();
            return data.SetEnd(await closeWindow.ExecuteAsync(closeContext));
        }
        catch (Exception error) when (data.HasError)
        {
            data.AddError(error);
            throw data.GetErrorException();
        }
    }
}
