using System;

namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// Function の状態を一時的に変更し、破棄時に元の状態へ戻すスコープです。
    /// </summary>
    /// <typeparam name="TState">スコープで扱う状態の型。</typeparam>
    /// <param name="target">状態を一時変更する対象。</param>
    public readonly struct FunctionStateScope<TState>(IHasFunctionState<TState> target) : IDisposable
        where TState : IFunctionState<TState>
    {
        private readonly TState unscopedState = target.State.Copy();

        /// <summary>
        /// スコープ内で使用する状態を取得または設定します。
        /// </summary>
        public TState State
        {
            get => target.State;
            set => target.State = value;
        }

        /// <summary>
        /// スコープ作成時に保存した状態へ対象を戻します。
        /// </summary>
        public readonly void Dispose()
        {
            target.State = unscopedState;
        }
    }
}
