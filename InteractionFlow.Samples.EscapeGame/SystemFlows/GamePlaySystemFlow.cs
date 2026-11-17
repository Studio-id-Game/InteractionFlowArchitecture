using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class GamePlaySystemFlow(BeginSession beginSession, GameSessionSystemFlow gameSessionFlow, EndSession endSession)
    : SystemFlow<IGameplayContext>(beginSession, gameSessionFlow, endSession)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        while (true)
        {
            var (end, sessionId) = await CheckStartAsync(context);
            if (end != null) return end;

            end = await PlayAndCheckContinueAsync(context, sessionId);
            if (end != null) return end;
        }
    }

    private class SessionEndException(Exception? innerException = null) : Exception("Session is end.", innerException)
    {
    }

    public async Task<(InteractionEnd? end, long sessionId)> CheckStartAsync(IGameplayContext context)
    {
        if (context.Gameplay.TryAllocateSession(out var sessionId))
            return (null, sessionId);

        // ウィンドウ停止とセッション生成が競合した通常終了です。
        return (await EndSessionAsync(context, Result.Success), 0);
    }

    public async Task<InteractionEnd?> PlayAndCheckContinueAsync(IGameplayContext rootContext, long sessionId)
    {
        using var context = new GameplayContext(rootContext, sessionId, ContextInheritance.All);
        return await context.AsResult<IGameplayContext>()
            .ThenAsync(GameplayAsync)
            .ResolveAsync(
                async context =>
                {
                    var retryRequested = context.Gameplay.ReadGameState(context.SessionId).State.RetryRequested;
                    var end = await EndSessionAsync(context, Result.Success);
                    var quit = end.HasException || context.Lifetime.ShutdownRequested || !retryRequested;
                    return quit ? end : null;
                },
                async error =>
                {
                    var isSessionEnd = error is ResultException { InnerException: SessionEndException };
                    var resultError = isSessionEnd ? Result.Success : error;
                    var end = await EndSessionAsync(context, resultError);
                    return (InteractionEnd?)end;
                }
            );
    }

    private async Task<Result<IGameplayContext>> GameplayAsync(IGameplayContext context)
    {
        try
        {
            InteractionEnd? end = null;
            return await context
                .AsResult()
                .ThenAsync(context => SetEnd(context, beginSession.ExecuteAsync))
                .ThenAsync(context => WaitToContinueAsync(context, end!))
                .ThenAsync(context => SetEnd(context, gameSessionFlow.ExecuteAsync));

            async Task<Result<IGameplayContext>> SetEnd(IGameplayContext context, Func<IGameplayContext, Task<InteractionEnd>> interaction)
            {
                end = await interaction(context);
                return end.Result.Then(() => context.AsResult());
            }

            async Task<Result<IGameplayContext>> WaitToContinueAsync(IGameplayContext context, InteractionEnd end)
            {
                return await GameplayCheckpoint.WaitToContinueAsync(context, end.End) ?
                    context.AsResult() : new SessionEndException();
            }
        }
        catch (OperationCanceledException e)
            when (context.Lifetime.ShutdownRequested || context.Cancellation.IsCancellationRequested)
        {
            return new SessionEndException(e);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private async Task<InteractionEnd> EndSessionAsync(IGameplayContext context, Result sessionResult)
    {
        // 旧セッションの待機を閉じてから、新しいセッションを作成・公開します。
        using var cleanupContext = new GameplayContext(context, ContextInheritance.None);
        cleanupContext.With(new CleanupInfo(CleanupKind.EndSession, sessionResult));

        try
        {
            return await endSession.ExecuteAsync(cleanupContext);
        }
        catch (Exception error) when (sessionResult.HasError(out var sessionError))
        {
            throw new AggregateException(sessionError, error);
        }
    }
}
