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
        using var session = GameTestSteps.Ready(app);
        GameCommand[] commands = [GameCommand.Up, GameCommand.Up, GameCommand.Down, GameCommand.Down,
            GameCommand.Right, GameCommand.Interact, GameCommand.Right, GameCommand.Right, GameCommand.Right,
            GameCommand.Interact, GameCommand.Up, GameCommand.Up];
        foreach (var command in commands) GameTestSteps.PlayTurn(session, command);
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
        using var session = GameTestSteps.Ready(app);
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
        using var session = GameTestSteps.Ready(app);
        for (var i = 0; i < 4; i++) GameTestSteps.PlayTurn(session, GameCommand.Right);
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
        using var session = GameTestSteps.Ready(app);
        if (playerEntersGuard) GameTestSteps.PlayTurn(session, GameCommand.Wait);
        GameTestSteps.PlayTurn(session, GameCommand.Up);
        GameTestSteps.PlayTurn(session, GameCommand.Right);
        var snapshot = app.Display.Snapshot();
        Assert.Equal(GameOutcome.Lost, snapshot.Outcome);
        Assert.Equal(snapshot.Player, snapshot.Enemy);
        Assert.Equal(playerEntersGuard ? GameStage.PlayerPresenting : GameStage.EnemyPresenting, snapshot.Stage);
    }
}
