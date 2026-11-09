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
        Assert.Same(app.Input, session.Input);
        Assert.Same(session.Cancellation, game.Cancellation);
        Assert.Same(session.Cancellation, child.Cancellation);
        Assert.NotSame(app.Cancellation, game.Cancellation);
        Assert.NotSame(session.Cancellation, sibling.Cancellation);
        Assert.Equal(session.SessionId, sibling.SessionId);
        Assert.False(game.TryGet<FrameState>(out _));
        Assert.Same(session.World, session.GetRequired<IGameWorldAccess>());
        Assert.Same(session.Presentation, session.GetRequired<IGamePresentationAccess>());
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
        app.Dispose();
        Assert.Throws<ObjectDisposedException>(() => app.World);
        Assert.Throws<ObjectDisposedException>(() => app.CreateIndependentContext());
        Assert.Throws<ObjectDisposedException>(() => app.CreateSessionContext());
        Assert.Same(shared, game.World);
        using var session = game.CreateSessionContext();
        Assert.Equal(MutationStatus.Applied, session.World.BeginSession(session.SessionId).Status);
        Assert.False(game.World.ShutdownRequested);
    }
}
