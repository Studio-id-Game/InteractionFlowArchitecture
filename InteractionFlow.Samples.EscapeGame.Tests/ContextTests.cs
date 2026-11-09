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
    public void HiddenAccessDoesNotFallBackEvenWhenParentValuesAreExplicitlyInherited()
    {
        using var app = new EscapeApplicationContext();
        app.With("inherited");
        using var game = new GameplayContext(app, ContextInheritance.All);
        using var view = new GameViewContext(app, ContextInheritance.All);
        Assert.Equal("inherited", game.GetRequired<string>());
        Assert.Equal("inherited", view.GetRequired<string>());
        Assert.False(game.TryGet<IGameViewAccess>(out _));
        Assert.False(view.TryGet<IGameplayAccess>(out _));
    }

    [Fact]
    public void ConstructorsRejectNullSources()
    {
        Assert.Throws<ArgumentNullException>(() => new GameplayContext(null!, ContextInheritance.None));
        Assert.Throws<ArgumentNullException>(() => new GameplayContext(null!, 0, ContextInheritance.All));
        Assert.Throws<ArgumentNullException>(() => new GameViewContext(null!, ContextInheritance.Cancellation));
    }

    [Fact]
    public void RoleContextsShareAccessButOnlyExposeTheirOwnLookupContracts()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);

        Assert.Same(app.Gameplay, game.GetRequired<IGameplayAccess>());
        Assert.Same(app.View, view.GetRequired<IGameViewAccess>());
        Assert.Same(app.Lifetime, game.GetRequired<IApplicationLifetimeAccess>());
        Assert.Same(app.Lifetime, view.GetRequired<IApplicationLifetimeAccess>());
        Assert.Same(app.Gameplay, app.View);
        Assert.Same(app.Gameplay, app.Lifetime);
        Assert.False(game.TryGet<IGameViewAccess>(out _));
        Assert.False(view.TryGet<IGameplayAccess>(out _));
        Assert.False(app.TryGet<EscapeAccess>(out _));
        Assert.False(game.TryGet<EscapeAccess>(out _));
        Assert.False(view.TryGet<EscapeAccess>(out _));
    }

    [Fact]
    public void GameSharesRootCancellationWithoutRootValuesAndFramesStayIndependent()
    {
        using var app = new EscapeApplicationContext();
        app.With(new LocalValue("application"));
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        game.With(new LocalValue("game"));
        using var session = new GameplayContext(game, game.Gameplay.AllocateSession(), ContextInheritance.All);
        using var frames = new GameViewContext(app, ContextInheritance.None);
        var frame = new FrameState();
        var window = new WindowState();
        frames.With(frame).With(window);
        using var close = new GameViewContext(frames, ContextInheritance.All);
        close.With(new CleanupInfo(CleanupKind.CloseWindow, Result.Success));
        using var cleanup = new GameplayContext(session, ContextInheritance.None);

        Assert.Same(app.Cancellation, game.Cancellation);
        Assert.Same(game.Cancellation, session.Cancellation);
        Assert.NotSame(app.Cancellation, frames.Cancellation);
        Assert.Same(frames.Cancellation, close.Cancellation);
        Assert.NotSame(session.Cancellation, cleanup.Cancellation);
        Assert.Equal(session.SessionId, cleanup.SessionId);
        Assert.Equal("game", session.GetRequired<LocalValue>().Name);
        Assert.False(frames.TryGet<LocalValue>(out _));
        Assert.False(cleanup.TryGet<LocalValue>(out _));
        Assert.False(session.TryGet<FrameState>(out _));
        Assert.False(session.TryGet<WindowState>(out _));
        Assert.False(session.TryGet<CleanupInfo>(out _));
        Assert.Same(frame, close.GetRequired<FrameState>());
        Assert.Same(window, close.GetRequired<WindowState>());
        Assert.False(frames.TryGet<CleanupInfo>(out _));
    }

    [Fact]
    public void RootValuesDoNotEscapeThroughEitherRoleContext()
    {
        using var app = new EscapeApplicationContext();
        var rootValue = new LocalValue("root");
        app.With<object>(rootValue).With(new CancellationObject());
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);
        Assert.NotSame(rootValue, game.GetRequired<object>());
        Assert.Same(app.Cancellation, game.GetRequired<CancellationObject>());
        Assert.Same(view.Cancellation, view.GetRequired<CancellationObject>());
        Assert.False(game.TryGet<LocalValue>(out _));
        Assert.False(view.TryGet<LocalValue>(out _));
    }

    [Fact]
    public void ConstructorsAcceptOnlyTheDeclaredInterfaceAndDoNotRequireConcreteParents()
    {
        using var app = new EscapeApplicationContext();
        using var source = new GameplaySource(app);
        using var viewSource = new ViewSource(app);
        using var game = new GameplayContext(source, ContextInheritance.Cancellation);
        using var view = new GameViewContext(viewSource, ContextInheritance.None);
        Assert.Same(source.Gameplay, game.Gameplay);
        Assert.Same(source.Cancellation, game.Cancellation);
        Assert.Equal(source.SessionId, game.SessionId);
        Assert.Same(viewSource.View, view.View);
        Assert.NotSame(viewSource.Cancellation, view.Cancellation);
    }

    [Fact]
    public void SameRoleParentsPreserveAdditionalValueAndCancellationValueOverrides()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        var local = new LocalValue("parent");
        var cancellationValue = new CancellationObject();
        game.With(cancellationValue).With<object>(local);
        using var child = new GameplayContext(game, ContextInheritance.All);
        Assert.Same(local, child.GetRequired<object>());
        Assert.Same(cancellationValue, child.GetRequired<CancellationObject>());
        Assert.Same(app.Cancellation, child.Cancellation);
        child.With<object>(new LocalValue("child"));
        Assert.Equal("child", ((LocalValue)child.GetRequired<object>()).Name);
        Assert.Same(local, game.GetRequired<LocalValue>());
        Assert.False(child.TryGet<IGameViewAccess>(out _));
    }

    [Fact]
    public void ApplicationAndGameRootsDoNotFollowThePublishedSessionId()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var session = new GameplayContext(game, game.Gameplay.AllocateSession(), ContextInheritance.All);
        Assert.Equal(0, app.SessionId);
        Assert.Equal(0, game.SessionId);
        Assert.Equal(0, app.View.CaptureSnapshot().SessionId);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.BeginSession(session.SessionId).Status);
        Assert.Equal(session.SessionId, app.View.CaptureSnapshot().SessionId);
        Assert.Equal(0, app.SessionId);
        Assert.Equal(0, game.SessionId);
        Assert.True(game.Gameplay.GetSessionProgress(game.SessionId).Stopping);
    }

    [Fact]
    public void FixedAndHiddenContractsAndEntriesCannotBeInjectedIntoRoleContexts()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);
        using var entry = new RefEntry<object>(app.View);
        var lifetime = new LifetimeOnlyAccess(app.Lifetime);
        foreach (var context in new FlowContext[] { app, game, view })
        {
            Assert.Throws<ArgumentException>(() => context.With<object>(app.Gameplay));
            Assert.Throws<ArgumentException>(() => context.With<object>(app.View));
            Assert.Throws<ArgumentException>(() => context.With<object>(lifetime));
            Assert.Throws<ArgumentException>(() => context.With(entry));
        }
        using var ordinaryChild = new FlowContext(game).With<IApplicationLifetimeAccess>(lifetime);
        Assert.Same(lifetime, ordinaryChild.GetRequired<IApplicationLifetimeAccess>());
        Assert.Same(game.Lifetime, game.GetRequired<IApplicationLifetimeAccess>());
    }

    [Fact]
    public async Task ViewInputReachesGameplayAndSharedCancellationCanBeResetFromEitherSide()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);
        using var session = GameTestSteps.Ready(app);
        var read = session.Gameplay.ReadCommandAsync(session.SessionId, session.Cancellation.GetToken());
        Assert.True(view.View.TrySubmitCommand(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await read);
        var token = game.Cancellation.GetToken();
        var next = session.Gameplay.ReadCommandAsync(session.SessionId, token).AsTask();
        app.Cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next);
        Assert.False(view.Cancellation.IsCancellationRequested);
        Assert.True(game.Cancellation.IsCancellationRequested);
        Assert.True((await game.Cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));
        Assert.False(app.Cancellation.IsCancellationRequested);
        Assert.True(token.IsCancellationRequested);
        game.Cancellation.Cancel();
        Assert.True(app.Cancellation.IsCancellationRequested);
        await app.Cancellation.WaitForTasksAndResetCancellationAsync();
        Assert.False(game.Cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposingSourceOrRoleContextDoesNotStopOtherScopes(bool disposeSource)
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);
        var gameplay = game.Gameplay;
        var lifetime = game.Lifetime;
        if (disposeSource) app.Dispose();
        else game.Dispose();
        var id = gameplay.AllocateSession();
        Assert.Equal(GameUpdateStatus.Applied, gameplay.BeginSession(id).Status);
        Assert.Equal(id, view.View.CaptureSnapshot().SessionId);
        Assert.False(lifetime.ShutdownRequested);
        Assert.False(game.Cancellation.IsCancellationRequested);
    }

    [Fact]
    public void DisposedContextsRejectPropertiesLookupsAndNewChildren()
    {
        using var app = new EscapeApplicationContext();
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        using var view = new GameViewContext(app, ContextInheritance.None);
        app.Dispose();
        game.Dispose();
        view.Dispose();
        Assert.Throws<ObjectDisposedException>(() => app.Gameplay);
        Assert.Throws<ObjectDisposedException>(() => app.View);
        Assert.Throws<ObjectDisposedException>(() => app.Lifetime);
        Assert.Throws<ObjectDisposedException>(() => game.Gameplay);
        Assert.Throws<ObjectDisposedException>(() => game.Lifetime);
        Assert.Throws<ObjectDisposedException>(() => view.View);
        Assert.Throws<ObjectDisposedException>(() => view.Lifetime);
        foreach (var context in new FlowContext[] { app, game, view })
        {
            Assert.Throws<ObjectDisposedException>(() => context.With("value"));
            Assert.Throws<ObjectDisposedException>(() => context.TryGet<IApplicationLifetimeAccess>(out _));
        }
        Assert.Throws<ObjectDisposedException>(() => new GameplayContext(game, ContextInheritance.None));
        Assert.Throws<ObjectDisposedException>(() => new GameViewContext(view, ContextInheritance.None));
    }

    private sealed class GameplaySource(IGameplayContext parent) : FlowContext, IGameplayContext
    {
        public long SessionId => parent.SessionId;
        public IGameplayAccess Gameplay => parent.Gameplay;
        public IApplicationLifetimeAccess Lifetime => parent.Lifetime;
    }

    private sealed class ViewSource(IGameViewContext parent) : FlowContext, IGameViewContext
    {
        public IGameViewAccess View => parent.View;
        public IApplicationLifetimeAccess Lifetime => parent.Lifetime;
    }

    private sealed class LifetimeOnlyAccess(IApplicationLifetimeAccess inner) : IApplicationLifetimeAccess
    {
        public bool ShutdownRequested => inner.ShutdownRequested;
        public void RequestShutdown() => inner.RequestShutdown();
    }

    private sealed record LocalValue(string Name);
}
