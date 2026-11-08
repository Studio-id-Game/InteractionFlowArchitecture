namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

public interface ICounterDisplayAccess
{
    void Snapshot(out int count, out string message, out bool acceptingInput);
}
