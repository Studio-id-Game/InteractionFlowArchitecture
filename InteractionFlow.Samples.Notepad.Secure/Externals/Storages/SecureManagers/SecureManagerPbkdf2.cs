using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages.SecureManagers
{
    public class SecureManagerPbkdf2 : ISecureManagerPort
    {
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256bit
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int Iterations = 100_000;
        private const int HeaderSize = 16; // 16byte Guid

        public int GetCipherBytesSize(NotepadData data)
        {
            var titleSize = sizeof(int);
            var title = Encoding.UTF8.GetByteCount(data.Title);
            var text = Encoding.UTF8.GetByteCount(data.Text);
            return HeaderSize + NonceSize + TagSize + titleSize + title + text;
        }

        public Result EncryptNotepadData(NotepadData data, SecretBuffer userkey, Span<byte> cipherBytes)
        {
            try
            {
                Span<byte> bytes = stackalloc byte[GetBytesSize(data)];
                GetBytesFromData(data, bytes);

                var fileId = Guid.NewGuid().ToByteArray();
                using var fileKey = GetFileKey(userkey, fileId);
                EncryptBytes(fileId, bytes, cipherBytes, fileKey.Value);

                return Result.Success;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public Result DecryptNotepadData(NotepadData data, SecretBuffer userkey, ReadOnlySpan<byte> cipherBytes)
        {
            try
            {
                var fileId = cipherBytes[..HeaderSize];
                Span<byte> bytes = stackalloc byte[GetBytesSize(cipherBytes.Length)];
                using (var fileKey = GetFileKey(userkey, fileId))
                {
                    DecryptBytes(cipherBytes[HeaderSize..], bytes, fileKey.Value);
                }

                GetDataFromBytes(data, bytes);

                return Result.Success;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public byte[] GetNewUserSalt()
        {
            return RandomNumberGenerator.GetBytes(SaltSize);
        }

        public SecretBuffer GetUserKey(ReadOnlySpan<char> password, UserSecureData userSecureData)
        {
            var userKey = new SecretBuffer(Pbkdf2(password, userSecureData.UserSalt!));
            userSecureData.LastUserKey = userKey.Value.ToArray();
            return userKey;
        }

        private static int GetBytesSize(int cipherBytesSize)
        {
            return cipherBytesSize - HeaderSize - NonceSize - TagSize;
        }

        private static int GetBytesSize(NotepadData data)
        {
            var titleSize = sizeof(int);
            var title = Encoding.UTF8.GetByteCount(data.Title);
            var text = Encoding.UTF8.GetByteCount(data.Text);
            return titleSize + title + text;
        }

        private static void GetBytesFromData(NotepadData data, Span<byte> bytes)
        {
            int offset = 0;

            BinaryPrimitives.WriteInt32LittleEndian(
                bytes[offset..(offset += sizeof(int))],
                data.Title.Length);

            offset += Encoding.UTF8.GetBytes(
                data.Title,
                bytes[offset..]);

            Encoding.UTF8.GetBytes(
                data.Text,
                bytes[offset..]);
        }

        private static void GetDataFromBytes(NotepadData data, ReadOnlySpan<byte> bytes)
        {
            int offset = 0;

            var titleSizeBytes = bytes[offset..(offset += sizeof(int))];
            var titleSize = BinaryPrimitives.ReadInt32LittleEndian(titleSizeBytes);

            var titleBytes = bytes[offset..(offset += titleSize)];
            var title = Encoding.UTF8.GetString(titleBytes);

            var textBytes = bytes[offset..];
            var text = Encoding.UTF8.GetString(textBytes);

            data.Title = title;
            data.Text = text;
        }

        private static void EncryptBytes(
            ReadOnlySpan<byte> headerbytes,
            ReadOnlySpan<byte> bytes,
            Span<byte> cipherBytes,
            ReadOnlySpan<byte> key)
        {
            Span<byte> nonce = stackalloc byte[NonceSize];
            Span<byte> tag = stackalloc byte[TagSize];
            Span<byte> ciphertext = stackalloc byte[bytes.Length];

            RandomNumberGenerator.Fill(nonce);
            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(
                    nonce,
                    bytes,
                    ciphertext,
                    tag);
            }


            int offset = 0;

            // format:
            // [head][nonce][tag][ciphertext]
            headerbytes.CopyTo(cipherBytes[..(offset += headerbytes.Length)]);
            nonce.CopyTo(cipherBytes[offset..(offset += NonceSize)]);
            tag.CopyTo(cipherBytes[offset..(offset += TagSize)]);
            ciphertext.CopyTo(cipherBytes[offset..(offset += bytes.Length)]);
        }

        private static void DecryptBytes(
            ReadOnlySpan<byte> cipherBytes,
            Span<byte> bytes,
            ReadOnlySpan<byte> key)
        {
            int offset = 0;

            ReadOnlySpan<byte> nonce = cipherBytes[offset..(offset += NonceSize)];
            ReadOnlySpan<byte> tag = cipherBytes[offset..(offset += TagSize)];
            ReadOnlySpan<byte> ciphertext = cipherBytes[offset..];

            using var aes = new AesGcm(key, TagSize);

            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                bytes);
        }

        private static byte[] Pbkdf2(ReadOnlySpan<char> password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
        }

        private static SecretBuffer GetFileKey(SecretBuffer userKey, ReadOnlySpan<byte> fileId)
        {
            return new(HkdfExpand(userKey.Value, fileId));
        }

        private static byte[] HkdfExpand(ReadOnlySpan<byte> baseKey, ReadOnlySpan<byte> info)
        {
            byte[] result = new byte[KeySize];

            Span<byte> input = stackalloc byte[info.Length + 1];

            info.CopyTo(input);
            input[^1] = 1;

            HMACSHA256.HashData(baseKey, input, result);

            CryptographicOperations.ZeroMemory(input);
            return result;
        }
    }
}
