using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using System;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts
{
    public interface ISecureSessionPort : ISilentExternalPort
    {
        Result<CredentialData> CreateCredential(ReadOnlySpan<char> password);
        Result Authenticate(string user, ReadOnlySpan<char> password, CredentialData credential);
        Result<byte[]> Encrypt(ReadOnlySpan<byte> plaintext);
        Result<byte[]> Decrypt(ReadOnlySpan<byte> ciphertext);
    }
}
