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
        foreach (var command in commands) GameTestSteps.PlayTurn(session, app, command);
        var snapshot = app.View.CaptureSnapshot();
        Assert.Equal(GameOutcome.Won, snapshot.Outcome);
        Assert.Equal(12, snapshot.Turn);
        Assert.Equal(snapshot.Room.Exit, snapshot.Player);
        Assert.True(snapshot.KeyCollected);
        Assert.False(snapshot.HasKey);
        Assert.True(snapshot.SwitchActivated);
        Assert.True(snapshot.DoorUnlocked);
        Assert.Equal(3, snapshot.EnemyPatrolIndex);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyEnemyAction(session.SessionId).Status);
    }

    [Fact]
    public void SelectionCancellationAndInvalidActionsDoNotConsumeATurn()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var initial = app.View.CaptureSnapshot();
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, GameCommand.Up));
        Assert.NotNull(app.View.CaptureSnapshot().Selection);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, GameCommand.CancelSelection));
        Assert.Null(app.View.CaptureSnapshot().Selection);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyInputCommand(id, GameCommand.Left));
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm));
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyPlayerAction(id).Status);
        var current = app.View.CaptureSnapshot();
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
        for (var i = 0; i < 4; i++) GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var before = app.View.CaptureSnapshot();
        Assert.Equal(new GridPosition(5, 3), before.Player);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyInputCommand(session.SessionId, GameCommand.Up));
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyInputCommand(session.SessionId, GameCommand.Interact));
        var after = app.View.CaptureSnapshot();
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
        if (playerEntersGuard) GameTestSteps.PlayTurn(session, app, GameCommand.Wait);
        GameTestSteps.PlayTurn(session, app, GameCommand.Up);
        GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var snapshot = app.View.CaptureSnapshot();
        Assert.Equal(GameOutcome.Lost, snapshot.Outcome);
        Assert.Equal(snapshot.Player, snapshot.Enemy);
        Assert.Equal(playerEntersGuard ? GameStage.PlayerPresenting : GameStage.EnemyPresenting, snapshot.Stage);
    }
}
