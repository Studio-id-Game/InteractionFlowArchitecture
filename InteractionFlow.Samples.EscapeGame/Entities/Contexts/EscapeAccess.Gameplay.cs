namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal sealed partial class EscapeAccess
{
    public MutationStatus ApplyInputCommand(long sessionId, GameCommand command)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return status;
            var applied = WorldRules.ApplyInputCommand(command, session!.Progress, session.Player,
                session.Inventory, session.Mechanisms, out var message);
            session.Display.Set(message);
            return applied;
        }
    }

    public MutationResult ApplyPlayerAction(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return new(status);
            var effect = WorldRules.ApplyPlayer(session!.Progress, session.Player,
                session.Inventory, session.Mechanisms, session.Enemy);
            return PublishEffect(session, effect);
        }
    }

    public MutationResult ApplyEnemyAction(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return new(status);
            if (!PresentationFinished(session!))
                return new(MutationStatus.Rejected);
            return PublishEffect(session!, WorldRules.ApplyEnemy(session!.Progress, session.Player, session.Enemy));
        }
    }

    private MutationResult PublishEffect(SessionRuntime session, ActionEffect? effect)
    {
        if (effect is not { } value)
            return new(MutationStatus.Rejected);
        session.Display.Set(value.Message);
        return Present(session, value.Kind, value.From, value.To, value.DurationSeconds);
    }

    public MutationStatus BeginActionSelection(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return status;
            if (session!.Progress.Stage is not (GameStage.Preparing or GameStage.EnemyPresenting)
                || session.Progress.Outcome != GameOutcome.Running || !PresentationFinished(session)) return MutationStatus.Rejected;
            session.Progress.Stage = GameStage.Selecting;
            session.Progress.Selection = null;
            session.Progress.Confirmed = false;
            session.Presentation = null;
            session.Display.Set("Choose an action, then confirm");
            return MutationStatus.Applied;
        }
    }

    public MutationResult BeginResultPresentation(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return new(status);
            if (session!.Progress.Outcome == GameOutcome.Running || !PresentationFinished(session)
                || session.Progress.Stage == GameStage.Result) return new(MutationStatus.Rejected);
            session.Progress.Stage = GameStage.Result;
            session.Display.Set(session.Progress.Outcome == GameOutcome.Won ? "Escaped! R to retry" : "Caught! R to retry");
            return Present(session, PresentationKind.Result, session.Player.Position, session.Player.Position, 0);
        }
    }

    public MutationStatus EnableResultInput(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            var status = CanMutate(session);
            if (status != MutationStatus.Applied)
                return status;
            if (session!.Progress.Stage != GameStage.Result || !PresentationFinished(session))
                return MutationStatus.Rejected;
            session.Progress.ResultInputEnabled = true;
            return MutationStatus.Applied;
        }
    }

}
