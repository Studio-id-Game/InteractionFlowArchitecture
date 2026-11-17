using InteractionFlow.Core.ResultAPI.Entities;
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

            var loaded = await LoadCredentialAsync(user);
            if (!loaded.HasValue(out var credential))
            {
                if (loaded.ErrorValue != CredentialLoadError.Missing) return loaded.WithoutValue;
                var listed = await notes.GetAllIds();
                if (!listed.HasValue(out var ids)) return listed.Error!;
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
                if (!created.HasValue(out credential)) return created.Error!;
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var saved = await credentials.Save(user, credential);
                if (saved.HasError(out var saveError)) return saveError;
            }
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            return session.Authenticate(user, password.Value, credential);
        }

        private async Task<Result<CredentialData, CredentialLoadError>> LoadCredentialAsync(string user)
        {
            var loaded = await credentials.Load(user, new CredentialData([], []));
            if (loaded.HasValue(out var credential)) return credential;

            var error = loaded.Error!;
            var detail = error.InnerException is FileNotFoundException or DirectoryNotFoundException
                ? CredentialLoadError.Missing
                : CredentialLoadError.ReadFailed;
            return (error, detail);
        }

        private enum CredentialLoadError { Missing, ReadFailed }
    }
}
