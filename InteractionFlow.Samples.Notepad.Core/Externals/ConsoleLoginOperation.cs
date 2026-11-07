using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    public class ConsoleLoginOperation(
        IConsoleLineInputPort input, IConsoleOutputPort output, params IDependencyNode[] dependency)
        : Operation([input, output, .. dependency]), INotepadLoginOperation
    {
        protected IConsoleLineInputPort Input => input;
        protected IConsoleOutputPort Output => output;
        public override void ForceResetMemoryState() { }

        public async Task<Result> LoginAsync(NotepadContext context)
        {
            // End the old session before asking for another user's credentials.
            ForceResetMemoryState();
            context.User = null;
            var end = await output.WriteLineAsync(context,
                "User ID (letters/digits/_/-, blank = Public, /exit = quit):");
            if (end.HasException) return end.Exception!;
            var text = await input.ReadLineAsync(context);
            if (text == null || text.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                context.ExitRequested = true;
                return Result.Success;
            }
            var user = NotepadRules.NormalizeId(text);
            if (user.Length != 0 && !NotepadRules.IsValidId(user))
                return new FormatException("Use 1–64 letters, digits, underscores or hyphens for a user ID.");
            var authentication = await AuthenticateAsync(context, user);
            if (authentication.Try(out _) && !context.ExitRequested) context.User = user;
            return authentication;
        }

        protected virtual Task<Result> AuthenticateAsync(NotepadContext context, string user)
            => Result.Success.StartAsync();
    }
}
