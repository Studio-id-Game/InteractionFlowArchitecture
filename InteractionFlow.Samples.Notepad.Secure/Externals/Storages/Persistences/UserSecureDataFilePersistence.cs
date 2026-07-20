using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.FileSystem.Externals.Persistences;
using System;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages.Persistences
{

    public sealed class UserSecureDataFilePersistence(IUserSecureDataSerializerPort serializer)
        : FilePersistence<NotepadUserKey, UserSecureData>((InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts.ISerializerPort<Stream, UserSecureData>)serializer), IUserSecureDataPersistencePort
    {
        public override string Extention => ".user";

        public override string RootPath => Path.Combine(base.RootPath, "NotepadData");

        public override NotepadUserKey GetFileId(string fileName)
        {
            var sepIndex = fileName.Replace('\\', '/').IndexOf('/');

            if (sepIndex <= 0 || sepIndex == fileName.Length - 1)
                throw new InvalidOperationException("Invalid Note FileName");

            var userName = fileName[..sepIndex];

            if (userName == NotepadUserKey.Public.Name)
            {
                return NotepadUserKey.Public;
            }
            else
            {
                return new NotepadUserKey(userName);
            }
        }

        public override string GetFileName(NotepadUserKey fileID)
        {
            return Path.Combine(fileID.Name, fileID.Name);
        }
    }
}
