using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using System;
using System.Security.Cryptography;
using System.Text;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    // One authenticated user per application scope. All cryptographic operations are local to this class.
    public sealed class SecureSession : SilentExternal, ISecureSessionPort, IDisposable
    {
        private const int Iterations = 100_000;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("IFN2");
        private static readonly byte[] VerifierLabel = Encoding.ASCII.GetBytes("InteractionFlow.Notepad.Password.v2");
        private byte[]? key;
        private byte[]? associatedData;
        private bool disposed;

        public override void ForceResetMemoryState()
        {
            if (key != null) CryptographicOperations.ZeroMemory(key);
            key = null;
            associatedData = null;
        }

        public Result<CredentialData> CreateCredential(ReadOnlySpan<char> password)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            try
            {
                var salt = RandomNumberGenerator.GetBytes(16);
                var derived = Derive(password, salt);
                try { return new CredentialData(salt, HMACSHA256.HashData(derived, VerifierLabel)); }
                finally { CryptographicOperations.ZeroMemory(derived); }
            }
            catch (CryptographicException error) { return error; }
        }

        public Result Authenticate(string user, ReadOnlySpan<char> password, CredentialData credential)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ForceResetMemoryState();
            if (credential.Salt.Length != 16 || credential.Verifier.Length != 32)
                return new FormatException("Invalid account metadata.");
            byte[]? derived = null;
            try
            {
                derived = Derive(password, credential.Salt);
                var verifier = HMACSHA256.HashData(derived, VerifierLabel);
                if (!CryptographicOperations.FixedTimeEquals(verifier, credential.Verifier))
                    return new UnauthorizedAccessException("Incorrect password.");
                associatedData = Encoding.UTF8.GetBytes("InteractionFlow.Notepad.v2:" + user);
                key = derived;
                derived = null;
                return Result.Success;
            }
            catch (CryptographicException error) { return error; }
            finally { if (derived != null) CryptographicOperations.ZeroMemory(derived); }
        }

        public Result<byte[]> Encrypt(ReadOnlySpan<byte> plaintext)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (key == null) return new InvalidOperationException("No authenticated session.");
            Span<byte> fileKey = stackalloc byte[32];
            try
            {
                // [magic:4][file ID:16][nonce:12][tag:16][ciphertext]
                var output = new byte[48 + plaintext.Length];
                Magic.CopyTo(output, 0);
                var fileId = output.AsSpan(4, 16);
                var nonce = output.AsSpan(20, 12);
                RandomNumberGenerator.Fill(fileId);
                RandomNumberGenerator.Fill(nonce);
                HKDF.Expand(HashAlgorithmName.SHA256, key, fileKey, fileId);
                using var aes = new AesGcm(fileKey, 16);
                aes.Encrypt(nonce, plaintext, output.AsSpan(48), output.AsSpan(32, 16), associatedData);
                return output;
            }
            catch (CryptographicException error) { return error; }
            finally { CryptographicOperations.ZeroMemory(fileKey); }
        }

        public Result<byte[]> Decrypt(ReadOnlySpan<byte> ciphertext)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (key == null) return new InvalidOperationException("No authenticated session.");
            if (ciphertext.Length < 48 || !ciphertext[..4].SequenceEqual(Magic))
                return new FormatException("Invalid encrypted note header.");
            var plaintext = new byte[ciphertext.Length - 48];
            Span<byte> fileKey = stackalloc byte[32];
            try
            {
                HKDF.Expand(HashAlgorithmName.SHA256, key, fileKey, ciphertext.Slice(4, 16));
                using var aes = new AesGcm(fileKey, 16);
                aes.Decrypt(ciphertext.Slice(20, 12), ciphertext[48..], ciphertext.Slice(32, 16),
                    plaintext, associatedData);
                return plaintext;
            }
            catch (CryptographicException error)
            {
                CryptographicOperations.ZeroMemory(plaintext);
                return error;
            }
            finally { CryptographicOperations.ZeroMemory(fileKey); }
        }

        private static byte[] Derive(ReadOnlySpan<char> password, byte[] salt)
            => Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);

        public void Dispose()
        {
            ForceResetMemoryState();
            disposed = true;
        }
    }
}
