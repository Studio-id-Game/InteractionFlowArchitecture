using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class GameSystemFlow(SessionSystemFlow sessionFlow, EndSession endSession)
    : SystemFlow<IGameplayContext>(sessionFlow, endSession)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        while (true)
        {
            GameplayContext created;
            try
            {
                var id = context.Gameplay.CreateSession();
                created = new GameplayContext(context, id, ContextInheritance.All);
            }
            catch (InvalidOperationException) when (context.Lifetime.ShutdownRequested)
            {
                // ウィンドウ停止とセッション生成が競合した通常終了です。
                using var stopped = new GameplayContext(context, ContextInheritance.None);
                stopped.With(new Finishing(FinishingKind.EndSession, Result.Success));
                return await endSession.ExecuteAsync(stopped);
            }

            using var session = created;
            Result outcome;
            var retry = false;
            try
            {
                var end = await sessionFlow.ExecuteAsync(session);
                outcome = end.Result;
                retry = !end.HasException && session.Gameplay.GetProgress(session.SessionId).RetryRequested;
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
            cleanup.With(new Finishing(FinishingKind.EndSession, outcome));
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
