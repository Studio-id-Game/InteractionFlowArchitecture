using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Externals;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class GameplayReactionTests
{
    [Theory]
    [InlineData(GameCommand.Left)]
    [InlineData(GameCommand.Interact)]
    [InlineData(GameCommand.Confirm)]
    public async Task InvalidUserSelectionIsSuccessfulFeedbackWithoutTurnConsumption(GameCommand command)
    {
        using var fixture = new ReactionFixture();
        var before = fixture.App.View.CaptureSnapshot();

        var end = await fixture.Reaction.HandleCommandAsync(fixture.Session, command);

        Assert.False(end.HasException);
        var after = fixture.App.View.CaptureSnapshot();
        Assert.Null(after.Selection);
        Assert.Equal(before.Player, after.Player);
        Assert.Equal(before.Enemy, after.Enemy);
        Assert.Equal(before.Turn, after.Turn);
        Assert.NotEqual(before.Message, after.Message);
    }

    [Theory]
    [InlineData(nameof(GameplayReaction.BeginSessionAsync))]
    [InlineData(nameof(GameplayReaction.BeginActionSelectionAsync))]
    [InlineData(nameof(GameplayReaction.ApplyPlayerActionAsync))]
    [InlineData(nameof(GameplayReaction.ApplyEnemyActionAsync))]
    [InlineData(nameof(GameplayReaction.PresentResultAsync))]
    [InlineData(nameof(GameplayReaction.EnableResultInputAsync))]
    public async Task InvalidRequiredTransitionReturnsFailureInsteadOfSilentSuccess(string operation)
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        var reaction = fixture.Reaction;

        var end = await (operation switch
        {
            nameof(GameplayReaction.BeginSessionAsync) => reaction.BeginSessionAsync(context),
            nameof(GameplayReaction.BeginActionSelectionAsync) => reaction.BeginActionSelectionAsync(context),
            nameof(GameplayReaction.ApplyPlayerActionAsync) => reaction.ApplyPlayerActionAsync(context),
            nameof(GameplayReaction.ApplyEnemyActionAsync) => reaction.ApplyEnemyActionAsync(context),
            nameof(GameplayReaction.PresentResultAsync) => reaction.PresentResultAsync(context),
            _ => reaction.EnableResultInputAsync(context)
        });

        Assert.True(end.HasException);
        var failure = Assert.IsType<InvalidOperationException>(end.Exception);
        Assert.Contains(operation, failure.Message);
        Assert.Contains(nameof(MutationStatus.Rejected), failure.Message);
        Assert.Equal(0, fixture.App.View.CaptureSnapshot().Turn);
    }

    [Fact]
    public async Task BeginExplicitlyPublishesUninitializedSessionAndWaitsForItsInitialFrame()
    {
        using var fixture = new ReactionFixture(prepare: false);
        Assert.Equal(0, fixture.App.View.CaptureSnapshot().SessionId);

        var begin = fixture.Reaction.BeginSessionAsync(fixture.Session).AsTask();

        var snapshot = fixture.App.View.CaptureSnapshot();
        Assert.Equal(fixture.Session.SessionId, snapshot.SessionId);
        Assert.Equal(GameStage.Preparing, snapshot.Stage);
        Assert.False(begin.IsCompleted);
        Assert.NotNull(snapshot.Presentation);
        GameTestSteps.Complete(fixture.App, snapshot.Presentation.Key);
        Assert.False((await begin).HasException);
        Assert.False((await fixture.Reaction.BeginActionSelectionAsync(fixture.Session)).HasException);
        Assert.Equal(GameStage.Selecting, fixture.App.View.CaptureSnapshot().Stage);
    }

    [Fact]
    public async Task SelectionStageCannotBeginBeforeInitialPresentationCompletes()
    {
        using var fixture = new ReactionFixture(prepare: false);
        var begin = fixture.Reaction.BeginSessionAsync(fixture.Session).AsTask();

        var early = await fixture.Reaction.BeginActionSelectionAsync(fixture.Session);

        Assert.True(early.HasException);
        Assert.False(begin.IsCompleted);
        Assert.Equal(GameStage.Preparing, fixture.App.View.CaptureSnapshot().Stage);
        fixture.App.Lifetime.RequestShutdown();
        Assert.False((await begin).HasException);
    }

    [Fact]
    public async Task EnemyCannotAdvanceBeforePlayerPresentationCompletes()
    {
        using var fixture = new ReactionFixture();
        await fixture.Reaction.HandleCommandAsync(fixture.Session, GameCommand.Up);
        await fixture.Reaction.HandleCommandAsync(fixture.Session, GameCommand.Confirm);
        var player = fixture.Reaction.ApplyPlayerActionAsync(fixture.Session).AsTask();
        var before = fixture.App.View.CaptureSnapshot();

        var enemy = await fixture.Reaction.ApplyEnemyActionAsync(fixture.Session);

        Assert.True(enemy.HasException);
        Assert.False(player.IsCompleted);
        Assert.Equal(before.Enemy, fixture.App.View.CaptureSnapshot().Enemy);
        Assert.Equal(1, fixture.App.View.CaptureSnapshot().Turn);
        fixture.App.Lifetime.RequestShutdown();
        Assert.False((await player).HasException);
    }

    [Fact]
    public async Task ResultInputCannotStartBeforeResultPresentationCompletes()
    {
        using var fixture = new ReactionFixture();
        GameTestSteps.PlayTurn(fixture.Session, fixture.App, GameCommand.Up);
        GameTestSteps.PlayTurn(fixture.Session, fixture.App, GameCommand.Right);
        var result = fixture.Reaction.PresentResultAsync(fixture.Session).AsTask();
        var snapshot = fixture.App.View.CaptureSnapshot();

        Assert.True((await fixture.Reaction.EnableResultInputAsync(fixture.Session)).HasException);
        Assert.False(result.IsCompleted);
        Assert.False(snapshot.AcceptingInput);
        GameTestSteps.Complete(fixture.App, snapshot.Presentation!.Key);
        Assert.False((await result).HasException);
        Assert.False((await fixture.Reaction.EnableResultInputAsync(fixture.Session)).HasException);
    }

    [Fact]
    public async Task PausedConfirmedWorkWaitsAndAppliesOnceAfterResume()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Up);
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Confirm);
        Assert.True(fixture.App.View.TrySetPaused(context.SessionId, true));

        var execution = fixture.Reaction.ApplyPlayerActionAsync(context).AsTask();

        Assert.False(execution.IsCompleted);
        Assert.Equal(0, fixture.App.View.CaptureSnapshot().Turn);
        Assert.True(context.Gameplay.GetSessionProgress(context.SessionId).HasConfirmedAction);
        Assert.True(fixture.App.View.TrySetPaused(context.SessionId, false));
        var presentation = await WaitForPresentationCompletionAsync(fixture.App);
        Assert.Equal(1, fixture.App.View.CaptureSnapshot().Turn);
        GameTestSteps.Complete(fixture.App, presentation.Key);
        Assert.False((await execution).HasException);
        Assert.Equal(1, fixture.App.View.CaptureSnapshot().Turn);
        Assert.True((await fixture.Reaction.ApplyPlayerActionAsync(context)).HasException);
    }

    [Fact]
    public async Task StoppingReleasesPausedWorkWithoutTurningItIntoARejectedTransition()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Up);
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Confirm);
        Assert.True(fixture.App.View.TrySetPaused(context.SessionId, true));
        var execution = fixture.Reaction.ApplyPlayerActionAsync(context).AsTask();

        fixture.App.Lifetime.RequestShutdown();

        Assert.False((await execution).HasException);
        Assert.Equal(0, fixture.App.View.CaptureSnapshot().Turn);
        Assert.False((await fixture.Reaction.BeginSessionAsync(context)).HasException);
        Assert.False((await fixture.Reaction.BeginActionSelectionAsync(context)).HasException);
        Assert.False((await fixture.Reaction.HandleCommandAsync(context, GameCommand.Up)).HasException);
        Assert.False((await fixture.Reaction.ApplyEnemyActionAsync(context)).HasException);
        Assert.False((await fixture.Reaction.PresentResultAsync(context)).HasException);
        Assert.False((await fixture.Reaction.EnableResultInputAsync(context)).HasException);
    }

    [Fact]
    public async Task PresentationCancellationPropagatesWithoutRollingBackCommittedState()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Up);
        await fixture.Reaction.HandleCommandAsync(context, GameCommand.Confirm);
        var execution = fixture.Reaction.ApplyPlayerActionAsync(context).AsTask();

        context.Cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(1, fixture.App.View.CaptureSnapshot().Turn);
        Assert.Equal(new GridPosition(1, 2), fixture.App.View.CaptureSnapshot().Player);
    }

    [Fact]
    public void AppliedPresentationRequiresAKeyWhileStoppedDoesNot()
    {
        var missing = Assert.IsType<InvalidOperationException>(
            GameMutationValidation.Presentation(new(MutationStatus.Applied), "apply"));
        Assert.Contains("presentation key", missing.Message);
        Assert.Null(GameMutationValidation.Presentation(new(MutationStatus.Stopped), "stop"));
        Assert.Null(GameMutationValidation.Presentation(
            new(MutationStatus.Applied, new PresentationKey(1, 1, 1)), "apply"));
    }

    [Fact]
    public void UnexpectedMutationStatusDoesNotBecomeSuccess()
    {
        var unknown = (MutationStatus)123;
        Assert.IsType<InvalidOperationException>(GameMutationValidation.Selection(unknown, "select"));
        Assert.IsType<InvalidOperationException>(GameMutationValidation.RequiredTransition(unknown, "advance"));
        Assert.IsType<InvalidOperationException>(GameMutationValidation.Presentation(new(unknown), "present"));
    }

    private static async Task<PresentationSnapshot> WaitForPresentationCompletionAsync(EscapeApplicationContext context)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            if (context.View.CaptureSnapshot().Presentation is { } presentation)
                return presentation;
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), "The resumed work did not request presentation.");
            await Task.Yield();
        }
    }

    private sealed class ReactionFixture : IDisposable
    {
        internal EscapeApplicationContext App { get; } = new();
        internal GameplayContext Session { get; }
        internal GameplayReaction Reaction { get; } = new();

        internal ReactionFixture(bool prepare = true)
        {
            Session = prepare ? GameTestSteps.Ready(App) : GameTestSteps.AllocateSession(App);
        }

        public void Dispose()
        {
            App.Lifetime.RequestShutdown();
            Session.Dispose();
            App.Dispose();
        }
    }
}
