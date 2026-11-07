using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using InteractionFlow.Standard.SerializationSystem.Externals;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class CredentialSerializer : TextSerializer<CredentialData>, ICredentialSerializerPort
    {
        public override Task<Result<string>> Serialize(Result<CredentialData> inputValue, Result<string> refText)
            => inputValue.Then(value => value.Salt.Length == 16 && value.Verifier.Length == 32
                ? ("IFACCOUNT2\n" + Convert.ToBase64String(value.Salt) + "\n" +
                    Convert.ToBase64String(value.Verifier)).AsResult()
                : new FormatException("Invalid account metadata size.")).StartAsync();

        public override Task<Result<CredentialData>> Deserialize(
            Result<string> inputText, Result<CredentialData> refValue)
            => inputText.Then(Parse).StartAsync();

        private static Result<CredentialData> Parse(string text)
        {
            var parts = text.Split('\n');
            if (parts.Length != 3 || parts[0] != "IFACCOUNT2") return new FormatException("Invalid account file.");
            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var verifier = Convert.FromBase64String(parts[2]);
                return salt.Length == 16 && verifier.Length == 32
                    ? new CredentialData(salt, verifier).AsResult()
                    : new FormatException("Invalid account metadata size.");
            }
            catch (FormatException error) { return error; }
        }
    }
}
