using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Core.Entities.Contexts;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

// 公開アクセスを操作する共通の準備手順です。ゲーム規則や描画計算は複製しません。
internal static class GameTestSteps
{
    internal static GameplayContext CreateSessionContext(IGameplayContext app)
        => new(app, app.Gameplay.AllocateSession(), ContextInheritance.Cancellation);

    internal static GameplayContext Ready(IApplicationContext app)
    {
        var session = CreateSessionContext(app);
        var begin = session.Gameplay.BeginSession(session.SessionId);
        Assert.Equal(GameUpdateStatus.Applied, begin.Status);
        Complete(app, Assert.IsType<PresentationKey>(begin.Presentation));
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.BeginActionSelection(session.SessionId));
        return session;
    }

    internal static void Complete(IGameViewContext context, PresentationKey key)
    {
        var revision = context.View.CaptureSnapshot().ControlRevision;
        var first = context.View.AdvancePresentation(key, 0, revision);
        Assert.NotNull(first);
        context.View.AdvancePresentation(key, first.DurationSeconds, revision);
        Assert.True(context.View.TryCompletePresentation(key));
    }

    internal static void PlayTurn(IGameplayContext session, IGameViewContext view, GameCommand command)
    {
        var id = session.SessionId;
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, command));
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm));

        var player = session.Gameplay.ApplyPlayerAction(id);
        Assert.Equal(GameUpdateStatus.Applied, player.Status);
        Complete(view, Assert.IsType<PresentationKey>(player.Presentation));
        if (session.Gameplay.GetSessionProgress(id).Outcome != GameOutcome.Running)
            return;

        var enemy = session.Gameplay.ApplyEnemyAction(id);
        Assert.Equal(GameUpdateStatus.Applied, enemy.Status);
        Complete(view, Assert.IsType<PresentationKey>(enemy.Presentation));
        if (session.Gameplay.GetSessionProgress(id).Outcome == GameOutcome.Running)
            Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.BeginActionSelection(id));
    }
}
