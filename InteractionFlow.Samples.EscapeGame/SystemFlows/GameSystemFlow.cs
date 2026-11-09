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
    : SystemFlow<EscapeApplicationContext>(sessionFlow, endSession)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        while (true)
        {
            EscapeApplicationContext created;
            try
            {
                created = context.CreateSessionContext();
            }
            catch (InvalidOperationException) when (context.Session.ShutdownRequested)
            {
                // ウィンドウ停止とセッション生成が競合した通常終了です。
                using var stopped = context.CreateIndependentContext();
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
                retry = !end.HasException && session.Session.GetProgress(session.SessionId).RetryRequested;
            }
            // Interaction間の再開待機も、実行自身へのキャンセルなら通常停止です。
            catch (OperationCanceledException) when (session.Session.ShutdownRequested
                || session.Cancellation.IsCancellationRequested)
            {
                outcome = Result.Success;
            }
            catch (Exception error)
            {
                outcome = error;
            }

            // 旧セッションの待機を閉じてから、新しいセッションを作成・公開します。
            using var cleanup = session.CreateIndependentContext();
            cleanup.With(new Finishing(FinishingKind.EndSession, outcome));
            InteractionEnd ended;
            try { ended = await endSession.ExecuteAsync(cleanup); }
            catch (Exception error) when (!outcome.Try(out _))
            {
                throw FailureCollector.Append(outcome, error);
            }

            if (ended.HasException || !retry || context.Session.ShutdownRequested)
                return ended;
        }
    }
}
