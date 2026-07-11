using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// キャンセル制御を保持する、基本的なフローコンテキストです。
    /// </summary>
    public class FlowContext : IFlowContext
    {
        /// <summary>
        /// 新しいコンテキストを作成します。
        /// </summary>
        public FlowContext()
        {
        }

        /// <summary>
        /// 指定したキャンセル制御で新しいコンテキストを作成します。
        /// </summary>
        /// <param name="cancellation">このコンテキストで共有するキャンセル制御。</param>
        public FlowContext(CancellationObject cancellation)
        {
            Cancellation = cancellation;
        }

        /// <summary>
        /// このコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation { get; } = new();

        /// <summary>
        /// 基本コンテキストから指定した型の値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。基本実装では常に既定値。</param>
        /// <returns>基本実装では常に <see langword="false"/>。</returns>
        public virtual bool TryGet<T>(out T? value)
        {
            value = default;
            return false;
        }

        /// <summary>
        /// 基本コンテキストに指定した型の値を設定します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="value">設定する値。</param>
        /// <returns>基本実装では常に <see langword="false"/>。</returns>
        public virtual bool TrySet<T>(T? value)
        {
            return false;
        }

        /// <summary>
        /// 基本コンテキストに指定した型の値を生成して設定します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="select">設定する値を生成する関数。</param>
        /// <returns>基本実装では常に <see langword="false"/>。</returns>
        public virtual bool TrySet<T>(Func<T> select)
        {
            return false;
        }
    }
}
