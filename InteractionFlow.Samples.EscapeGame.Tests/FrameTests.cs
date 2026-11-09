using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class FrameTests
{
    [Theory]
    [InlineData(false, false, false, 1)]
    [InlineData(true, false, false, 0)]
    [InlineData(false, true, false, 0)]
    [InlineData(false, false, true, 0)]
    [InlineData(true, true, true, 0)]
    public async Task FrameControlRequestsSuppressOrdinaryInputBeforeDrawing(
        bool stop, bool pause, bool skip, int delivered)
    {
        using var context = new WindowContext();
        context.With(new FrameState());
        var operation = new FrameOperation(frame =>
        {
            frame.SessionId = 17;
            frame.Command = GameCommand.Up;
            frame.ShutdownRequested = stop;
            frame.PauseRequested = pause;
            frame.SkipRequested = skip;
        });
        var game = new RecordedGameOperation();
        var window = new FrameReaction(() => Assert.Equal(delivered, game.Commands.Count));
        var interaction = new PresentFrame(operation, window, game,
            new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.Equal(1, window.DrawCount);
        Assert.Equal(delivered, game.Commands.Count);
        if (delivered == 1)
            Assert.Equal((17L, GameCommand.Up), game.Commands[0]);
    }

    [Fact]
    public async Task PauseAndResumeControlDoesNotReplaceExistingInputWait()
    {
        using var app = new WindowContext();
        using var game = app.CreateGameContext();
        using var session = GameTestSteps.Ready(game, app);
        var pending = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();
        using var frames = app.CreateIndependentContext();
        var frame = new FrameState { SessionId = session.SessionId, PauseRequested = true };
        frames.With(frame);
        var control = new ChangeControl(new GameControlReaction(),
            new GameExceptionHandling(), new GameCancellationHandling());

        Assert.False((await control.ExecuteAsync(frames)).HasException);
        Assert.True(session.Session.GetProgress(session.SessionId).Paused);
        Assert.False(pending.IsCompleted);
        Assert.False(app.Input.TrySubmit(session.SessionId, GameCommand.Up));

        Assert.False((await control.ExecuteAsync(frames)).HasException);
        Assert.False(session.Session.GetProgress(session.SessionId).Paused);
        Assert.False(pending.IsCompleted);
        Assert.True(app.Input.TrySubmit(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await pending.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SkipCapturedForPreviousPresentationDoesNotSkipTheNextOne()
    {
        using var app = new WindowContext();
        using var game = app.CreateGameContext();
        using var session = GameTestSteps.Ready(game, app);
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(session.SessionId, GameCommand.Up));
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(session.SessionId, GameCommand.Confirm));
        var player = session.World.ApplyPlayer(session.SessionId);
        var oldKey = Assert.IsType<PresentationKey>(player.Presentation);
        var frame = new FrameState
        {
            SessionId = session.SessionId,
            SkipRequested = true,
            Presentation = oldKey
        };
        GameTestSteps.Complete(app, oldKey);
        var enemy = session.World.ApplyEnemy(session.SessionId);
        var newKey = Assert.IsType<PresentationKey>(enemy.Presentation);
        Assert.NotEqual(oldKey, newKey);
        using var frames = app.CreateIndependentContext();
        frames.With(frame);
        var control = new GameControlReaction();

        Assert.False(control.Change(frames).HasException);
        Assert.False(app.Display.Snapshot().Presentation!.SkipRequested);

        frame.Presentation = newKey;
        Assert.False(control.Change(frames).HasException);
        Assert.True(app.Display.Snapshot().Presentation!.SkipRequested);
    }

    private sealed class FrameOperation(Action<FrameState> poll) : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void Poll(WindowContext context) => poll(context.GetRequired<FrameState>());
    }

    private sealed class RecordedGameOperation : Operation, IGameOperation
    {
        public List<(long, GameCommand)> Commands { get; } = [];
        public override void ForceResetMemoryState() { }
        public ValueTask<GameCommand> ReadAsync(EscapeApplicationContext context)
            => throw new NotSupportedException();
        public bool Submit(WindowContext context, long sessionId, GameCommand command)
        {
            Commands.Add((sessionId, command));
            return true;
        }
    }

    private sealed class FrameReaction(Action onDraw) : Reaction, IWindowReaction
    {
        public int DrawCount { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(WindowContext context) => GetEnd();
        public ReactionEnd Draw(WindowContext context)
        {
            onDraw();
            DrawCount++;
            return GetEnd();
        }
        public ReactionEnd Close(WindowContext context, Result previous) => GetEnd(previous);
    }
}
