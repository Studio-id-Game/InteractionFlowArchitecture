using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;

/// <summary>
/// ゲーム規則を評価し、操作の成立可否と次状態を返します。保留・停止等の受付判断は共有アクセスが担います。
/// </summary>
public static class WorldRules
{
    public static GameInfo<GameChangeInfo> BeginSession(GameState state)
        => Present(state, GameEventKind.Initial, state.Player, state.Player);

    public static GameInfo<GameChangeInfo> BeginActionSelection(GameState state, bool presentationCompleted)
    {
        if (state.Stage is not (GameStage.Preparing or GameStage.EnemyPresenting)
            || state.Outcome != GameOutcome.Running || !presentationCompleted)
            return Rejected(state);
        return new(state with { Stage = GameStage.Selecting, Selection = null, Confirmed = false },
            new(true) { ReplacePresentation = true, ReplaceFeedback = true });
    }

    public static GameInfo<GameChangeInfo> ApplyInputCommand(GameState state, GameCommand command)
    {
        if (state.Stage == GameStage.Result && state.ResultInputEnabled && command == GameCommand.Retry)
            return Changed(state with { RetryRequested = true }, replaceFeedback: true);
        if (state.Stage != GameStage.Selecting || state.Outcome != GameOutcome.Running || state.Confirmed)
            return Rejected(state, InputFeedback.StageDoesNotAcceptSelection);
        if (command == GameCommand.CancelSelection)
            return Changed(state with { Selection = null }, replaceFeedback: true,
                feedback: InputFeedback.SelectionCanceled);
        if (command == GameCommand.Confirm)
        {
            if (state.Selection is not { } action || !IsValid(state, action))
                return Rejected(state with { Selection = null }, InputFeedback.InvalidSelection);
            return Changed(state with { Confirmed = true }, replaceFeedback: true);
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
            ? Changed(state with { Selection = proposed }, replaceFeedback: true)
            : Rejected(state with { Selection = null }, command == GameCommand.Interact
                ? InputFeedback.NoUsableMechanism : InputFeedback.InvalidSelection);
    }

    public static GameInfo<GameChangeInfo> ApplyPlayer(GameState state)
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

    public static GameInfo<GameChangeInfo> ApplyEnemy(GameState state, bool presentationCompleted)
    {
        if (state.Stage != GameStage.PlayerPresenting || state.Outcome != GameOutcome.Running
            || !presentationCompleted)
            return Rejected(state);
        var from = state.Room.PatrolAt(state.EnemyPatrolIndex);
        state = state with { EnemyPatrolIndex = (state.EnemyPatrolIndex + 1) % state.Room.PatrolLength,
            Stage = GameStage.EnemyPresenting };
        return Present(EvaluateOutcome(state), GameEventKind.EnemyMove, from, state.Room.PatrolAt(state.EnemyPatrolIndex));
    }

    public static GameInfo<GameChangeInfo> BeginResultPresentation(GameState state, bool presentationCompleted)
    {
        if (state.Outcome == GameOutcome.Running || !presentationCompleted || state.Stage == GameStage.Result)
            return Rejected(state);
        return Present(state with { Stage = GameStage.Result },
            GameEventKind.Result, state.Player, state.Player);
    }

    public static GameInfo<GameChangeInfo> EnableResultInput(GameState state, bool presentationCompleted)
        => state.Stage == GameStage.Result && presentationCompleted
            ? Changed(state with { ResultInputEnabled = true }) : Rejected(state);

    public static GameInfo<GameChangeInfo> ApplyControl(
        GameState state, GameControlInput input, ExecutionMode mode,
        PresentationKey? presentation, bool presentationCompleted)
    {
        if (input.Command == GameControlCommand.TogglePause
            && state.Stage is GameStage.Selecting or GameStage.PlayerPresenting or GameStage.EnemyPresenting)
            return new(state, new(true)
            {
                Mode = mode == ExecutionMode.Paused ? ExecutionMode.Active : ExecutionMode.Paused
            });
        if (input.Command == GameControlCommand.Skip && mode == ExecutionMode.Active
            && state.Stage is GameStage.PlayerPresenting or GameStage.EnemyPresenting
            && presentation is { } current && current == input.Presentation && !presentationCompleted)
            return new(state, new(true) { RequestSkip = true });
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

    private static GameInfo<GameChangeInfo> Present(
        GameState state, GameEventKind kind, GridPosition from, GridPosition to)
        => new(state, new(true)
        {
            ReplacePresentation = true,
            Presentation = kind,
            From = from,
            To = to,
            ReplaceFeedback = true
        });

    private static GameInfo<GameChangeInfo> Changed(
        GameState state, bool replaceFeedback = false, InputFeedback? feedback = null)
        => new(state, new(true) { ReplaceFeedback = replaceFeedback, Feedback = feedback });

    private static GameInfo<GameChangeInfo> Rejected(GameState state, InputFeedback? feedback = null)
        => new(state, new(false) { ReplaceFeedback = feedback.HasValue, Feedback = feedback });
}
