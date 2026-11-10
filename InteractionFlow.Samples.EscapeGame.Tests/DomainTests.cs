using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
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
        var snapshot = app.View.ReadGameState();
        Assert.Equal(GameOutcome.Won, snapshot.Outcome);
        Assert.Equal(12, snapshot.Turn);
        Assert.Equal(snapshot.Room.Exit, snapshot.Player);
        Assert.True(snapshot.KeyCollected);
        Assert.False(snapshot.HasKey);
        Assert.True(snapshot.SwitchActivated);
        Assert.True(snapshot.DoorUnlocked);
        Assert.Equal(3, snapshot.EnemyPatrolIndex);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId, WorldRules.ApplyEnemy).Status);
    }

    [Fact]
    public void SelectionCancellationAndInvalidActionsDoNotConsumeATurn()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var initial = app.View.ReadGameState();
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.Up)).Status);
        Assert.NotNull(app.View.ReadGameState().Selection);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.CancelSelection)).Status);
        Assert.Null(app.View.ReadGameState().Selection);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.Left)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.Confirm)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id, WorldRules.ApplyPlayer).Status);
        var current = app.View.ReadGameState();
        Assert.Equal(initial.Player, current.Player);
        Assert.Equal(initial.EnemyPatrolIndex, current.EnemyPatrolIndex);
        Assert.Equal(0, current.Turn);
        Assert.False(current.KeyCollected);
    }

    [Fact]
    public void ClosedDoorAndUnqualifiedUnlockRemainInvalid()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        for (var i = 0; i < 4; i++) GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var before = app.View.ReadGameState();
        Assert.Equal(new GridPosition(5, 3), before.Player);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId, state => WorldRules.ApplyInputCommand(state, GameCommand.Up)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId, state => WorldRules.ApplyInputCommand(state, GameCommand.Interact)).Status);
        var after = app.View.ReadGameState();
        Assert.False(after.DoorUnlocked);
        Assert.Equal(before.Turn, after.Turn);
        Assert.Equal(before.EnemyPatrolIndex, after.EnemyPatrolIndex);
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
        var snapshot = app.View.ReadGameState();
        Assert.Equal(GameOutcome.Lost, snapshot.Outcome);
        Assert.Equal(snapshot.Player, snapshot.Room.PatrolAt(snapshot.EnemyPatrolIndex));
        Assert.Equal(playerEntersGuard ? GameStage.PlayerPresenting : GameStage.EnemyPresenting, snapshot.Stage);
    }
}
