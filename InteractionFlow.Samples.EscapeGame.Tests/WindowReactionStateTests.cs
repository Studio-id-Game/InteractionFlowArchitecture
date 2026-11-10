using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;
using System;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class WindowReactionStateTests
{
    [Fact]
    public void ReactionRemembersTextAndDerivesButtonsFromCurrentGameState()
    {
        var state = new GameState(1)
        {
            Version = 1,
            Stage = GameStage.Selecting,
            WaitingForInput = true,
            Selection = new(ActionKind.Wait, new(1, 3)),
            Feedback = GameFeedback.Selected
        };
        var reaction = new RaylibWindowReaction();
        reaction.PrepareFrame(state, 0);
        Assert.Equal("Selected Wait; Enter to confirm", reaction.Message);
        Assert.True(reaction.CanConfirm);
        Assert.True(reaction.AcceptingInput);
        var message = reaction.Message;
        reaction.PrepareFrame(state with { Mode = ExecutionMode.Paused }, 1);
        Assert.False(reaction.AcceptingInput);
        Assert.False(reaction.CanConfirm);
        Assert.Same(message, reaction.Message);
        reaction.PrepareFrame(state with { Selection = new(ActionKind.Move, new(1, 2)) }, 0);
        Assert.Equal("Selected Move; Enter to confirm", reaction.Message);

        reaction.ForceResetMemoryState();
        Assert.Equal("Preparing room", reaction.Message);
        Assert.False(reaction.AcceptingInput);
        Assert.False(reaction.CanConfirm);
        // 同じSessionId/Versionでも、再利用先の状態から表示を作り直します。
        reaction.PrepareFrame(state with { Selection = new(ActionKind.Move, new(1, 2)) }, 0);
        Assert.Equal("Selected Move; Enter to confirm", reaction.Message);
    }

    [Fact]
    public void PresentationClockIsLocalToViewAndDiscardsPauseBoundaryTime()
    {
        var key = new PresentationKey(1, 1, 1);
        var state = new GameState(1) { Event = new(key, GameEventKind.PlayerMove, new(1, 3), new(1, 2)) };
        var clock = new PresentationClock();
        Assert.Equal(0, Advance(clock, state, 10));
        Assert.Equal(.05, Advance(clock, state, .05));
        var paused = state with { Mode = ExecutionMode.Paused, ControlRevision = 1 };
        Assert.Equal(.05, Advance(clock, paused, 10));
        var resumed = state with { ControlRevision = 2 };
        Assert.Equal(.05, Advance(clock, resumed, 10));
        Assert.Equal(.15, Advance(clock, resumed, 1));
        Assert.Equal(0, Advance(clock, resumed with
        {
            Event = new(new(1, 2, 2), GameEventKind.EnemyMove, new(3, 1), new(3, 2))
        }, 10));
    }

    private static double Advance(PresentationClock clock, GameState state, double delta)
    {
        clock.Update(state, delta);
        return clock.ElapsedSeconds;
    }

    [Fact]
    public void ClockResetDiscardsPreviousEventAndElapsedTime()
    {
        var state = new GameState(1)
        {
            Event = new(new(1, 1, 1), GameEventKind.PlayerMove, default, default)
        };
        var clock = new PresentationClock();
        Advance(clock, state, 0);
        Advance(clock, state, 1);
        Assert.True(clock.IsComplete);
        clock.Reset();
        Assert.False(clock.IsComplete);
        Assert.Equal(0, Advance(clock, state, 10));
        clock.Update(GameState.Empty, 0);
        Assert.False(clock.IsComplete);
        Assert.Equal(0, clock.DurationSeconds);
    }

    [Fact]
    public void SkipAndDurationAreViewDecisionsFromTheGameEvent()
    {
        var state = new GameState(1)
        {
            Event = new(new(1, 1, 1), GameEventKind.Unlock, default, default),
            SkipRequested = true
        };
        var shown = new PresentationClock();
        shown.Update(state, 0);
        Assert.Equal(.2, shown.DurationSeconds);
        Assert.Equal(shown.DurationSeconds, shown.ElapsedSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PresentationClock().Update(state, double.NaN));
    }
}
