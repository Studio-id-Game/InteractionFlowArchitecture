using InteractionFlow.Samples.Notepad.Core.Builders;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.SystemFlows;
using InteractionFlow.Samples.Notepad.Externals;
using InteractionFlow.Standard.Builders;
using System;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.InputEncoding = new UTF8Encoding(false, true);
            Console.OutputEncoding = new UTF8Encoding(false);
            var builder = new ScopeBuilder();
            builder.Apply(NotepadProfile.Default)
                .UseFunction<INotepadLoginOperation, ConsoleLoginOperation>()
                .Use<INotepadDataSerializerPort, NotepadDataSimpleSerializer>()
                .Use<INotepadDataPersistencePort, NotepadDataFilePersistence>();
            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<MainLoop, NotepadContext>();
            var end = await flow.ExecuteAsync(new NotepadContext());
            if (end.HasException) Environment.ExitCode = 1;
        }
    }
}
