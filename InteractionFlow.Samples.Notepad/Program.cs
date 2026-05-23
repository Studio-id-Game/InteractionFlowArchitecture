using InteractionFlow.Samples.Notepad.Entities.Contexts;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Externals.Storages;
using InteractionFlow.Samples.Notepad.Interactions;
using InteractionFlow.Samples.Notepad.ProgramFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad
{
    internal class Program
    {
        static async Task Main(string[] _)
        {
            var scopeBuilder = new ScopeBuilder();

            scopeBuilder.Apply(ConsoleBuilder.Profile)
                .UseFunction<INotepadUserDataMemory, NotepadUserDataMemory>()
                .UseFunction<INotepadUserDataFiles, NotepadUserDataDirectories>()
                .UseFunction<INotepadDataMemory, NotepadDataMemory>()
                .UseFunction<INotepadDataFiles, NotepadDataFiles>()
                .UseInteraction<Login>()
                .UseInteraction<NoteCreate>()
                .UseInteraction<NoteDelete>()
                .UseInteraction<NoteEdit>()
                .UseInteraction<NoteListView>()
                .UseInteraction<SelectUserAction>()
                .UseInteraction<ConsoleWriting>();

            var scope = scopeBuilder.BuildScope();

            var mainLoop = scope.BuildFlow<MainLoop, NotepadContext>();

            var context = new NotepadContext(NotepadUserObject.Public);

            var end = await mainLoop.ExecuteAsync(context);

            end.LastContext.TryGet<NotepadUserObject>(out var notepadUser);

            Console.WriteLine($"[Exit Notepad] - Goodbye, {notepadUser?.Id}.");
        }
    }
}
