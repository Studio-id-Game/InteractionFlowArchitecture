using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.Notepad.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Focuses;
using InteractionFlow.Samples.Notepad.Interactions;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Samples.Notepad.Storages;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Builders.Profiles;
using InteractionFlow.Standard.Interactions;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.Operations;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad
{
    internal class Program
    {
        static async Task Main(string[] _)
        {
            var scopeBuilder = new ScopeBuilder();

            scopeBuilder.UseFunction<IConsoleOperation, ConsoleOperation>()
                .Apply(ConsoleFunction.Profile)
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
                .UseInteraction<ConsoleWrite>();

            var scope = scopeBuilder.BuildScope();

            var mainLoop = scope.BuildFocus<MainLoop, NotepadContext>();

            var context = new NotepadContext(NotepadUserObject.Public);

            var end = await mainLoop.UseUserFlowAsync(context);

            end.LastContext.TryGet<NotepadUserObject>(out var notepadUser);

            Console.WriteLine($"[Exit Notepad] - Goodbye, {notepadUser?.Id}.");
        }
    }
}
