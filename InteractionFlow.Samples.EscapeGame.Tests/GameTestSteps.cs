using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Core.Entities.Contexts;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

// 公開アクセスを操作する共通の準備手順です。ゲーム規則や描画計算は複製しません。
internal static class GameTestSteps
{
    internal static GameplayContext CreateSession(IGameplayContext app)
        => new(app, app.Gameplay.CreateSession(), ContextInheritance.Cancellation);

    internal static GameplayContext Ready(IApplicationContext app)
    {
        var session = CreateSession(app);
        var begin = session.Gameplay.BeginSession(session.SessionId);
        Assert.Equal(MutationStatus.Applied, begin.Status);
        Complete(app, Assert.IsType<PresentationKey>(begin.Presentation));
        Assert.Equal(MutationStatus.Applied, session.Gameplay.EnterSelecting(session.SessionId));
        return session;
    }

    internal static void Complete(IGameViewContext context, PresentationKey key)
    {
        var revision = context.View.Snapshot().ControlRevision;
        var first = context.View.Advance(key, 0, revision);
        Assert.NotNull(first);
        context.View.Advance(key, first.DurationSeconds, revision);
        Assert.True(context.View.TryComplete(key));
    }

    internal static void PlayTurn(IGameplayContext session, IGameViewContext view, GameCommand command)
    {
        var id = session.SessionId;
        Assert.Equal(MutationStatus.Applied, session.Gameplay.ApplySelection(id, command));
        Assert.Equal(MutationStatus.Applied, session.Gameplay.ApplySelection(id, GameCommand.Confirm));

        var player = session.Gameplay.ApplyPlayer(id);
        Assert.Equal(MutationStatus.Applied, player.Status);
        Complete(view, Assert.IsType<PresentationKey>(player.Presentation));
        if (session.Gameplay.GetProgress(id).Outcome != GameOutcome.Running)
            return;

        var enemy = session.Gameplay.ApplyEnemy(id);
        Assert.Equal(MutationStatus.Applied, enemy.Status);
        Complete(view, Assert.IsType<PresentationKey>(enemy.Presentation));
        if (session.Gameplay.GetProgress(id).Outcome == GameOutcome.Running)
            Assert.Equal(MutationStatus.Applied, session.Gameplay.EnterSelecting(id));
    }
}
