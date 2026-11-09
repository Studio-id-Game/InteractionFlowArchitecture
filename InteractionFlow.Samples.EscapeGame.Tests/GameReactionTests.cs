using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Externals;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class GameReactionTests
{
    [Theory]
    [InlineData(GameCommand.Left)]
    [InlineData(GameCommand.Interact)]
    [InlineData(GameCommand.Confirm)]
    public async Task InvalidUserSelectionIsSuccessfulFeedbackWithoutTurnConsumption(GameCommand command)
    {
        using var fixture = new ReactionFixture();
        var before = fixture.App.Display.Snapshot();

        var end = await fixture.Reaction.SelectAsync(fixture.Session, command);

        Assert.False(end.HasException);
        var after = fixture.App.Display.Snapshot();
        Assert.Null(after.Selection);
        Assert.Equal(before.Player, after.Player);
        Assert.Equal(before.Enemy, after.Enemy);
        Assert.Equal(before.Turn, after.Turn);
        Assert.NotEqual(before.Message, after.Message);
    }

    [Theory]
    [InlineData(nameof(GameReaction.BeginAsync))]
    [InlineData(nameof(GameReaction.EnableSelectionAsync))]
    [InlineData(nameof(GameReaction.ApplyPlayerAsync))]
    [InlineData(nameof(GameReaction.ApplyEnemyAsync))]
    [InlineData(nameof(GameReaction.ShowResultAsync))]
    [InlineData(nameof(GameReaction.EnableResultInputAsync))]
    public async Task InvalidRequiredTransitionReturnsFailureInsteadOfSilentSuccess(string operation)
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        var reaction = fixture.Reaction;

        var end = await (operation switch
        {
            nameof(GameReaction.BeginAsync) => reaction.BeginAsync(context),
            nameof(GameReaction.EnableSelectionAsync) => reaction.EnableSelectionAsync(context),
            nameof(GameReaction.ApplyPlayerAsync) => reaction.ApplyPlayerAsync(context),
            nameof(GameReaction.ApplyEnemyAsync) => reaction.ApplyEnemyAsync(context),
            nameof(GameReaction.ShowResultAsync) => reaction.ShowResultAsync(context),
            _ => reaction.EnableResultInputAsync(context)
        });

        Assert.True(end.HasException);
        var failure = Assert.IsType<InvalidOperationException>(end.Exception);
        Assert.Contains(operation, failure.Message);
        Assert.Contains(nameof(MutationStatus.Rejected), failure.Message);
        Assert.Equal(0, fixture.App.Display.Snapshot().Turn);
    }

    [Fact]
    public async Task BeginExplicitlyPublishesUninitializedSessionAndWaitsForItsInitialFrame()
    {
        using var fixture = new ReactionFixture(prepare: false);
        Assert.Equal(0, fixture.App.World.ActiveSessionId);

        var begin = fixture.Reaction.BeginAsync(fixture.Session).AsTask();

        var snapshot = fixture.App.Display.Snapshot();
        Assert.Equal(fixture.Session.SessionId, snapshot.SessionId);
        Assert.Equal(GameStage.Preparing, snapshot.Stage);
        Assert.False(begin.IsCompleted);
        Assert.NotNull(snapshot.Presentation);
        DomainTests.Complete(fixture.Session, snapshot.Presentation.Key);
        Assert.False((await begin).HasException);
        Assert.False((await fixture.Reaction.EnableSelectionAsync(fixture.Session)).HasException);
        Assert.Equal(GameStage.Selecting, fixture.App.Display.Snapshot().Stage);
    }

    [Fact]
    public async Task SelectionStageCannotBeginBeforeInitialPresentationCompletes()
    {
        using var fixture = new ReactionFixture(prepare: false);
        var begin = fixture.Reaction.BeginAsync(fixture.Session).AsTask();

        var early = await fixture.Reaction.EnableSelectionAsync(fixture.Session);

        Assert.True(early.HasException);
        Assert.False(begin.IsCompleted);
        Assert.Equal(GameStage.Preparing, fixture.App.Display.Snapshot().Stage);
        fixture.App.World.RequestShutdown();
        Assert.False((await begin).HasException);
    }

    [Fact]
    public async Task EnemyCannotAdvanceBeforePlayerPresentationCompletes()
    {
        using var fixture = new ReactionFixture();
        await fixture.Reaction.SelectAsync(fixture.Session, GameCommand.Up);
        await fixture.Reaction.SelectAsync(fixture.Session, GameCommand.Confirm);
        var player = fixture.Reaction.ApplyPlayerAsync(fixture.Session).AsTask();
        var before = fixture.App.Display.Snapshot();

        var enemy = await fixture.Reaction.ApplyEnemyAsync(fixture.Session);

        Assert.True(enemy.HasException);
        Assert.False(player.IsCompleted);
        Assert.Equal(before.Enemy, fixture.App.Display.Snapshot().Enemy);
        Assert.Equal(1, fixture.App.Display.Snapshot().Turn);
        fixture.App.World.RequestShutdown();
        Assert.False((await player).HasException);
    }

    [Fact]
    public async Task ResultInputCannotStartBeforeResultPresentationCompletes()
    {
        using var fixture = new ReactionFixture();
        DomainTests.PlayTurn(fixture.Session, GameCommand.Up);
        DomainTests.PlayTurn(fixture.Session, GameCommand.Right);
        var result = fixture.Reaction.ShowResultAsync(fixture.Session).AsTask();
        var snapshot = fixture.App.Display.Snapshot();

        Assert.True((await fixture.Reaction.EnableResultInputAsync(fixture.Session)).HasException);
        Assert.False(result.IsCompleted);
        Assert.False(snapshot.AcceptingInput);
        DomainTests.Complete(fixture.Session, snapshot.Presentation!.Key);
        Assert.False((await result).HasException);
        Assert.False((await fixture.Reaction.EnableResultInputAsync(fixture.Session)).HasException);
    }

    [Fact]
    public async Task PausedConfirmedWorkWaitsAndAppliesOnceAfterResume()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.SelectAsync(context, GameCommand.Up);
        await fixture.Reaction.SelectAsync(context, GameCommand.Confirm);
        Assert.True(context.Control.TrySetPaused(context.SessionId, true));

        var execution = fixture.Reaction.ApplyPlayerAsync(context).AsTask();

        Assert.False(execution.IsCompleted);
        Assert.Equal(0, fixture.App.Display.Snapshot().Turn);
        Assert.True(context.World.GetProgress(context.SessionId).HasConfirmedAction);
        Assert.True(context.Control.TrySetPaused(context.SessionId, false));
        var presentation = await WaitForPresentationAsync(fixture.App);
        Assert.Equal(1, fixture.App.Display.Snapshot().Turn);
        DomainTests.Complete(context, presentation.Key);
        Assert.False((await execution).HasException);
        Assert.Equal(1, fixture.App.Display.Snapshot().Turn);
        Assert.True((await fixture.Reaction.ApplyPlayerAsync(context)).HasException);
    }

    [Fact]
    public async Task StoppingReleasesPausedWorkWithoutTurningItIntoARejectedTransition()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.SelectAsync(context, GameCommand.Up);
        await fixture.Reaction.SelectAsync(context, GameCommand.Confirm);
        Assert.True(context.Control.TrySetPaused(context.SessionId, true));
        var execution = fixture.Reaction.ApplyPlayerAsync(context).AsTask();

        fixture.App.World.RequestShutdown();

        Assert.False((await execution).HasException);
        Assert.Equal(0, fixture.App.Display.Snapshot().Turn);
        Assert.False((await fixture.Reaction.BeginAsync(context)).HasException);
        Assert.False((await fixture.Reaction.EnableSelectionAsync(context)).HasException);
        Assert.False((await fixture.Reaction.SelectAsync(context, GameCommand.Up)).HasException);
        Assert.False((await fixture.Reaction.ApplyEnemyAsync(context)).HasException);
        Assert.False((await fixture.Reaction.ShowResultAsync(context)).HasException);
        Assert.False((await fixture.Reaction.EnableResultInputAsync(context)).HasException);
    }

    [Fact]
    public async Task PresentationCancellationPropagatesWithoutRollingBackCommittedState()
    {
        using var fixture = new ReactionFixture();
        var context = fixture.Session;
        await fixture.Reaction.SelectAsync(context, GameCommand.Up);
        await fixture.Reaction.SelectAsync(context, GameCommand.Confirm);
        var execution = fixture.Reaction.ApplyPlayerAsync(context).AsTask();

        context.Cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(1, fixture.App.Display.Snapshot().Turn);
        Assert.Equal(new GridPosition(1, 2), fixture.App.Display.Snapshot().Player);
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

    private static async Task<PresentationSnapshot> WaitForPresentationAsync(EscapeApplicationContext context)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            if (context.Display.Snapshot().Presentation is { } presentation)
                return presentation;
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), "The resumed work did not request presentation.");
            await Task.Yield();
        }
    }

    private sealed class ReactionFixture : IDisposable
    {
        internal EscapeApplicationContext App { get; } = new();
        internal EscapeApplicationContext Session { get; }
        internal GameReaction Reaction { get; } = new();

        internal ReactionFixture(bool prepare = true)
            => Session = prepare ? DomainTests.Ready(App) : App.CreateSessionContext();

        public void Dispose()
        {
            App.World.RequestShutdown();
            Session.Dispose();
            App.Dispose();
        }
    }
}
