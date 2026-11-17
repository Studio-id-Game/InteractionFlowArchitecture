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

    private enum SessionProgress { Completed, Stopped }

    private readonly record struct SessionFailure(InteractionEnd? End);

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
        return await GameplayAsync(context)
            .ResolveAsync(
                progress => FinishAsync(progress == SessionProgress.Completed),
                (error, failure) => FinishAsync(false, error, failure.End));

        async Task<InteractionEnd?> FinishAsync(bool allowRetry, Result previous = default, InteractionEnd? failed = null)
        {
            var retry = allowRetry && context.Gameplay.ReadGameState(context.SessionId).State.RetryRequested;
            var end = await EndSessionAsync(context, previous);
            if (failed != null && ReferenceEquals(end.Result.Error, previous.Error))
                return failed;

            return retry && !end.HasException && !context.Lifetime.ShutdownRequested ? null : end;
        }
    }

    private async Task<Result<SessionProgress, SessionFailure>> GameplayAsync(IGameplayContext context)
    {
        try
        {
            var started = await beginSession.ExecuteAsync(context);
            return await ToSessionResult(started).ThenAsync(ContinueAsync);

            async Task<Result<SessionProgress, SessionFailure>> ContinueAsync(InteractionEnd end)
            {
                if (!await GameplayCheckpoint.WaitToContinueAsync(context, end.End))
                    return SessionProgress.Stopped;

                return ToSessionResult(await gameSessionFlow.ExecuteAsync(context))
                    .Then(_ => Result<SessionProgress, SessionFailure>.CreateSuccess(SessionProgress.Completed));
            }
        }
        catch (OperationCanceledException)
            when (context.Lifetime.ShutdownRequested || context.Cancellation.IsCancellationRequested)
        {
            return SessionProgress.Stopped;
        }
        catch (Exception e)
        {
            return (e, new SessionFailure(null));
        }
    }

    private static Result<InteractionEnd, SessionFailure> ToSessionResult(InteractionEnd end)
    {
        if (end.Result.HasError(out var error))
            return (error, new SessionFailure(end));

        return end;
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
