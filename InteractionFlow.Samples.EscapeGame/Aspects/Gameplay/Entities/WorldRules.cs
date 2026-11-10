using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;

/// <summary>
/// ゲーム規則を評価し、操作の成立可否と次状態を返します。保留・停止等の受付判断は共有アクセスが担います。
/// </summary>
public static class WorldRules
{
    public static GameStateChange BeginSession(GameState state)
        => Present(state, GameEventKind.Initial, state.Player, state.Player);

    public static GameStateChange BeginActionSelection(GameState state)
    {
        if (state.Stage is not (GameStage.Preparing or GameStage.EnemyPresenting)
            || state.Outcome != GameOutcome.Running || !state.PresentationCompleted)
            return Rejected(state);
        return Changed(state with { Stage = GameStage.Selecting, Selection = null, Confirmed = false,
            Event = null, Feedback = null });
    }

    public static GameStateChange ApplyInputCommand(GameState state, GameCommand command)
    {
        if (state.Stage == GameStage.Result && state.ResultInputEnabled && command == GameCommand.Retry)
            return Changed(state with { RetryRequested = true, Feedback = null });
        if (state.Stage != GameStage.Selecting || state.Outcome != GameOutcome.Running || state.Confirmed)
            return Rejected(state with { Feedback = InputFeedback.StageDoesNotAcceptSelection });
        if (command == GameCommand.CancelSelection)
            return Changed(state with { Selection = null, Feedback = InputFeedback.SelectionCanceled });
        if (command == GameCommand.Confirm)
        {
            if (state.Selection is not { } action || !IsValid(state, action))
                return Rejected(state with { Selection = null, Feedback = InputFeedback.InvalidSelection });
            return Changed(state with { Confirmed = true, Feedback = null });
        }
        var position = state.Player;
        GameAction? selected = command switch
        {
            GameCommand.Up => new(ActionKind.Move, position with { Y = position.Y - 1 }),
            GameCommand.Right => new(ActionKind.Move, position with { X = position.X + 1 }),
            GameCommand.Down => new(ActionKind.Move, position with { Y = position.Y + 1 }),
            GameCommand.Left => new(ActionKind.Move, position with { X = position.X - 1 }),
            GameCommand.Wait => new(ActionKind.Wait, position),
            GameCommand.Interact when position.DistanceTo(state.Room.Switch) == 1 && !state.SwitchActivated
                => new(ActionKind.Switch, state.Room.Switch),
            GameCommand.Interact when position.DistanceTo(state.Room.Door) == 1 && !state.DoorUnlocked
                => new(ActionKind.Unlock, state.Room.Door),
            _ => null
        };
        return selected is { } proposed && IsValid(state, proposed)
            ? Changed(state with { Selection = proposed, Feedback = null })
            : Rejected(state with { Selection = null, Feedback = command == GameCommand.Interact
                ? InputFeedback.NoUsableMechanism : InputFeedback.InvalidSelection });
    }

    public static GameStateChange ApplyPlayer(GameState state)
    {
        if (state.Stage != GameStage.Selecting || !state.Confirmed || state.Selection is not { } action
            || !IsValid(state, action))
            return Rejected(state);
        var from = state.Player;
        var kind = GameEventKind.Wait;
        switch (action.Kind)
        {
            case ActionKind.Move:
                var collect = action.Target == state.Room.Key && !state.KeyCollected;
                state = state with { Player = action.Target, HasKey = state.HasKey || collect,
                    KeyCollected = state.KeyCollected || collect };
                kind = GameEventKind.PlayerMove;
                break;
            case ActionKind.Switch:
                state = state with { SwitchActivated = true };
                kind = GameEventKind.Switch;
                break;
            case ActionKind.Unlock:
                state = state with { HasKey = false, DoorUnlocked = true };
                kind = GameEventKind.Unlock;
                break;
        }
        state = state with { Turn = state.Turn + 1, Selection = null, Confirmed = false,
            Stage = GameStage.PlayerPresenting };
        return Present(EvaluateOutcome(state), kind, from, state.Player);
    }

    public static GameStateChange ApplyEnemy(GameState state)
    {
        if (state.Stage != GameStage.PlayerPresenting || state.Outcome != GameOutcome.Running
            || !state.PresentationCompleted)
            return Rejected(state);
        var from = state.Room.PatrolAt(state.EnemyPatrolIndex);
        state = state with { EnemyPatrolIndex = (state.EnemyPatrolIndex + 1) % state.Room.PatrolLength,
            Stage = GameStage.EnemyPresenting };
        return Present(EvaluateOutcome(state), GameEventKind.EnemyMove, from, state.Room.PatrolAt(state.EnemyPatrolIndex));
    }

    public static GameStateChange BeginResultPresentation(GameState state)
    {
        if (state.Outcome == GameOutcome.Running || !state.PresentationCompleted || state.Stage == GameStage.Result)
            return Rejected(state);
        return Present(state with { Stage = GameStage.Result },
            GameEventKind.Result, state.Player, state.Player);
    }

    public static GameStateChange EnableResultInput(GameState state)
        => state.Stage == GameStage.Result && state.PresentationCompleted
            ? Changed(state with { ResultInputEnabled = true }) : Rejected(state);

    public static GameStateChange ApplyControl(GameState state, GameControlInput input)
    {
        if (input.Command == GameControlCommand.TogglePause
            && state.Stage is GameStage.Selecting or GameStage.PlayerPresenting or GameStage.EnemyPresenting)
            return Changed(state with { Mode = state.Mode == ExecutionMode.Paused ? ExecutionMode.Active : ExecutionMode.Paused,
                ControlRevision = state.ControlRevision + 1 });
        if (input.Command == GameControlCommand.Skip && state.Mode == ExecutionMode.Active
            && state.Stage is GameStage.PlayerPresenting or GameStage.EnemyPresenting
            && state.Event is { } current && current.Key == input.Presentation && !state.PresentationCompleted)
            return Changed(state with { SkipRequested = true });
        return Rejected(state);
    }

    private static bool IsValid(GameState state, GameAction action)
        => action.Kind switch
        {
            ActionKind.Move => state.Player.DistanceTo(action.Target) == 1 && state.Room.CanEnter(action.Target, state.DoorUnlocked),
            ActionKind.Switch => action.Target == state.Room.Switch && state.Player.DistanceTo(action.Target) == 1
                && !state.SwitchActivated,
            ActionKind.Unlock => action.Target == state.Room.Door && state.Player.DistanceTo(action.Target) == 1
                && !state.DoorUnlocked && state.SwitchActivated && state.HasKey,
            ActionKind.Wait => action.Target == state.Player,
            _ => false
        };

    private static GameState EvaluateOutcome(GameState state)
        => state.Outcome != GameOutcome.Running ? state
            : state.Player == state.Room.PatrolAt(state.EnemyPatrolIndex) ? state with { Outcome = GameOutcome.Lost }
            : state.Player == state.Room.Exit ? state with { Outcome = GameOutcome.Won } : state;

    private static GameStateChange Present(
        GameState state, GameEventKind kind, GridPosition from, GridPosition to)
    {
        var key = new PresentationKey(state.SessionId, state.Version + 1, state.Version + 1);
        // 新しい提示は直前の操作結果を置き換えます。通常の文言はViewが状態から導きます。
        return Changed(state with { Event = new(key, kind, from, to), PresentationCompleted = false,
            SkipRequested = false, Feedback = null });
    }

    private static GameStateChange Changed(GameState state)
    {
        state = state with { Version = state.Version + 1 };
        return new(true, state);
    }

    private static GameStateChange Rejected(GameState state)
        => new(false, state with { Version = state.Version + 1 });
}
