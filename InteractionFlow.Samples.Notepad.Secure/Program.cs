using InteractionFlow.Samples.Notepad.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.ProgramFlows;
using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.Silents;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.Externals.Silents;
using InteractionFlow.Samples.Notepad.Secure.Externals.Storages;
using InteractionFlow.Samples.Notepad.Secure.Interactions;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Interactions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure
{
    internal class Program
    {
        static void EncryptTest()
        {
            var mng = new SecureManagerPbkdf2();
            var userSecureData = new NotepadUserSecureData();

            Console.Write("Encrypt Pass : ");
            var pass = Console.ReadLine();
            var data = new NotepadData() { Title = "Test Notepad", Text = "MyTestText" };
            Span<byte> cipherBytes = stackalloc byte[mng.GetCipherBytesSize(data)];

            using (var userKey = mng.GetUserKey(pass, userSecureData))
            {
                var result = mng.EncryptNotepadData(data, userKey, cipherBytes);

                if (!result)
                {
                    Console.WriteLine($"[{result.Exception!.GetType().Name}] {result.Exception!.Message}");
                    return;
                }
            }


            Console.WriteLine(Encoding.UTF8.GetString(cipherBytes));

            Console.Write("Decrypt Pass : ");
            var newPass = Console.ReadLine();

            var newData = new NotepadData();
            using (var userKey = mng.GetUserKey(newPass, userSecureData))
            {
                var result = mng.DecryptNotepadData(newData, userKey, cipherBytes);

                if (!result)
                {
                    Console.WriteLine($"[{result.Exception!.GetType().Name}] {result.Exception!.Message}");
                    return;
                }
            }

            Console.WriteLine($"[{newData.Title}] {newData.Text}");
        }

        static async Task Main(string[] _)
        {

            var scopeBuilder = new ScopeBuilder();

            scopeBuilder.Apply(ConsoleBuilder.Profile)
                .UseFunction<INotepadUserDataMemory, NotepadUserDataMemory>()
                .UseFunction<INotepadUserDataFiles, NotepadUserDataDirectories>()
                .UseFunction<INotepadDataMemory, NotepadDataMemory>()
                .UseFunction<INotepadDataFiles, NotepadDataFiles>()
                .UseFunction<INotepadUserSecureDataMemory, NotepadUserSecureDataMemory>()
                .UseFunction<INotepadUserSecureDataFiles, NotepadUserSecureDataFiles>()
                .UseFunction<ISecureManager, SecureManagerPbkdf2>()
                .Use<Login, LoginSecure>()
                .UseInteraction<EnterPassword>()
                .UseInteraction<NoteCreate>()
                .UseInteraction<NoteDelete>()
                .UseInteraction<NoteEdit>()
                .UseInteraction<NoteListView>()
                .UseInteraction<SelectUserAction>()
                .UseInteraction<ConsoleWriting>();

            var scope = scopeBuilder.BuildScope();

            var mainLoop = scope.BuildProgramFlow<MainLoop, NotepadContext>();

            var context = new NotepadContext(NotepadUserObject.Public);

            var end = await mainLoop.ExecuteAsync(context);

            end.LastContext.TryGet<NotepadUserObject>(out var notepadUser);

            Console.WriteLine($"[Exit Notepad] - Goodbye, {notepadUser?.Id}.");
        }
    }
}
