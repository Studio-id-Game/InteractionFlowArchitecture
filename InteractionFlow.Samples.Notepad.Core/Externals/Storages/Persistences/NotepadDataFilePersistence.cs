using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.FileSystem.Externals.Persistences;
using System;
using System.IO;
using System.Linq;

namespace InteractionFlow.Samples.Notepad.Core.Externals.Storages.Persistences
{
    public sealed class NotepadDataFilePersistence(INotepadDataSerializerPort serializer)
        : FilePersistence<NotepadDataKey, NotepadData>((InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts.ISerializerPort<Stream, NotepadData>)serializer), INotepadDataPersistencePort
    {
        public override string RootPath => Path.Combine(base.RootPath, "NotepadData");

        public NotepadDataKey[] GetAlllIdWithUser(NotepadUserKey notepadUserKey)
        {
            var dir = Path.Combine(RootPath, notepadUserKey.Name);
            DirectoryUtility.CreateDirectories(RootPath, dir);
            var files = Directory.GetFiles(dir, $"*{Extention}", SearchOption.AllDirectories);
            var ids = files.Select(GetFileIdFromPath);
            return [.. ids];
        }

        public override NotepadDataKey GetFileId(string fileName)
        {
            var sepIndex = fileName.Replace('\\', '/').IndexOf('/');

            if (sepIndex <= 0 || sepIndex == fileName.Length - 1)
                throw new InvalidOperationException("Invalid Note FileName");

            var userName = fileName[..sepIndex];

            var userId = NotepadUserKey.Public.Name == userName ? NotepadUserKey.Public.Id : userName;

            return new NotepadDataKey(
                UserId: userId,
                NoteId: fileName[(sepIndex + 1)..]);
        }

        public override string GetFileName(NotepadDataKey fileID)
        {


            return Path.Combine(new NotepadUserKey(fileID.UserId).Name, fileID.NoteId);
        }

        public string GetViewName(NotepadDataKey key)
        {
            return GetFileName(key) + Extention;
        }
    }
}
