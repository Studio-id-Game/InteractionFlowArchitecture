using System.Diagnostics.CodeAnalysis;

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
        /// このコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation { get; } = new();

        /// <summary>
        /// 基本コンテキストから指定した型の値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。基本実装では常に既定値。</param>
        /// <returns>基本実装では常に <see langword="false"/>。</returns>
        public virtual bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            value = default;
            return false;
        }
    }
}
