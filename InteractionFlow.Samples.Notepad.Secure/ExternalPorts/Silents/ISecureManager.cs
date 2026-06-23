using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using System;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.Silents
{
    public interface ISecureManager : ISilentExternalPort
    {
        int GetCipherBytesSize(NotepadData data);

        Result EncryptNotepadData(NotepadData data, SecretBuffer userkey, Span<byte> cipherBytes);

        Result DecryptNotepadData(NotepadData data, SecretBuffer userkey, ReadOnlySpan<byte> cipherBytes);

        SecretBuffer GetUserKey(ReadOnlySpan<char> password, NotepadUserSecureData notepadUserSecureData);

        bool TryGetLastUserKey(NotepadUserSecureData notepadUserSecureData, out SecretBuffer userKey);
    }
}
