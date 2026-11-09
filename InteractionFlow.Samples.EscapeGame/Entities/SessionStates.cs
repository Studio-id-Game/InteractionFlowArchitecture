namespace InteractionFlow.Samples.EscapeGame.Entities;

internal sealed class PlayerState
{
    internal GridPosition Position = RoomDefinition.Default.PlayerStart;
}

internal sealed class InventoryState
{
    internal bool HasKey;
}

internal sealed class MechanismState
{
    internal bool KeyCollected;
    internal bool SwitchActivated;
    internal bool DoorUnlocked;
}
internal sealed class EnemyState
{
    internal int PatrolIndex;
}

internal sealed class SessionProgressState(long id)
{
    internal long Id { get; } = id;
    internal GameStage Stage = GameStage.Preparing;
    internal GameOutcome Outcome;
    internal int Turn;
    internal bool Published;
    internal bool Retired;
    internal bool Confirmed;
    internal bool RetryRequested;
    internal bool ResultInputEnabled;
    internal GameAction? Selection;
}
internal sealed class DisplayState
{
    internal long Version;
    internal string Message = "Preparing room";

    internal void SetMessage(string message)
    {
        Message = message;
        Version++;
    }
}
