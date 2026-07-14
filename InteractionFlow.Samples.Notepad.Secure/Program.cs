using InteractionFlow.Samples.Notepad.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Samples.Notepad.Core.Externals.Storages;
using InteractionFlow.Samples.Notepad.Core.Externals.Storages.Persistences;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.SystemFlows;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Samples.Notepad.Secure.Externals.Storages;
using InteractionFlow.Samples.Notepad.Secure.Externals.Storages.Persistences;
using InteractionFlow.Samples.Notepad.Secure.Externals.Storages.SecureManagers;
using InteractionFlow.Samples.Notepad.Secure.Externals.Storages.Serializers;
using InteractionFlow.Samples.Notepad.Secure.Interactions;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure
{
    internal class Program
    {
        static async Task Main(string[] _)
        {

            var scopeBuilder = new ScopeBuilder();

            scopeBuilder.Apply(ConsoleBuilder.Profile)
                .UseFunction<INotepadDataStoragePort, NotepadDataStorage>()
                .UseFunction<INotepadUserDataStoragePort, NotepadUserDataStorage>()
                .Use<INotepadDataPersistencePort, NotepadDataFilePersistence>()
                .Use<INotepadUserDataPersistencePort, NotepadUserDataDirectoryPersistence>()

                .UseInteraction<NoteCreate>()
                .UseInteraction<NoteDelete>()
                .UseInteraction<NoteEdit>()
                .UseInteraction<NoteListView>()
                .UseInteraction<SelectUserAction>()
                .UseInteraction<ConsoleWriting>()

                //Secure Custom
                .Use<IUserSecureDataPersistencePort, UserSecureDataFilePersistence>()
                .Use<ISecureManagerPort, SecureManagerPbkdf2>()
                .Use<INotepadDataSerializerPort, NotepadDataSecureSerializer>()
                .Use<IUserSecureDataSerializerPort, UserSecureDataSerializer>()
                .Use<ICurrentUserStoragePort, CurrentUserFilesStorage>()
                .Use<EnterPassword>()
                .Use<Login, LoginSecure>();

            var scope = scopeBuilder.BuildScope();

            var mainLoop = scope.BuildSystemFlow<MainLoop, NotepadContext>();

            var context = new NotepadContext(NotepadUserObject.Public);

            var end = await mainLoop.ExecuteAsync(context);

            context.TryGet<NotepadUserObject>(out var notepadUser);

            Console.WriteLine($"[Exit Notepad] - Goodbye, {notepadUser?.Id}.");
        }
    }
}
