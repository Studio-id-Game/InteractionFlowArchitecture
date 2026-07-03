namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts
{
    public interface IFilePersistencePort<TFileId, TValue> : IPersistencePort<TFileId, TValue>
    {
        string Extention { get; }
        string RootPath { get; }

        TFileId GetFileId(string fileName);

        TFileId GetFileIdFromPath(string filePath);

        string GetFileName(TFileId id);
        string GetFilePath(TFileId id);
    }
}
