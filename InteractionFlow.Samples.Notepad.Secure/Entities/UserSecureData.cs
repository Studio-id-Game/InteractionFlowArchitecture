using System;
using System.Security.Cryptography;

namespace InteractionFlow.Samples.Notepad.Secure.Entities
{
    public class UserSecureData : IDisposable
    {
        private bool disposedValue;
        private byte[]? lastUserKey;
        private byte[]? userSalt;

        public byte[]? LastUserKey
        {
            get => lastUserKey;
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                lastUserKey = value;
            }
        }

        public byte[]? UserSalt
        {
            get => userSalt;
            set
            {
                ObjectDisposedException.ThrowIf(disposedValue, this);
                userSalt = value;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    CryptographicOperations.ZeroMemory(lastUserKey);
                    CryptographicOperations.ZeroMemory(userSalt);
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                LastUserKey = null;
                userSalt = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
