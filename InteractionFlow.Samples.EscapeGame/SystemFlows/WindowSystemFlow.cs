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
    private class RunContext : IDisposable
    {
        private FailureCollector Failures { get; }

        public IApplicationContext Context { get; }

        public ViewContext WindowContext { get; }
        public GameplayContext GameContext { get; }
        public FrameState FrameState { get; }
        public Task<InteractionEnd>? GameTask { get; set; }

        public InteractionEnd? End { get; set; }

        public RunContext(IApplicationContext context)
        {
            Context = context;
            Failures = new FailureCollector();
            FrameState = new FrameState();
            WindowContext = new ViewContext(context, ContextInheritance.None)
                .WithT(FrameState)
                .WithT(new WindowState());
            GameContext = new GameplayContext(context, ContextInheritance.Cancellation);
        }

        public void Dispose()
        {
            WindowContext.Dispose();
            GameContext.Dispose();
        }

        public Result CollectedErrors => Failures.ToResult();

        public AggregateException AggregateErrors() => Failures.ToAggregate();

        public Result<RunContext> CollectError(Exception error)
        {
            Failures.Add(error);
            return this;
        }
    }

    protected override async Task<InteractionEnd> ExecuteCoreAsync(IApplicationContext context)
    {
        using var runContext = new RunContext(context);

        return await runContext.AsResult()
            .ThenAsync(StartGameAsync)
            .ThenAsync(StopGameAsync)
            .ThenAsync(CloseWindowAsync)
            .ResolveAsync(
                completed => completed.End ?? throw completed.AggregateErrors(),
                error => throw error
            );
    }

    private async Task<Result<RunContext>> StartGameAsync(RunContext context)
    {
        try
        {
            return await context.AsResult()
                .ThenAsync(OpenWindowAsync)
                .ThenAsync(RunGameAsync)
                .ThenAsync(FrameLoopAsync)
                .ThenErrorAsync(context.CollectError);
        }
        catch (Exception error)
        {
            return context.CollectError(error);
        }

        async Task<Result<RunContext>> OpenWindowAsync(RunContext context)
        {
            var end = await openWindow.ExecuteAsync(context.WindowContext);
            return end.Result.Then(() => context.AsResult());
        }

        async Task<Result<RunContext>> RunGameAsync(RunContext context)
        {
            context.GameTask = gamePlaySystem.ExecuteAsync(context.GameContext);
            return context;
        }

        async Task<Result<RunContext>> FrameLoopAsync(RunContext context)
        {
            using var controlContext = new GameplayContext(context.Context, ContextInheritance.None);
            var windowContext = context.WindowContext;
            var frameState = context.FrameState;
            var gameTask = context.GameTask!;

            var result = Result.Success;

            while (!gameTask.IsCompleted && !windowContext.Lifetime.ShutdownRequested)
            {
                var frame = await presentFrame.ExecuteAsync(windowContext);
                if (frame.HasException || frameState.ShutdownRequested)
                {
                    result = frame.Result;
                    break;
                }

                if (!frameState.PauseRequested && !frameState.SkipRequested)
                {
                    continue;
                }

                var control = await controlInput.ExecuteAsync(controlContext);
                if (control.HasException)
                {
                    result = control.Result;
                    break;
                }
            }

            return result.Then(() => context.AsResult());
        }
    }

    private async Task<Result<RunContext>> StopGameAsync(RunContext context)
    {
        // Stop は提示・入力・再開待機を解除します。失敗しても cancel/join/close を続けます。
        return await context.AsResult()
            .ThenAsync(ShutdownRequestAsync)
            .ThenAsync(GameCancel)
            .ThenAsync(WaitGameEnd);

        async Task<Result<RunContext>> ShutdownRequestAsync(RunContext context)
        {
            var windowContext = context.WindowContext;

            try
            {
                using var shutdownContext = new ViewContext(windowContext, ContextInheritance.None);
                var end = await requestShutdown.ExecuteAsync(shutdownContext);
                return end.Result.Resolve(() => context.AsResult(), context.CollectError);
            }
            catch (Exception error)
            {
                return context.CollectError(error);
            }
        }

        Result<RunContext> GameCancel(RunContext context)
        {
            var gameContext = context.GameContext;
            var gameTask = context.GameTask;

            if (gameTask == null || gameTask.IsCompleted)
            {
                return context;
            }

            try
            {
                gameContext.Cancellation.Cancel();
                return context;
            }
            catch (Exception error)
            {
                return context.CollectError(error);
            }
        }

        Result<RunContext> WaitGameEnd(RunContext context)
        {
            var gameTask = context.GameTask;

            if (gameTask == null)
            {
                return context;
            }

            try
            {
                var end = gameTask.GetAwaiter().GetResult();
                return end.Result.Resolve(() => context.AsResult(), context.CollectError);
            }
            catch (Exception error)
            {
                return context.CollectError(error);
            }
        }
    }

    private async Task<Result<RunContext>> CloseWindowAsync(RunContext context)
    {
        var windowContext = context.WindowContext;
        var errors = context.CollectedErrors;

        try
        {
            var cleanupInfo = new CleanupInfo(CleanupKind.CloseWindow, errors);
            InteractionEnd end;
            using (var closeContext = new ViewContext(windowContext, ContextInheritance.All).WithT(cleanupInfo))
            {
                end = await closeWindow.ExecuteAsync(closeContext);
            }

            context.End = end;
            return context;
        }
        catch (Exception error) when (!errors.IsSuccess)
        {
            return context.CollectError(error);
        }
    }
}
