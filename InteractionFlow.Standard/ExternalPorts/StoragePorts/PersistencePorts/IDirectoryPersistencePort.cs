namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts
{
    public interface IDirectoryPersistencePort<TDirectoryId, TValue> : IPersistencePort<TDirectoryId, TValue>
    {
        string RootPath { get; }

        TDirectoryId GetDirectoryId(string directoryName);
        string GetDirectoryName(TDirectoryId id);
        string GetDirectoryPath(TDirectoryId id);
    }
}
