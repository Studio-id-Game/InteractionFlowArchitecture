using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;
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
        using var app = new EscapeApplicationContext();
        using var context = new ViewContext(app, ContextInheritance.None);
        context.With(new FrameState());
        var operation = new FrameOperation(frame =>
        {
            frame.SessionId = 17;
            frame.Command = GameCommand.Up;
            frame.ShutdownRequested = stop;
            frame.PauseRequested = pause;
            frame.SkipRequested = skip;
        });
        var game = new RecordedGameInputSubmissionOperation();
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
    public async Task ControlIsOnlyInputUntilGameplayConsumesIt()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var pending = session.Gameplay.ReadCommandAsync(id, session.Cancellation.GetToken()).AsTask();
        var control = new HandleControlInput(new GameplayReaction(), new GameExceptionHandling(), new GameCancellationHandling());
        Assert.True(app.View.TrySubmitControl(new(id, GameControlCommand.TogglePause)));
        Assert.Equal(ExecutionMode.Active, app.View.ReadGameState().Mode);
        Assert.False((await control.ExecuteAsync(session)).HasException);
        Assert.Equal(ExecutionMode.Paused, app.View.ReadGameState().Mode);
        Assert.False(pending.IsCompleted);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.True(app.View.TrySubmitControl(new(id, GameControlCommand.TogglePause)));
        Assert.False((await control.ExecuteAsync(session)).HasException);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GameplayRejectsSkipForPreviousPresentation()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        session.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));
        var player = session.Gameplay.UpdateGameState(id, WorldRules.ApplyPlayer).Presentation!.Value;
        GameTestSteps.Complete(app, player);
        var enemy = session.Gameplay.UpdateGameState(id, WorldRules.ApplyEnemy).Presentation!.Value;
        var control = new GameplayReaction();
        Assert.True(app.View.TrySubmitControl(new(id, GameControlCommand.Skip, player)));
        Assert.False(control.ApplyControlRequest(session).HasException);
        Assert.False(app.View.ReadGameState().SkipRequested);
        Assert.True(app.View.TrySubmitControl(new(id, GameControlCommand.Skip, enemy)));
        Assert.False(control.ApplyControlRequest(session).HasException);
        Assert.True(app.View.ReadGameState().SkipRequested);
    }

    private sealed class FrameOperation(Action<FrameState> poll) : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void PollFrameInput(IViewContext context) => poll(context.GetRequired<FrameState>());
    }

    private sealed class RecordedGameInputSubmissionOperation : Operation, IGameInputSubmissionOperation
    {
        public List<(long, GameCommand)> Commands { get; } = [];
        public bool TrySubmitControl(IViewContext context, GameControlInput input) => true;
        public override void ForceResetMemoryState() { }
        public bool TrySubmitCommand(IViewContext context, long sessionId, GameCommand command)
        {
            Commands.Add((sessionId, command));
            return true;
        }
    }

    private sealed class FrameReaction(Action onDraw) : Reaction, IWindowReaction
    {
        public int DrawCount { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(IViewContext context) => GetEnd();
        public ReactionEnd DrawFrame(IViewContext context)
        {
            onDraw();
            DrawCount++;
            return GetEnd();
        }
        public ReactionEnd Close(ILifetimeContext context, Result previous) => GetEnd(previous);
    }
}
