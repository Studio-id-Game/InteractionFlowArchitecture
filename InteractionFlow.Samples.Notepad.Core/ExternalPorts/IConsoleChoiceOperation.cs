using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface IConsoleChoiceOperation : IOperationPort
    {
        // The successful value contains a zero-based index; null means cancel, -1 means invalid redirected input.
        // EOF also records an exit request in the context.
        // Returned failures are preserved; thrown exceptions and Task cancellation propagate.
        Task<Result<ChoiceValue>> SelectAsync(NotepadContext context, string title, string[] choices, string[]? labels = null);
    }
}
