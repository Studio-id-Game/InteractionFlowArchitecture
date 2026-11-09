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
    public void SessionAndChildShareParentValuesAndCancellationWhileIndependentScopeDoesNot()
    {
        using var app = new EscapeApplicationContext();
        var local = new LocalValue("root");
        app.With(local);
        using var session = app.CreateSessionContext();
        using var child = new EscapeApplicationContext(session);
        using var independent = session.CreateIndependentContext();

        Assert.Same(app.World, session.World);
        Assert.Same(app.Session, child.Session);
        Assert.Same(app.Input, child.Input);
        Assert.Same(app.InputWriter, child.InputWriter);
        Assert.Same(app.Display, child.Display);
        Assert.Same(app.Presentation, child.Presentation);
        Assert.Same(app.Control, child.Control);
        Assert.Same(app.Cancellation, session.Cancellation);
        Assert.Same(app.Cancellation, child.Cancellation);
        Assert.Same(local, child.GetRequired<LocalValue>());
        Assert.NotSame(session.Cancellation, independent.Cancellation);
        Assert.Equal(session.SessionId, independent.SessionId);
        Assert.False(independent.TryGet<LocalValue>(out _));
        Assert.Same(app.World, independent.GetRequired<IGameWorldAccess>());
        Assert.Same(app.InputWriter, app.GetRequired<IGameInputWriter>());
        Assert.Same(app.Input, app.GetRequired<IGameInputReader>());
        Assert.Same(app.Display, app.GetRequired<IGameDisplayAccess>());
    }

    [Fact]
    public void IndependentFrameAndFinishingValuesDoNotLeakIntoGameExecution()
    {
        using var app = new EscapeApplicationContext();
        var local = new LocalValue("root");
        app.With(local);
        using var session = app.CreateSessionContext();
        using var frames = app.CreateIndependentContext();
        var frame = new FrameState();
        var window = new WindowState();
        frames.With(frame).With(window);
        using var shutdown = new EscapeApplicationContext(frames);
        shutdown.With(new Finishing(FinishingKind.CloseWindow, Result.Success));

        Assert.False(frames.TryGet<LocalValue>(out _));
        Assert.Same(frame, shutdown.GetRequired<FrameState>());
        Assert.Same(window, shutdown.GetRequired<WindowState>());
        Assert.Same(frames.Cancellation, shutdown.Cancellation);
        Assert.NotSame(app.Cancellation, frames.Cancellation);
        Assert.Same(local, session.GetRequired<LocalValue>());
        Assert.False(app.TryGet<FrameState>(out _));
        Assert.False(session.TryGet<FrameState>(out _));
        Assert.False(session.TryGet<WindowState>(out _));
        Assert.False(session.TryGet<Finishing>(out _));
        Assert.False(frames.TryGet<Finishing>(out _));
    }

    [Fact]
    public void ParentObjectValuesAndCancellationValueOverridesKeepTheirLookupOrder()
    {
        using var app = new EscapeApplicationContext();
        var originalCancellation = app.Cancellation;
        var local = new LocalValue("root object");
        var cancellationValue = new CancellationObject();
        app.With(cancellationValue).With<object>(local);
        using var session = app.CreateSessionContext();
        using var child = new EscapeApplicationContext(session);

        Assert.Same(local, child.GetRequired<object>());
        Assert.Same(cancellationValue, child.GetRequired<CancellationObject>());
        Assert.Same(originalCancellation, child.Cancellation);
        Assert.Same(app.Display, child.GetRequired<IGameDisplayAccess>());
        var replacement = new LocalValue("child object");
        child.With<object>(replacement);
        Assert.Same(replacement, child.GetRequired<object>());
        Assert.Same(local, app.GetRequired<object>());

        using var independent = session.CreateIndependentContext();
        Assert.False(independent.TryGet<LocalValue>(out _));
        Assert.Same(independent.Cancellation, independent.GetRequired<CancellationObject>());
        Assert.NotSame(cancellationValue, independent.GetRequired<CancellationObject>());
    }

    [Fact]
    public void ApplicationScopeDoesNotImplicitlyBecomeTheActiveSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = app.CreateSessionContext();

        Assert.Equal(EscapeApplicationContext.ApplicationSessionId, app.SessionId);
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
    public void FixedContractsAndEntryValuesCannotReplaceDedicatedContextAccess()
    {
        using var app = new EscapeApplicationContext();
        using var other = new EscapeApplicationContext();
        var replacement = new SessionOnlyAccess(other.Session);
        using var entry = new RefEntry<object>(other.World);

        Assert.Throws<ArgumentException>(() => app.With<object>(replacement));
        Assert.Throws<ArgumentException>(() => app.With<object>(other.World));
        Assert.Throws<ArgumentException>(() => app.With<object>(other.Input));
        Assert.Throws<ArgumentException>(() => app.With<object>(other.InputWriter));
        Assert.Throws<ArgumentException>(() => app.With<object>(other.Display));
        Assert.Throws<ArgumentException>(() => app.With(entry));
        Assert.Same(app.Session, app.GetRequired<IGameSessionAccess>());
        Assert.Same(app.Input, app.GetRequired<IGameInputReader>());
        Assert.Same(app.InputWriter, app.GetRequired<IGameInputWriter>());
        using var sessionChild = new FlowContext(app).With<IGameSessionAccess>(replacement);
        using var entryChild = new FlowContext(app).With(entry);
        Assert.Same(replacement, sessionChild.GetRequired<IGameSessionAccess>());
        Assert.Same(app.World, sessionChild.GetRequired<IGameWorldAccess>());
        Assert.Same(other.World, entryChild.GetRequired<IGameWorldAccess>());
        Assert.Same(app.World, app.GetRequired<IGameWorldAccess>());
    }

    [Fact]
    public async Task InputWriterCommunicatesWithSessionReaderAndRootCancellationWithoutFrames()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var read = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();

        Assert.True(app.InputWriter.TrySubmit(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await read.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(session.SessionId, GameCommand.Up));
        var next = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();
        app.Cancellation.Cancel();

        Assert.Same(app.Cancellation, session.Cancellation);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await next.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.False(app.InputWriter.TrySubmit(session.SessionId, GameCommand.Confirm));
        using var frames = app.CreateIndependentContext();
        Assert.False(frames.Cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedCancellationResetIsVisibleToRootAndSessionButNotIndependentFrames(bool cancelFromSession)
    {
        using var app = new EscapeApplicationContext();
        using var frames = app.CreateIndependentContext();
        using var session = GameTestSteps.Ready(app);
        var original = app.Cancellation.GetToken();
        var read = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken()).AsTask();
        var source = cancelFromSession ? session.Cancellation : app.Cancellation;

        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True((await source.WaitForTasksAndResetCancellationAsync()).Try(out _));

        Assert.True(original.IsCancellationRequested);
        Assert.False(app.Cancellation.IsCancellationRequested);
        Assert.False(session.Cancellation.IsCancellationRequested);
        Assert.False(frames.Cancellation.IsCancellationRequested);
        Assert.NotEqual(original, app.Cancellation.GetToken());
        var retry = session.Input.ReadAsync(session.SessionId, session.Cancellation.GetToken());
        Assert.True(app.InputWriter.TrySubmit(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await retry);
    }

    [Fact]
    public void DisposedContextRejectsNewAcquisitionWithoutStoppingSharedScopes()
    {
        using var app = new EscapeApplicationContext();
        using var independent = app.CreateIndependentContext();
        var cancellation = app.Cancellation;
        var sessionAccess = app.Session;

        app.Dispose();

        Assert.Throws<ObjectDisposedException>(() => app.Input);
        Assert.Throws<ObjectDisposedException>(() => app.InputWriter);
        Assert.Throws<ObjectDisposedException>(() => app.Session);
        Assert.Throws<ObjectDisposedException>(() => app.World);
        Assert.Throws<ObjectDisposedException>(() => app.Display);
        Assert.Throws<ObjectDisposedException>(() => app.Presentation);
        Assert.Throws<ObjectDisposedException>(() => app.Control);
        Assert.Throws<ObjectDisposedException>(() => app.CreateSessionContext());
        Assert.Throws<ObjectDisposedException>(() => app.CreateIndependentContext());
        Assert.Throws<ObjectDisposedException>(() => new EscapeApplicationContext(app));
        Assert.Throws<ObjectDisposedException>(() => app.GetRequired<IGameDisplayAccess>());
        Assert.Same(cancellation, app.Cancellation);
        Assert.False(cancellation.IsCancellationRequested);
        Assert.Same(sessionAccess, independent.Session);
        Assert.False(independent.Session.ShutdownRequested);
        using var session = independent.CreateSessionContext();
        Assert.Equal(MutationStatus.Applied, session.Session.BeginSession(session.SessionId).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposingChildOrParentDoesNotOwnSharedStateOrCancellation(bool disposeParent)
    {
        using var app = new EscapeApplicationContext();
        using var child = new EscapeApplicationContext(app);
        var surviving = disposeParent ? child : app;
        var discarded = disposeParent ? app : child;
        var cancellation = app.Cancellation;

        discarded.Dispose();

        Assert.Same(cancellation, surviving.Cancellation);
        Assert.False(cancellation.IsCancellationRequested);
        using var session = surviving.CreateSessionContext();
        Assert.Equal(MutationStatus.Applied, session.Session.BeginSession(session.SessionId).Status);
        Assert.Equal(session.SessionId, surviving.Display.Snapshot().SessionId);
        Assert.False(surviving.Session.ShutdownRequested);
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
