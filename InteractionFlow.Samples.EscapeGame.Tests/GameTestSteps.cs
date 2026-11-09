using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

// 公開アクセスを操作する共通の準備手順です。ゲーム規則や描画計算は複製しません。
internal static class GameTestSteps
{
    internal static EscapeApplicationContext Ready(EscapeApplicationContext app, WindowContext window)
    {
        var session = app.CreateSessionContext();
        var begin = session.Session.BeginSession(session.SessionId);
        Assert.Equal(MutationStatus.Applied, begin.Status);
        Complete(window, Assert.IsType<PresentationKey>(begin.Presentation));
        Assert.Equal(MutationStatus.Applied, session.World.EnterSelecting(session.SessionId));
        return session;
    }

    internal static void Complete(WindowContext context, PresentationKey key)
    {
        var revision = context.Display.Snapshot().ControlRevision;
        var first = context.Presentation.Advance(key, 0, revision);
        Assert.NotNull(first);
        context.Presentation.Advance(key, first.DurationSeconds, revision);
        Assert.True(context.Presentation.TryComplete(key));
    }

    internal static void PlayTurn(EscapeApplicationContext session, WindowContext window, GameCommand command)
    {
        var id = session.SessionId;
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, command));
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, GameCommand.Confirm));

        var player = session.World.ApplyPlayer(id);
        Assert.Equal(MutationStatus.Applied, player.Status);
        Complete(window, Assert.IsType<PresentationKey>(player.Presentation));
        if (session.Session.GetProgress(id).Outcome != GameOutcome.Running)
            return;

        var enemy = session.World.ApplyEnemy(id);
        Assert.Equal(MutationStatus.Applied, enemy.Status);
        Complete(window, Assert.IsType<PresentationKey>(enemy.Presentation));
        if (session.Session.GetProgress(id).Outcome == GameOutcome.Running)
            Assert.Equal(MutationStatus.Applied, session.World.EnterSelecting(id));
    }
}
