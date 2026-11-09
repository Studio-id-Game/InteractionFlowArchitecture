using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class ContextTests
{
    [Fact]
    public void DedicatedContextsImplementIFlowContextWithoutInheritingFlowContext()
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();

        Assert.IsAssignableFrom<IFlowContext>(window);
        Assert.IsAssignableFrom<IFlowContext>(game);
        Assert.False(typeof(FlowContext).IsAssignableFrom(typeof(WindowContext)));
        Assert.False(typeof(FlowContext).IsAssignableFrom(typeof(EscapeApplicationContext)));
    }

    [Fact]
    public void SameKindParentsPreserveObjectValuesAndCancellationValueOverrides()
    {
        using var window = new WindowContext();
        var originalCancellation = window.Cancellation;
        var windowValue = new LocalValue("window object");
        var windowCancellationValue = new CancellationObject();
        window.With(windowCancellationValue).With<object>(windowValue);
        using var child = new WindowContext(window);

        Assert.Same(windowValue, child.GetRequired<object>());
        Assert.Same(windowCancellationValue, child.GetRequired<CancellationObject>());
        Assert.Same(originalCancellation, child.Cancellation);
        Assert.Same(window.Display, child.GetRequired<IGameDisplayAccess>());

        using var game = window.CreateGameContext();
        Assert.Same(originalCancellation, game.Cancellation);
        Assert.Same(originalCancellation, game.GetRequired<CancellationObject>());
        Assert.NotSame(windowValue, game.GetRequired<object>());
        var gameValue = new LocalValue("game object");
        var gameCancellationValue = new CancellationObject();
        game.With(gameCancellationValue).With<object>(gameValue);
        using var gameChild = new EscapeApplicationContext(game);

        Assert.Same(gameValue, gameChild.GetRequired<object>());
        Assert.Same(gameCancellationValue, gameChild.GetRequired<CancellationObject>());
        Assert.Same(originalCancellation, gameChild.Cancellation);
        Assert.Same(game.World, gameChild.GetRequired<IGameWorldAccess>());
        var childValue = new LocalValue("child object");
        gameChild.With<object>(childValue);
        Assert.Same(childValue, gameChild.GetRequired<object>());
        Assert.Same(gameValue, game.GetRequired<object>());
    }

    [Fact]
    public void WindowAndGameExposeDifferentContracts()
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();

        Assert.Same(window.Input, window.GetRequired<IGameInputWriter>());
        Assert.Same(game.Input, game.GetRequired<IGameInputReader>());
        Assert.Same(window.Display, window.GetRequired<IGameDisplayAccess>());
        Assert.Same(game.World, game.GetRequired<IGameWorldAccess>());
        Assert.False(window.TryGet<IGameWorldAccess>(out _));
        Assert.False(window.TryGet<IGameInputReader>(out _));
        Assert.False(game.TryGet<IGameInputWriter>(out _));
        Assert.False(game.TryGet<IGameDisplayAccess>(out _));
        Assert.Null(typeof(WindowContext).GetProperty("World"));
        Assert.Null(typeof(EscapeApplicationContext).GetProperty("Display"));
    }

    [Fact]
    public void FixedContractsCannotBeAddedToTheWrongSideEvenAsObjectOrEntry()
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();
        using var entry = new RefEntry<object>(game.World);

        Assert.Throws<ArgumentException>(() => window.With<object>(game.World));
        Assert.Throws<ArgumentException>(() => window.With<object>(game.Input));
        Assert.Throws<ArgumentException>(() => window.With(entry));
        Assert.Throws<ArgumentException>(() => game.With<object>(window.Display));
        Assert.Throws<ArgumentException>(() => game.With<object>(window.Input));
        Assert.Throws<ArgumentException>(() => game.With(entry));
        Assert.False(window.TryGet<IGameWorldAccess>(out _));
        Assert.False(window.TryGet<IGameInputReader>(out _));
        Assert.False(game.TryGet<IGameDisplayAccess>(out _));
        Assert.False(game.TryGet<IGameInputWriter>(out _));
    }

    [Fact]
    public void DisposedWindowRejectsNewAccessAndContextFactories()
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();
        var sessionAccess = game.Session;

        window.Dispose();

        Assert.Throws<ObjectDisposedException>(() => window.Input);
        Assert.Throws<ObjectDisposedException>(() => window.Session);
        Assert.Throws<ObjectDisposedException>(() => window.Display);
        Assert.Throws<ObjectDisposedException>(() => window.Presentation);
        Assert.Throws<ObjectDisposedException>(() => window.Control);
        Assert.Throws<ObjectDisposedException>(() => window.CreateGameContext());
        Assert.Throws<ObjectDisposedException>(() => window.CreateIndependentContext());
        Assert.Throws<ObjectDisposedException>(() => new WindowContext(window));
        Assert.Throws<ObjectDisposedException>(() => window.GetRequired<IGameDisplayAccess>());
        Assert.Same(sessionAccess, game.Session);
        Assert.False(game.Session.ShutdownRequested);
    }

    [Fact]
    public void WindowValuesDoNotCrossTheGameFactoryBoundaryWhileCancellationIsShared()
    {
        using var window = new WindowContext();
        var frame = new FrameState();
        var native = new WindowState();
        var finishing = new Finishing(FinishingKind.CloseWindow, Result.Success);
        window.With(frame).With(native).With(finishing).With(new LocalValue("window"));
        using var windowChild = new WindowContext(window);
        using var independentWindow = window.CreateIndependentContext();
        using var game = window.CreateGameContext();
        Assert.False(game.TryGet<LocalValue>(out _));
        game.With(new LocalValue("game"));
        using var session = game.CreateSessionContext();
        using var gameChild = new EscapeApplicationContext(session);
        using var independentGame = session.CreateIndependentContext();

        Assert.Same(window.Cancellation, windowChild.Cancellation);
        Assert.Same(frame, windowChild.GetRequired<FrameState>());
        Assert.Same(native, windowChild.GetRequired<WindowState>());
        Assert.False(independentWindow.TryGet<FrameState>(out _));
        Assert.NotSame(window.Cancellation, independentWindow.Cancellation);
        Assert.Same(window.Cancellation, game.Cancellation);
        Assert.Same(game.Cancellation, session.Cancellation);
        Assert.Same(game.Cancellation, gameChild.Cancellation);
        Assert.NotSame(session.Cancellation, independentGame.Cancellation);
        Assert.False(game.TryGet<FrameState>(out _));
        Assert.False(game.TryGet<WindowState>(out _));
        Assert.False(game.TryGet<Finishing>(out _));
        Assert.Equal("game", gameChild.GetRequired<LocalValue>().Name);
        Assert.False(independentGame.TryGet<LocalValue>(out _));
    }

    [Fact]
    public async Task WindowCancellationDirectlyCancelsGameReaderWithoutFrameExecution()
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();
        using var session = GameTestSteps.Ready(game, window);
        var read = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();

        window.Cancellation.Cancel();

        Assert.Same(window.Cancellation, game.Cancellation);
        Assert.True(game.Cancellation.IsCancellationRequested);
        Assert.True(session.Cancellation.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.False(window.Input.TrySubmit(session.SessionId, GameCommand.Up));
        Assert.True(window.Cancellation.IsCancellationRequested);
        using var frames = window.CreateIndependentContext();
        Assert.False(frames.Cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedCancellationRecoveryResetsBothSidesWithoutAffectingIndependentFrames(bool cancelFromGame)
    {
        using var window = new WindowContext();
        using var frames = window.CreateIndependentContext();
        using var game = window.CreateGameContext();
        using var session = GameTestSteps.Ready(game, window);
        var original = window.Cancellation.GetToken();
        var read = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();

        var source = cancelFromGame ? game.Cancellation : window.Cancellation;
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True((await source.WaitForTasksAndResetCancellationAsync()).Try(out _));

        Assert.True(original.IsCancellationRequested);
        Assert.Same(window.Cancellation, game.Cancellation);
        Assert.False(window.Cancellation.IsCancellationRequested);
        Assert.False(game.Cancellation.IsCancellationRequested);
        Assert.False(frames.Cancellation.IsCancellationRequested);
        Assert.NotEqual(original, window.Cancellation.GetToken());
        var nextRead = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken());
        Assert.True(window.Input.TrySubmit(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await nextRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposingOneSideDoesNotStopTheOtherOrItsExistingSession(bool disposeWindow)
    {
        using var window = new WindowContext();
        using var game = window.CreateGameContext();
        using var session = game.CreateSessionContext();
        var display = window.Display;
        var cancellation = window.Cancellation;
        if (disposeWindow)
            window.Dispose();
        else
            game.Dispose();

        Assert.Equal(MutationStatus.Applied, session.Session.BeginSession(session.SessionId).Status);
        Assert.Equal(session.SessionId, display.Snapshot().SessionId);
        Assert.False(session.Session.ShutdownRequested);
        Assert.Same(cancellation, session.Cancellation);
        Assert.False(cancellation.IsCancellationRequested);
        using var sibling = session.CreateIndependentContext();
        Assert.Same(session.World, sibling.World);
    }

    [Fact]
    public void ScopeSharesAccessButSeparatesCancellationOnlyForIndependentRoots()
    {
        using var app = new WindowContext();
        app.With(new FrameState());
        using var game = app.CreateGameContext();
        using var session = game.CreateSessionContext();
        using var sibling = session.CreateIndependentContext();
        using var child = new EscapeApplicationContext(session);
        Assert.Same(game.World, session.World);
        Assert.Same(app.Session, session.Session);
        Assert.Same(app.Session, game.World);
        Assert.Same(app.Input, session.Input);
        Assert.Same(session.Cancellation, game.Cancellation);
        Assert.Same(session.Cancellation, child.Cancellation);
        Assert.Same(app.Cancellation, game.Cancellation);
        Assert.NotSame(session.Cancellation, sibling.Cancellation);
        Assert.Equal(session.SessionId, sibling.SessionId);
        Assert.False(game.TryGet<FrameState>(out _));
        Assert.Same(session.World, session.GetRequired<IGameWorldAccess>());
        Assert.Same(session.Session, session.GetRequired<IGameSessionAccess>());
        Assert.Same(session.Presentation, session.GetRequired<IGamePresentationAccess>());
    }

    [Fact]
    public void ApplicationScopeDoesNotImplicitlyBecomeTheActiveSession()
    {
        using var app = new EscapeApplicationContext();
        using var game = app.CreateIndependentContext();
        using var session = game.CreateSessionContext();

        Assert.Equal(EscapeApplicationContext.ApplicationSessionId, app.SessionId);
        Assert.Equal(EscapeApplicationContext.ApplicationSessionId, game.SessionId);
        Assert.Equal(EscapeApplicationContext.ApplicationSessionId, app.Session.ActiveSessionId);
        Assert.True(session.SessionId > EscapeApplicationContext.ApplicationSessionId);
        Assert.Equal(MutationStatus.Applied, session.Session.BeginSession(session.SessionId).Status);
        Assert.Equal(session.SessionId, app.Session.ActiveSessionId);
        Assert.True(app.Session.GetProgress(app.SessionId).Stopping);
        Assert.Equal(GameStage.Preparing, session.Session.GetProgress(session.SessionId).Stage);

        using var sameTarget = session.CreateIndependentContext();
        Assert.Equal(session.SessionId, sameTarget.SessionId);
        Assert.NotSame(session.Cancellation, sameTarget.Cancellation);
        Assert.Equal(EscapeApplicationContext.ApplicationSessionId, app.SessionId);
    }

    [Fact]
    public void DedicatedContextRejectsSessionContractEvenWithoutWorldInterface()
    {
        using var app = new EscapeApplicationContext();
        using var other = new EscapeApplicationContext();
        var replacement = new SessionOnlyAccess(other.Session);

        Assert.Throws<ArgumentException>(() => app.With<object>(replacement));
        Assert.Same(app.Session, app.GetRequired<IGameSessionAccess>());
        using var child = new FlowContext(app).With<IGameSessionAccess>(replacement);
        Assert.Same(replacement, child.GetRequired<IGameSessionAccess>());
        Assert.Same(app.World, child.GetRequired<IGameWorldAccess>());
    }

    [Fact]
    public void FixedAccessAndMutableEntryOverridesAreRejectedOnlyByDedicatedContext()
    {
        using var app = new EscapeApplicationContext();
        using var other = new EscapeApplicationContext();
        using var entry = new RefEntry<object>(other.World);
        Assert.Throws<ArgumentException>(() => app.With<object>(other.World));
        Assert.Throws<ArgumentException>(() => app.With(entry));
        using var child = new FlowContext(app).With(entry);
        Assert.Same(other.World, child.GetRequired<IGameWorldAccess>());
        Assert.Same(app.World, app.GetRequired<IGameWorldAccess>());
    }

    [Fact]
    public void DisposingAContextDoesNotStopSharedApplicationAccess()
    {
        using var app = new EscapeApplicationContext();
        using var game = app.CreateIndependentContext();
        var shared = app.World;
        var sharedSession = app.Session;
        app.Dispose();
        Assert.Throws<ObjectDisposedException>(() => app.World);
        Assert.Throws<ObjectDisposedException>(() => app.Session);
        Assert.Throws<ObjectDisposedException>(() => app.CreateIndependentContext());
        Assert.Throws<ObjectDisposedException>(() => app.CreateSessionContext());
        Assert.Same(shared, game.World);
        Assert.Same(sharedSession, game.Session);
        using var session = game.CreateSessionContext();
        Assert.Equal(MutationStatus.Applied, session.Session.BeginSession(session.SessionId).Status);
        Assert.False(game.Session.ShutdownRequested);
    }

    private sealed class SessionOnlyAccess(IGameSessionAccess inner) : IGameSessionAccess
    {
        public long ActiveSessionId => inner.ActiveSessionId;
        public bool ShutdownRequested => inner.ShutdownRequested;
        public GameProgress GetProgress(long sessionId) => inner.GetProgress(sessionId);
        public MutationResult BeginSession(long sessionId) => inner.BeginSession(sessionId);
        public void EndSession(long sessionId) => inner.EndSession(sessionId);
        public void RequestShutdown() => inner.RequestShutdown();
    }

    private sealed record LocalValue(string Name);
}
