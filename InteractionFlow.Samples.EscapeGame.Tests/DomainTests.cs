using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class DomainTests
{
    [Fact]
    public void KnownSolutionWinsInTwelveTurnsWithKeyConsumedAndNoTerminalEnemyMove()
    {
        using var app = new EscapeApplicationContext();
        using var session = Ready(app);
        GameCommand[] commands = [GameCommand.Up, GameCommand.Up, GameCommand.Down, GameCommand.Down,
            GameCommand.Right, GameCommand.Interact, GameCommand.Right, GameCommand.Right, GameCommand.Right,
            GameCommand.Interact, GameCommand.Up, GameCommand.Up];
        foreach (var command in commands) PlayTurn(session, command);
        var snapshot = app.Display.Snapshot();
        Assert.Equal(GameOutcome.Won, snapshot.Outcome);
        Assert.Equal(12, snapshot.Turn);
        Assert.Equal(snapshot.Room.Exit, snapshot.Player);
        Assert.True(snapshot.KeyCollected);
        Assert.False(snapshot.HasKey);
        Assert.True(snapshot.SwitchActivated);
        Assert.True(snapshot.DoorUnlocked);
        Assert.Equal(3, snapshot.EnemyPatrolIndex);
        Assert.Equal(MutationStatus.Rejected, session.World.ApplyEnemy(session.SessionId).Status);
    }

    [Fact]
    public void SelectionCancellationAndInvalidActionsDoNotConsumeATurn()
    {
        using var app = new EscapeApplicationContext();
        using var session = Ready(app);
        var id = session.SessionId;
        var initial = app.Display.Snapshot();
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, GameCommand.Up));
        Assert.NotNull(app.Display.Snapshot().Selection);
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, GameCommand.CancelSelection));
        Assert.Null(app.Display.Snapshot().Selection);
        Assert.Equal(MutationStatus.Rejected, session.World.ApplySelection(id, GameCommand.Left));
        Assert.Equal(MutationStatus.Rejected, session.World.ApplySelection(id, GameCommand.Confirm));
        Assert.Equal(MutationStatus.Rejected, session.World.ApplyPlayer(id).Status);
        var current = app.Display.Snapshot();
        Assert.Equal(initial.Player, current.Player);
        Assert.Equal(initial.Enemy, current.Enemy);
        Assert.Equal(0, current.Turn);
        Assert.False(current.KeyCollected);
    }

    [Fact]
    public void ClosedDoorAndUnqualifiedUnlockRemainInvalid()
    {
        using var app = new EscapeApplicationContext();
        using var session = Ready(app);
        for (var i = 0; i < 4; i++) PlayTurn(session, GameCommand.Right);
        var before = app.Display.Snapshot();
        Assert.Equal(new GridPosition(5, 3), before.Player);
        Assert.Equal(MutationStatus.Rejected, session.World.ApplySelection(session.SessionId, GameCommand.Up));
        Assert.Equal(MutationStatus.Rejected, session.World.ApplySelection(session.SessionId, GameCommand.Interact));
        var after = app.Display.Snapshot();
        Assert.False(after.DoorUnlocked);
        Assert.Equal(before.Turn, after.Turn);
        Assert.Equal(before.Enemy, after.Enemy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlayerAndEnemyCaptureBothProduceGameLoss(bool playerEntersGuard)
    {
        using var app = new EscapeApplicationContext();
        using var session = Ready(app);
        if (playerEntersGuard) PlayTurn(session, GameCommand.Wait);
        PlayTurn(session, GameCommand.Up);
        PlayTurn(session, GameCommand.Right);
        var snapshot = app.Display.Snapshot();
        Assert.Equal(GameOutcome.Lost, snapshot.Outcome);
        Assert.Equal(snapshot.Player, snapshot.Enemy);
        Assert.Equal(playerEntersGuard ? GameStage.PlayerPresenting : GameStage.EnemyPresenting, snapshot.Stage);
    }

    internal static EscapeApplicationContext Ready(EscapeApplicationContext app)
    {
        var session = app.CreateSessionContext();
        var begin = session.World.BeginSession(session.SessionId);
        Assert.Equal(MutationStatus.Applied, begin.Status);
        Complete(session, begin.Presentation!.Value);
        Assert.Equal(MutationStatus.Applied, session.World.EnterSelecting(session.SessionId));
        return session;
    }

    internal static void Complete(EscapeApplicationContext context, PresentationKey key)
    {
        var revision = context.Display.Snapshot().ControlRevision;
        context.Presentation.Advance(key, 0, revision);
        context.Presentation.Advance(key, 1, revision);
        Assert.True(context.Presentation.TryComplete(key));
    }

    internal static void PlayTurn(EscapeApplicationContext session, GameCommand command)
    {
        var id = session.SessionId;
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, command));
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, GameCommand.Confirm));
        var player = session.World.ApplyPlayer(id);
        Assert.Equal(MutationStatus.Applied, player.Status);
        Complete(session, player.Presentation!.Value);
        if (session.World.GetProgress(id).Outcome != GameOutcome.Running) return;
        var enemy = session.World.ApplyEnemy(id);
        Assert.Equal(MutationStatus.Applied, enemy.Status);
        Complete(session, enemy.Presentation!.Value);
        if (session.World.GetProgress(id).Outcome == GameOutcome.Running)
            Assert.Equal(MutationStatus.Applied, session.World.EnterSelecting(id));
    }
}
