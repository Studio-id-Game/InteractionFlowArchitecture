using System;
using System.Security.Cryptography;

namespace InteractionFlow.Samples.Notepad.Secure.Entities
{
    public readonly ref struct SecretBuffer(Span<byte> value)
    {
        public Span<byte> Value { get; } = value;

        public readonly void Dispose()
        {
            CryptographicOperations.ZeroMemory(Value);
        }
    }
}
