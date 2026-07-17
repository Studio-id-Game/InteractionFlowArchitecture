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
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        /// <remarks>
        /// 基本実装では、このコンテキストに紐づく <see cref="Cancellation"/> を取得対象にします。
        /// オーバーライドする場合は、基底クラスが提供する文脈値を保持するために
        /// <see langword="base"/> の実装を明示的に呼び出してください。
        /// </remarks>
        public virtual bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            if (Cancellation is T _value)
            {
                value = _value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
