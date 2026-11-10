using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
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
        Assert.Equal(GameOutcome.Won, snapshot.State.Outcome);
        Assert.Equal(12, snapshot.State.Turn);
        Assert.Equal(snapshot.State.Room.Exit, snapshot.State.Player);
        Assert.True(snapshot.State.KeyCollected);
        Assert.False(snapshot.State.HasKey);
        Assert.True(snapshot.State.SwitchActivated);
        Assert.True(snapshot.State.DoorUnlocked);
        Assert.Equal(3, snapshot.State.EnemyPatrolIndex);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, completed) => WorldRules.ApplyEnemy(state, completed)).Status);
    }

    [Fact]
    public void SelectionCancellationAndInvalidActionsDoNotConsumeATurn()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var initial = app.View.ReadGameState();
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up)).Status);
        Assert.NotNull(app.View.ReadGameState().State.Selection);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.CancelSelection)).Status);
        Assert.Null(app.View.ReadGameState().State.Selection);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Left)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Status);
        var current = app.View.ReadGameState();
        Assert.Equal(initial.State.Player, current.State.Player);
        Assert.Equal(initial.State.EnemyPatrolIndex, current.State.EnemyPatrolIndex);
        Assert.Equal(0, current.State.Turn);
        Assert.False(current.State.KeyCollected);
    }

    [Fact]
    public void ClosedDoorAndUnqualifiedUnlockRemainInvalid()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        for (var i = 0; i < 4; i++) GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var before = app.View.ReadGameState();
        Assert.Equal(new GridPosition(5, 3), before.State.Player);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up)).Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Interact)).Status);
        var after = app.View.ReadGameState();
        Assert.False(after.State.DoorUnlocked);
        Assert.Equal(before.State.Turn, after.State.Turn);
        Assert.Equal(before.State.EnemyPatrolIndex, after.State.EnemyPatrolIndex);
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
        Assert.Equal(GameOutcome.Lost, snapshot.State.Outcome);
        Assert.Equal(snapshot.State.Player, snapshot.State.Room.PatrolAt(snapshot.State.EnemyPatrolIndex));
        Assert.Equal(playerEntersGuard ? GameStage.PlayerPresenting : GameStage.EnemyPresenting, snapshot.State.Stage);
    }
}
