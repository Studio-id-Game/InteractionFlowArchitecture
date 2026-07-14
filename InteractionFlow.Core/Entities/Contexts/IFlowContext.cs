using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// SystemFlow や Interaction の実行中に受け渡されるコンテキストを表します。
    /// </summary>
    public interface IFlowContext
    {
        /// <summary>
        /// このコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation { get; }

        /// <summary>
        /// 指定した型として文脈値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        public bool TryGet<T>([MaybeNullWhen(false)] out T value);
    }
}
