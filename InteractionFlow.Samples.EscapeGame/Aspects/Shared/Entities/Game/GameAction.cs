namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;

public readonly record struct GameAction(ActionKind Kind, GridPosition Target);

public enum ActionKind { Move, Switch, Unlock, Wait }
