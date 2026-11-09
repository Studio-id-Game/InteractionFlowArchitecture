namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal sealed partial class EscapeAccess
{
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

    private GameUpdateResult PublishEffect(SessionRuntime session, ActionEffect? effect)
    {
        if (effect is not { } value)
            return new(GameUpdateStatus.Rejected);
        session.Display.SetMessage(value.Message);
        return BeginPresentation(session, value.Kind, value.From, value.To, value.DurationSeconds);
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

}
