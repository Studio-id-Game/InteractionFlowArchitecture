using System;

namespace InteractionFlow.Samples.Notepad.Secure.Entities
{
    public sealed class SecretPassword(char[] value) : IDisposable
    {
        public ReadOnlySpan<char> Value => value;
        public void Dispose() => Array.Clear(value);
    }
}
