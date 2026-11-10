using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class WindowReactionStateTests
{
    [Fact]
    public void ReactionRemembersTextAndDerivesButtonsFromCurrentGameState()
    {
        var state = new GameInfo<GameViewInfo>(new(1)
        {
            Stage = GameStage.Selecting,
            Selection = new(ActionKind.Wait, new(1, 3))
        }, new(WaitingForInput: true));
        var reaction = new RaylibWindowReaction();
        reaction.PrepareFrame(state, 0);
        Assert.Equal("Selected Wait; Enter to confirm", reaction.Message);
        Assert.True(reaction.CanConfirm);
        Assert.True(reaction.AcceptingInput);
        var message = reaction.Message;
        reaction.PrepareFrame(state with { Info = state.Info with { Mode = ExecutionMode.Paused } }, 1);
        Assert.False(reaction.AcceptingInput);
        Assert.False(reaction.CanConfirm);
        Assert.Same(message, reaction.Message);
        reaction.PrepareFrame(state with { State = state.State with { Selection = new(ActionKind.Move, new(1, 2)) } }, 0);
        Assert.Equal("Selected Move; Enter to confirm", reaction.Message);

        reaction.ForceResetMemoryState();
        Assert.Equal("Preparing room", reaction.Message);
        Assert.False(reaction.AcceptingInput);
        Assert.False(reaction.CanConfirm);
        // 同じSessionId/Versionでも、再利用先の状態から表示を作り直します。
        reaction.PrepareFrame(state with { State = state.State with { Selection = new(ActionKind.Move, new(1, 2)) } }, 0);
        Assert.Equal("Selected Move; Enter to confirm", reaction.Message);
    }

    [Theory]
    [InlineData(GameCommand.CancelSelection, "Selection canceled")]
    [InlineData(GameCommand.Left, "That action is not valid")]
    [InlineData(GameCommand.Confirm, "That action is not valid")]
    [InlineData(GameCommand.Interact, "No usable mechanism (switch and key are required for door)")]
    public async Task InputFeedbackSurvivesInputWaitAndPauseUntilTheNextValidSelection(
        GameCommand command, string expected)
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var reaction = new RaylibWindowReaction();
        if (command == GameCommand.CancelSelection)
            app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, command));
        var input = app.Gameplay.ReadCommandAsync(id, session.Cancellation.GetToken()).AsTask();
        reaction.PrepareFrame(app.View.ReadGameState(), 0);
        Assert.Equal(expected, reaction.Message);
        Assert.Equal(0, app.View.ReadGameState().State.Turn);

        Assert.True(app.Gameplay.SetPaused(id, true));
        reaction.PrepareFrame(app.View.ReadGameState(), 0);
        Assert.Equal(expected, reaction.Message);
        Assert.True(app.Gameplay.SetPaused(id, false));
        reaction.PrepareFrame(app.View.ReadGameState(), 0);
        Assert.Equal(expected, reaction.Message);

        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        var next = await input;
        app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, next));
        reaction.PrepareFrame(app.View.ReadGameState(), 0);
        Assert.Null(app.View.ReadGameState().Info.Feedback);
        Assert.Equal("Selected Move; Enter to confirm", reaction.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProgressMessagesFollowTheGameAndResultUntilRetry(bool win)
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.CreateSessionContext(app);
        var id = session.SessionId;
        var reaction = new RaylibWindowReaction();
        void Show(string expected)
        {
            reaction.PrepareFrame(app.View.ReadGameState(), 0);
            Assert.Equal(expected, reaction.Message);
        }

        Show("Preparing room");
        var initial = app.Gameplay.BeginSession(id, WorldRules.BeginSession);
        Show("Find the key, activate the switch, unlock the door");
        GameTestSteps.Complete(app, initial.Presentation!.Value);
        Show("Find the key, activate the switch, unlock the door");
        app.Gameplay.UpdateGameState(id, (state, _, _, completed) => WorldRules.BeginActionSelection(state, completed));
        Show("Choose an action, then confirm");

        GameCommand[] commands = win
            ? [GameCommand.Up, GameCommand.Up, GameCommand.Down, GameCommand.Down,
                GameCommand.Right, GameCommand.Interact, GameCommand.Right, GameCommand.Right, GameCommand.Right,
                GameCommand.Interact, GameCommand.Up, GameCommand.Up]
            : [GameCommand.Right, GameCommand.Up];
        foreach (var command in commands)
        {
            app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, command));
            var action = app.View.ReadGameState().State.Selection!.Value;
            Show("Selected " + action.Kind + "; Enter to confirm");
            app.Gameplay.UpdateGameState(id,
                (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));
            Show("Action confirmed");
            var player = app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyPlayer(state));
            Show("Player: " + action.Kind);

            // 段階外の入力結果はイベント文言より優先し、描画完了通知でも消えません。
            app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
            Show("This stage does not accept selection");
            GameTestSteps.Complete(app, player.Presentation!.Value);
            Show("This stage does not accept selection");
            if (app.View.ReadGameState().State.Outcome != GameOutcome.Running)
                break;

            var enemy = app.Gameplay.UpdateGameState(id,
                (state, _, _, completed) => WorldRules.ApplyEnemy(state, completed));
            Show("Guard moves");
            GameTestSteps.Complete(app, enemy.Presentation!.Value);
            if (app.View.ReadGameState().State.Outcome != GameOutcome.Running)
                break;
            app.Gameplay.UpdateGameState(id,
                (state, _, _, completed) => WorldRules.BeginActionSelection(state, completed));
            Show("Choose an action, then confirm");
        }

        Assert.Equal(win ? GameOutcome.Won : GameOutcome.Lost, app.View.ReadGameState().State.Outcome);
        var result = app.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.BeginResultPresentation(state, completed));
        var message = win ? "Escaped! R to retry" : "Caught! R to retry";
        Show(message);
        Assert.Null(app.View.ReadGameState().Info.Feedback);
        GameTestSteps.Complete(app, result.Presentation!.Value);
        app.Gameplay.UpdateGameState(id, (state, _, _, completed) => WorldRules.EnableResultInput(state, completed));
        Show(message);
        app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Retry));
        Show("Retrying");
        app.Gameplay.EndSession(id);
        Show("Retrying");
        using var retry = GameTestSteps.Ready(app);
        Show("Choose an action, then confirm");
        Assert.Null(app.View.ReadGameState().Info.Feedback);
    }

    [Fact]
    public void PresentationClockIsLocalToViewAndDiscardsPauseBoundaryTime()
    {
        var key = new PresentationKey(1, 1, 1);
        var state = new GameInfo<GameViewInfo>(new(1), new(Event: new(key, GameEventKind.PlayerMove, new(1, 3), new(1, 2))));
        var clock = new PresentationClock();
        Assert.Equal(0, Advance(clock, state, 10));
        Assert.Equal(.05, Advance(clock, state, .05));
        var paused = state with { Info = state.Info with { Mode = ExecutionMode.Paused, ControlRevision = 1 } };
        Assert.Equal(.05, Advance(clock, paused, 10));
        var resumed = state with { Info = state.Info with { ControlRevision = 2 } };
        Assert.Equal(.05, Advance(clock, resumed, 10));
        Assert.Equal(.15, Advance(clock, resumed, 1));
        Assert.Equal(0, Advance(clock, resumed with
        {
            Info = resumed.Info with { Event = new(new(1, 2, 2), GameEventKind.EnemyMove, new(3, 1), new(3, 2)) }
        }, 10));
    }

    private static double Advance(PresentationClock clock, GameInfo<GameViewInfo> state, double delta)
    {
        clock.Update(state, delta);
        return clock.ElapsedSeconds;
    }

    [Fact]
    public void ClockResetDiscardsPreviousEventAndElapsedTime()
    {
        var state = new GameInfo<GameViewInfo>(new(1),
            new(Event: new(new(1, 1, 1), GameEventKind.PlayerMove, default, default)));
        var clock = new PresentationClock();
        Advance(clock, state, 0);
        Advance(clock, state, 1);
        Assert.True(clock.IsComplete);
        clock.Reset();
        Assert.False(clock.IsComplete);
        Assert.Equal(0, Advance(clock, state, 10));
        clock.Update(new(GameState.Empty, new()), 0);
        Assert.False(clock.IsComplete);
        Assert.Equal(0, clock.DurationSeconds);
    }

    [Fact]
    public void SkipAndDurationAreViewDecisionsFromTheGameEvent()
    {
        var state = new GameInfo<GameViewInfo>(new(1),
            new(Event: new(new(1, 1, 1), GameEventKind.Unlock, default, default), SkipRequested: true));
        var shown = new PresentationClock();
        shown.Update(state, 0);
        Assert.Equal(.2, shown.DurationSeconds);
        Assert.Equal(shown.DurationSeconds, shown.ElapsedSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PresentationClock().Update(state, double.NaN));
    }
}
