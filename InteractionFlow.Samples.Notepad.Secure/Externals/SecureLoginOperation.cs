using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class SecureLoginOperation(IConsoleLineInputPort input, IConsoleOutputPort output,
        IPasswordInputPort passwords, ICredentialPersistencePort credentials, ISecureSessionPort session,
        INotepadDataPersistencePort notes)
        : ConsoleLoginOperation(input, output, passwords, credentials, session, notes)
    {
        public override void ForceResetMemoryState() => session.ForceResetMemoryState();

        protected override async Task<Result> AuthenticateAsync(NotepadContext context, string user)
        {
            var end = await Output.WriteLineAsync(context, "Password:");
            if (end.HasException) return end.Exception!;
            using var password = await passwords.ReadAsync(context);
            if (password == null) { context.ExitRequested = true; return Result.Success; }
            if (password.Value.Length == 0) return new FormatException("Password must not be empty.");

            var loaded = await credentials.Load(user, new CredentialData([], []));
            if (!loaded.Try(out var credential, out var error))
            {
                // Only missing files allow registration. Corrupt or inaccessible metadata must be preserved.
                if (error.InnerException is not (FileNotFoundException or DirectoryNotFoundException)) return error;
                var listed = await notes.GetAllIds();
                if (!listed.Try(out var ids, out var listError)) return listError;
                if (ids.Any(id => id.User == user))
                    return new InvalidOperationException(
                        "Account metadata is missing for existing notes. Restore the account file.");
                end = await Output.WriteLineAsync(context, "New account. Confirm password:");
                if (end.HasException) return end.Exception!;
                using var confirmation = await passwords.ReadAsync(context);
                if (confirmation == null) { context.ExitRequested = true; return Result.Success; }
                if (!password.Value.SequenceEqual(confirmation.Value))
                    return new FormatException("Passwords do not match.");
                var created = session.CreateCredential(password.Value);
                if (!created.Try(out credential, out var createError)) return createError;
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var saved = await credentials.Save(user, credential);
                if (!saved.Try(out var saveError)) return saveError;
            }
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            return session.Authenticate(user, password.Value, credential);
        }
    }
}
