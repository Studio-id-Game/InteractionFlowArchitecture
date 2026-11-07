namespace InteractionFlow.Samples.Notepad.Secure.Entities
{
    public sealed class CredentialData(byte[] salt, byte[] verifier)
    {
        public byte[] Salt { get; } = salt;
        public byte[] Verifier { get; } = verifier;
    }
}
