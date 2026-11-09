using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class ContextTests
{
    [Fact]
    public void ScopeSharesAccessButSeparatesCancellationOnlyForIndependentRoots()
    {
        using var app = new EscapeApplicationContext();
        app.With(new FrameState());
        using var game = app.CreateIndependentContext();
        using var session = game.CreateSessionContext();
        using var sibling = session.CreateIndependentContext();
        using var child = new EscapeApplicationContext(session);
        Assert.Same(app.World, session.World);
        Assert.Same(app.Session, session.Session);
        Assert.Same(app.Session, app.World);
        Assert.Same(app.Input, session.Input);
        Assert.Same(session.Cancellation, game.Cancellation);
        Assert.Same(session.Cancellation, child.Cancellation);
        Assert.NotSame(app.Cancellation, game.Cancellation);
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
}
