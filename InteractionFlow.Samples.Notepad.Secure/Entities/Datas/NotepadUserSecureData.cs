using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Standard.Entities.Storages;
using System;
using System.IO;
using System.Security.Cryptography;

namespace InteractionFlow.Samples.Notepad.Secure.Entities.Datas
{
    public class NotepadUserSecureData : IKeyedMemoryValue<NotepadUserKey>, IDisposable
    {
        private bool disposedValue;

        public static string FileName => ".user";

        public NotepadUserKey UserId { get; private set; } = NotepadUserKey.Public;

        public byte[]? UserSalt { get; set; }

        public byte[]? LastUserKey { get; set; }

        public bool TryInitialize(IFlowContext context, NotepadUserKey contextKey)
        {
            UserId = contextKey;

            return true;
        }

        public FileInfo? GetFileInfo()
        {
            var dir = UserId.GetUserDirectory();
            if (dir == null)
                return null;

            return new(Path.Join(dir.FullName, FileName));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)
                    CryptographicOperations.ZeroMemory(LastUserKey);
                    LastUserKey = null;
                    UserSalt = null;
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~NotepadUserSecureData()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
