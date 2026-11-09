namespace InteractionFlow.Samples.EscapeGame.Entities;

internal static class WorldRules
{
    internal static MutationStatus ApplySelection(GameCommand command, SessionState state, PlayerState player,
        InventoryState inventory, MechanismState mechanisms, out string message)
    {
        if (state.Stage == GameStage.Result && state.ResultInputEnabled && command == GameCommand.Retry)
        {
            state.RetryRequested = true;
            message = "Retrying";
            return MutationStatus.Applied;
        }
        if (state.Stage != GameStage.Selecting || state.Outcome != GameOutcome.Running || state.Confirmed)
        {
            message = "This stage does not accept selection";
            return MutationStatus.Rejected;
        }
        if (command == GameCommand.CancelSelection)
        {
            state.Selection = null;
            message = "Selection canceled";
        }
        else if (command == GameCommand.Confirm)
        {
            if (state.Selection is not { } action || !IsValid(action, player, inventory, mechanisms))
            {
                state.Selection = null;
                message = "Select a valid action first";
                return MutationStatus.Rejected;
            }
            state.Confirmed = true;
            message = "Action confirmed";
        }
        else
        {
            state.Selection = Select(command, player, inventory, mechanisms, out message);
            if (state.Selection is null)
                return MutationStatus.Rejected;
        }
        return MutationStatus.Applied;
    }

    internal static ActionEffect? ApplyPlayer(SessionState state, PlayerState player,
        InventoryState inventory, MechanismState mechanisms, EnemyState enemy)
    {
        if (state.Stage != GameStage.Selecting || !state.Confirmed || state.Selection is not { } action
            || !IsValid(action, player, inventory, mechanisms)) return null;
        var room = RoomDefinition.Default;
        var from = player.Position;
        var kind = PresentationKind.Wait;
        var duration = 0d;
        switch (action.Kind)
        {
            case ActionKind.Move:
                player.Position = action.Target;
                if (action.Target == room.Key && !mechanisms.KeyCollected)
                {
                    mechanisms.KeyCollected = true;
                    inventory.HasKey = true;
                }
                kind = PresentationKind.PlayerMove;
                duration = .15;
                break;
            case ActionKind.Switch:
                mechanisms.SwitchActivated = true;
                kind = PresentationKind.Switch;
                duration = .2;
                break;
            case ActionKind.Unlock:
                inventory.HasKey = false;
                mechanisms.DoorUnlocked = true;
                kind = PresentationKind.Unlock;
                duration = .2;
                break;
        }
        state.Turn++;
        state.Selection = null;
        state.Confirmed = false;
        state.Stage = GameStage.PlayerPresenting;
        EvaluateOutcome(state, player, enemy);
        return new(kind, from, player.Position, duration, "Player: " + action.Kind);
    }

    internal static ActionEffect? ApplyEnemy(SessionState state, PlayerState player, EnemyState enemy)
    {
        if (state.Stage != GameStage.PlayerPresenting || state.Outcome != GameOutcome.Running)
            return null;
        var room = RoomDefinition.Default;
        var from = room.PatrolAt(enemy.PatrolIndex);
        enemy.PatrolIndex = (enemy.PatrolIndex + 1) % room.PatrolLength;
        state.Stage = GameStage.EnemyPresenting;
        EvaluateOutcome(state, player, enemy);
        return new(PresentationKind.EnemyMove, from, room.PatrolAt(enemy.PatrolIndex), .15, "Guard moves");
    }

    private static void EvaluateOutcome(SessionState state, PlayerState player, EnemyState enemy)
    {
        if (state.Outcome != GameOutcome.Running) return;
        if (player.Position == RoomDefinition.Default.PatrolAt(enemy.PatrolIndex)) state.Outcome = GameOutcome.Lost;
        else if (player.Position == RoomDefinition.Default.Exit) state.Outcome = GameOutcome.Won;
    }

    internal static GameAction? Select(GameCommand command, PlayerState player, InventoryState inventory,
        MechanismState mechanisms, out string message)
    {
        var room = RoomDefinition.Default;
        var position = player.Position;
        GameAction? action = command switch
        {
            GameCommand.Up => new(ActionKind.Move, position with { Y = position.Y - 1 }),
            GameCommand.Right => new(ActionKind.Move, position with { X = position.X + 1 }),
            GameCommand.Down => new(ActionKind.Move, position with { Y = position.Y + 1 }),
            GameCommand.Left => new(ActionKind.Move, position with { X = position.X - 1 }),
            GameCommand.Wait => new(ActionKind.Wait, position),
            GameCommand.Interact when position.DistanceTo(room.Switch) == 1 && !mechanisms.SwitchActivated
                => new(ActionKind.Switch, room.Switch),
            GameCommand.Interact when position.DistanceTo(room.Door) == 1 && !mechanisms.DoorUnlocked
                => new(ActionKind.Unlock, room.Door),
            _ => null
        };
        if (action is { } proposed && IsValid(proposed, player, inventory, mechanisms))
        {
            message = "Selected " + proposed.Kind + "; Enter to confirm";
            return proposed;
        }
        message = command == GameCommand.Interact ? "No usable mechanism (switch and key are required for door)"
            : "That action is not valid";
        return null;
    }

    internal static bool IsValid(
        GameAction action, PlayerState player, InventoryState inventory, MechanismState mechanisms)
    {
        var room = RoomDefinition.Default;
        return action.Kind switch
        {
            ActionKind.Move => player.Position.DistanceTo(action.Target) == 1
                && room.CanEnter(action.Target, mechanisms.DoorUnlocked),
            ActionKind.Switch => action.Target == room.Switch && player.Position.DistanceTo(action.Target) == 1
                && !mechanisms.SwitchActivated,
            ActionKind.Unlock => action.Target == room.Door && player.Position.DistanceTo(action.Target) == 1
                && !mechanisms.DoorUnlocked && mechanisms.SwitchActivated && inventory.HasKey,
            ActionKind.Wait => action.Target == player.Position,
            _ => false
        };
    }
}

internal readonly record struct ActionEffect(PresentationKind Kind, GridPosition From, GridPosition To,
    double DurationSeconds, string Message);
