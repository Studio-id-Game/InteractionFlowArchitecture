using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    public long AllocateSession()
    {
        lock (gate)
        {
            if (shutdown)
                throw new InvalidOperationException("The application is stopping.");
            var id = ++nextSession;
            sessions.Add(id, new SessionRuntime(id));
            return id;
        }
    }

    public GameUpdateResult BeginSession(long sessionId)
    {
        lock (gate)
        {
            if (shutdown || !sessions.TryGetValue(sessionId, out var session) || session.Progress.Retired)
                return new(GameUpdateStatus.Stopped);
            if (session.Progress.Published || active is { Progress.Retired: false })
                return new(GameUpdateStatus.Rejected);
            active = session;
            session.Progress.Published = true;
            session.Display.SetMessage("Find the key, activate the switch, unlock the door");
            return BeginPresentation(session, PresentationKind.Initial, session.Player.Position, session.Player.Position, 0);
        }
    }

    public void EndSession(long sessionId)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(sessionId, out var session)) return;
            StopSession(session);
            sessions.Remove(sessionId);
        }
    }

    public GameUpdateStatus ApplyInputCommand(long sessionId, GameCommand command)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return status;
            var applied = WorldRules.ApplyInputCommand(command, session!.Progress, session.Player,
                session.Inventory, session.Mechanisms, out var message);
            session.Display.SetMessage(message);
            return applied;
        }
    }

    public GameUpdateResult ApplyPlayerAction(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return new(status);
            var effect = WorldRules.ApplyPlayer(session!.Progress, session.Player,
                session.Inventory, session.Mechanisms, session.Enemy);
            return PublishEffect(session, effect);
        }
    }

    public GameUpdateResult ApplyEnemyAction(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return new(status);
            if (!IsPresentationComplete(session!))
                return new(GameUpdateStatus.Rejected);
            return PublishEffect(session!, WorldRules.ApplyEnemy(session!.Progress, session.Player, session.Enemy));
        }
    }

    public GameUpdateStatus BeginActionSelection(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return status;
            if (session!.Progress.Stage is not (GameStage.Preparing or GameStage.EnemyPresenting)
                || session.Progress.Outcome != GameOutcome.Running || !IsPresentationComplete(session)) return GameUpdateStatus.Rejected;
            session.Progress.Stage = GameStage.Selecting;
            session.Progress.Selection = null;
            session.Progress.Confirmed = false;
            session.Presentation = null;
            session.Display.SetMessage("Choose an action, then confirm");
            return GameUpdateStatus.Applied;
        }
    }

    public GameUpdateResult BeginResultPresentation(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return new(status);
            if (session!.Progress.Outcome == GameOutcome.Running || !IsPresentationComplete(session)
                || session.Progress.Stage == GameStage.Result) return new(GameUpdateStatus.Rejected);
            session.Progress.Stage = GameStage.Result;
            session.Display.SetMessage(session.Progress.Outcome == GameOutcome.Won ? "Escaped! R to retry" : "Caught! R to retry");
            return BeginPresentation(session, PresentationKind.Result, session.Player.Position, session.Player.Position, 0);
        }
    }

    public GameUpdateStatus EnableResultInput(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            var status = GetUpdateStatus(session);
            if (status != GameUpdateStatus.Applied)
                return status;
            if (session!.Progress.Stage != GameStage.Result || !IsPresentationComplete(session))
                return GameUpdateStatus.Rejected;
            session.Progress.ResultInputEnabled = true;
            return GameUpdateStatus.Applied;
        }
    }

    public async ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token)
    {
        SessionRuntime session;
        TaskCompletionSource<GameCommand> source;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            session = FindActiveSession(sessionId)!;
            if (session is null)
                return GameCommand.Stop;
            if (session.Input is not null)
                throw new InvalidOperationException("Only one input wait is supported.");
            if (!StageAllowsInput(session))
                throw new InvalidOperationException("This stage does not accept game input.");
            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            session.Input = source;
            session.InputToken = token;
        }
        try { return await source.Task.WaitAsync(token).ConfigureAwait(false); }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(session.Input, source))
                {
                    session.Input = null;
                    session.InputToken = default;
                }
            }
        }
    }

    public async ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token)
    {
        while (true)
        {
            Task<bool> resume;
            lock (gate)
            {
                token.ThrowIfCancellationRequested();
                var session = FindActiveSession(sessionId);
                if (session is null || session.Mode == ExecutionMode.Stopping)
                    return false;
                if (!IsPaused(session))
                    return true;
                resume = session.Resume!.Task;
            }
            if (!await resume.WaitAsync(token).ConfigureAwait(false))
                return false;
        }
    }

    public async ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(
        PresentationKey key, CancellationToken token)
    {
        Task<PresentationCompletion> task;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            var session = FindActiveSession(key.SessionId);
            if (session?.Presentation is not { } presentation || presentation.Snapshot.Key != key)
                return PresentationCompletion.Stopped;
            task = presentation.Completion.Task;
        }
        return await task.WaitAsync(token).ConfigureAwait(false);
    }
}
