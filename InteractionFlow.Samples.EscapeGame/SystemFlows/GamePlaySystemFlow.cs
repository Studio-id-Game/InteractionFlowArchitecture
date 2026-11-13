using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
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
            var run = new RunData(beginSession, gameSessionFlow, endSession);

            if (!await run.CheckStartAsync(context))
            {
                return run.LastEnd;
            }

            if (!await run.PlayAndCheckContinueAsync(context))
            {
                return run.LastEnd;
            }
        }
    }

    private class RunData(BeginSession beginSession, GameSessionSystemFlow gameSessionFlow, EndSession endSession)
    {
        private long sessionId;

        private bool isCanceled;

        private InteractionEnd? lastEnd;

        public InteractionEnd LastEnd
        {
            get => lastEnd ?? throw new NullReferenceException("GamePlaySystemFlow.RunData.LastEnd");
            set => lastEnd = value;
        }

        public async Task<bool> CheckStartAsync(IGameplayContext rootContext)
        {
            if (rootContext.Gameplay.TryAllocateSession(out sessionId))
                return true;

            // ウィンドウ停止とセッション生成が競合した通常終了です。
            await EndSessionAsync(rootContext, Result.Success);
            return false;
        }

        public async Task<bool> PlayAndCheckContinueAsync(IGameplayContext rootContext)
        {
            using var sessionContext = new GameplayContext(rootContext, sessionId, ContextInheritance.All);

            return await sessionContext
                .AsResultAsync()
                .ThenAsync(async context => await ExecuteAsync(context, beginSession.ExecuteAsync))
                .ThenAsync(async context => await ExecuteAsync(context, async context =>
                    {
                        return (await GameplayCheckpoint.WaitToContinueResultAsync(context, LastEnd.End))
                        .Resolve(
                            () => context.AsResult(),
                            e => { isCanceled = true; return e; }
                        );
                    })
                )
                .ThenAsync(async context => await ExecuteAsync(context, gameSessionFlow.ExecuteAsync))
                .ResolveAsync(
                    async context =>
                    {
                        var retryRequested = context.Gameplay.ReadGameState(context.SessionId).State.RetryRequested;
                        await EndSessionAsync(context, Result.Success);
                        return !LastEnd.HasException && retryRequested && !context.Lifetime.ShutdownRequested;
                    },
                    async error =>
                    {
                        await EndSessionAsync(sessionContext, isCanceled ? Result.Success : error);
                        return false;
                    }
                );
        }

        private async Task EndSessionAsync(IGameplayContext context, Result sessionResult)
        {
            // 旧セッションの待機を閉じてから、新しいセッションを作成・公開します。
            using var cleanupContext = new GameplayContext(context, ContextInheritance.None);
            cleanupContext.With(new CleanupInfo(CleanupKind.EndSession, sessionResult));

            try
            {
                LastEnd = await endSession.ExecuteAsync(cleanupContext);
            }
            catch (Exception error) when (!sessionResult.Try(out var sessionError))
            {
                throw new AggregateException(sessionError, error);
            }
        }

        private async Task<Result<TContext>> ExecuteAsync<TContext>(TContext context, Func<TContext, Task<InteractionEnd>> interaction)
            where TContext : IGameplayContext
        {
            return await ExecuteAsync(context, async c =>
            {
                LastEnd = await interaction(context);
                return LastEnd.Result.Then(() => context.AsResult());
            });
        }

        private async Task<Result<TContext>> ExecuteAsync<TContext>(TContext context, Func<TContext, Task<Result<TContext>>> action)
            where TContext : IGameplayContext
        {
            try
            {
                return await action(context);
            }
            catch (OperationCanceledException e)
                when (context.Lifetime.ShutdownRequested || context.Cancellation.IsCancellationRequested)
            {
                isCanceled = true;
                return e;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
