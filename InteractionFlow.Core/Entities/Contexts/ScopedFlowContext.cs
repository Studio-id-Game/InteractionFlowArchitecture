using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// 既存のコンテキストに一時的な文脈値を重ねて扱うコンテキストです。
    /// </summary>
    /// <remarks>
    /// 元のコンテキストと <see cref="With{T}(T)"/> で追加した値の所有権は取得せず、
    /// このコンテキストの破棄時にもそれらを破棄しません。
    /// </remarks>
    /// <param name="parentContext">値の探索先となる元のコンテキスト。</param>
    public sealed class ScopedFlowContext(IFlowContext parentContext) : IFlowContext, IDisposable
    {
        private sealed class Box<T>(T value) : Entry<T>(value)
        {
        }

        private List<IEntry>? values = [];

        private List<IEntry> Values => values ?? throw new ObjectDisposedException(nameof(ScopedFlowContext));
        private bool disposedValue;

        /// <summary>
        /// 元のコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation => parentContext.Cancellation;

        /// <summary>
        /// 一時的な文脈値を追加します。
        /// </summary>
        /// <typeparam name="T">追加する値の型。</typeparam>
        /// <param name="value">追加する値。<see langword="null"/> は指定できません。</param>
        /// <returns>現在のスコープ付きコンテキスト。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> が <see langword="null"/> の場合。</exception>
        /// <exception cref="ObjectDisposedException">このコンテキストが破棄済みの場合。</exception>
        public ScopedFlowContext With<T>(T value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Values.Add(new Box<T>(value));

            return this;
        }

        /// <summary>
        /// 追加された一時文脈値を新しい順に探索し、見つからない場合は元のコンテキストから値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        /// <remarks>
        /// 追加された値が Entry の場合は、Entry が保持する値を再帰的に解決します。
        /// Entry が要求型の値を持たない場合は探索を継続し、その他の解決失敗は例外として送出します。
        /// </remarks>
        /// <exception cref="ObjectDisposedException">このコンテキストが破棄済みの場合。</exception>
        /// <exception cref="ResultException">Entry の循環など、値の未発見以外の解決失敗が発生した場合。</exception>
        public bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            for (int i = Values.Count - 1; i >= 0; i--)
            {
                var item = Values[i];

                if (item.Parse<T>().Try(out var v, out var e))
                {
                    value = v;
                    return true;
                }
                else if (e.InnerException is null or not EntryValueNotFoundException)
                {
                    throw e;
                }

            }

            return parentContext.TryGet(out value);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)
                }

                values = null;

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        /// <summary>
        /// このコンテキストが保持する検索状態と参照を解放します。
        /// </summary>
        /// <remarks>
        /// 元のコンテキストと <see cref="With{T}(T)"/> で追加した値は破棄しません。
        /// </remarks>
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
