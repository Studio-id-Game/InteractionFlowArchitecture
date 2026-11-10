using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class GamePlaySystemFlow(BeginSession begin, GameSessionSystemFlow sessionFlow, EndSession endSession)
    : SystemFlow<IGameplayContext>(begin, sessionFlow, endSession)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        while (true)
        {
            if (!context.Gameplay.TryAllocateSession(out var id))
            {
                // ウィンドウ停止とセッション生成が競合した通常終了です。
                using var stopped = new GameplayContext(context, ContextInheritance.None);
                stopped.With(new CleanupInfo(CleanupKind.EndSession, Result.Success));
                return await endSession.ExecuteAsync(stopped);
            }

            using var session = new GameplayContext(context, id, ContextInheritance.All);
            Result outcome;
            var retry = false;
            try
            {
                var end = await begin.ExecuteAsync(session);
                if (await GameplayCheckpoint.WaitToContinueAsync(session, end.End))
                    end = await sessionFlow.ExecuteAsync(session);
                outcome = end.Result;
                retry = !end.HasException && session.Gameplay.ReadGameState(session.SessionId).State.RetryRequested;
            }
            // Interaction間の再開待機も、実行自身へのキャンセルなら通常停止です。
            catch (OperationCanceledException) when (session.Lifetime.ShutdownRequested
                || session.Cancellation.IsCancellationRequested)
            {
                outcome = Result.Success;
            }
            catch (Exception error)
            {
                outcome = error;
            }

            // 旧セッションの待機を閉じてから、新しいセッションを作成・公開します。
            using var cleanup = new GameplayContext(session, ContextInheritance.None);
            cleanup.With(new CleanupInfo(CleanupKind.EndSession, outcome));
            InteractionEnd ended;
            try { ended = await endSession.ExecuteAsync(cleanup); }
            catch (Exception error) when (!outcome.Try(out _))
            {
                throw FailureCollector.Append(outcome, error);
            }

            if (ended.HasException || !retry || context.Lifetime.ShutdownRequested)
                return ended;
        }
    }
}
