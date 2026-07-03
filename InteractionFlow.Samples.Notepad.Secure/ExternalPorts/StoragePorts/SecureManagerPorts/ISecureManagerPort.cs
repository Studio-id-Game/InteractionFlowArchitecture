using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using System;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts
{

    public interface ISecureManagerPort
    {
        byte[] GetNewUserSalt();

        SecretBuffer GetUserKey(ReadOnlySpan<char> password, UserSecureData notepadUserSecureData);

        Result DecryptNotepadData(NotepadData data, SecretBuffer userkey, ReadOnlySpan<byte> cipherBytes);

        Result EncryptNotepadData(NotepadData data, SecretBuffer userkey, Span<byte> cipherBytes);

        int GetCipherBytesSize(NotepadData data);
    }
}
