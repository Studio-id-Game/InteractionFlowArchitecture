using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using InteractionFlow.Standard.SerializationSystem.Externals;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class EncryptedNoteSerializer(ISecureSessionPort session)
        : StreamSerializer<NotepadData>(session), INotepadDataSerializerPort
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private const int MaxFileBytes = 4 * (NotepadRules.MaxTextLength + NotepadRules.MaxTitleLength + 1) + 48;

        public override async Task<Result<Stream>> Serialize(Result<NotepadData> inputValue, Result<Stream> refData)
        {
            if (!inputValue.HasValue(out var note)) return inputValue.Error!;
            if (!refData.HasValue(out var stream)) return refData.Error!;
            if (note.Title.Length > NotepadRules.MaxTitleLength
                || note.Title.Contains('\n') || note.Title.Contains('\r')
                || note.Text.Length > NotepadRules.MaxTextLength)
                return new FormatException("Invalid note size or title.");
            byte[] plaintext;
            try { plaintext = Utf8.GetBytes(note.Title + "\n" + note.Text); }
            catch (EncoderFallbackException error) { return error; }
            Result<byte[]> encrypted;
            try { encrypted = session.Encrypt(plaintext); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
            if (!encrypted.HasValue(out var bytes)) return encrypted.Error!;
            try { await stream.WriteAsync(bytes); return stream.AsResult(); }
            catch (Exception error) { return error; }
        }

        public override async Task<Result<NotepadData>> Deserialize(
            Result<Stream> inputData, Result<NotepadData> refValue)
        {
            if (!inputData.HasValue(out var stream)) return inputData.Error!;
            byte[] bytes;
            try
            {
                using var memory = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer)) != 0)
                {
                    if (memory.Length + count > MaxFileBytes)
                        return new FormatException("Encrypted note is too large.");
                    memory.Write(buffer, 0, count);
                }
                bytes = memory.ToArray();
            }
            catch (Exception error) { return error; }
            var decrypted = session.Decrypt(bytes);
            if (!decrypted.HasValue(out var plaintext)) return decrypted.Error!;
            try
            {
                string text;
                try { text = Utf8.GetString(plaintext); }
                catch (DecoderFallbackException error) { return error; }
                var split = text.IndexOf('\n');
                if (split < 0 || split > NotepadRules.MaxTitleLength
                    || text.Length - split - 1 > NotepadRules.MaxTextLength)
                    return new FormatException("Invalid decrypted note.");
                return new NotepadData(text[..split], text[(split + 1)..]);
            }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
    }
}
