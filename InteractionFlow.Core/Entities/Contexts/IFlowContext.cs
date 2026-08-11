using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// 概念上の Context のうち System 側で扱う文脈値とキャンセル制御を、
    /// SystemFlow や Interaction の実行へ提供する実装上の投影を表します。
    /// </summary>
    /// <remarks>
    /// この型は、User と System が共有する Context またはその時間的な過程である
    /// Context Loop そのものを表すものではありません。
    /// </remarks>
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
        /// <remarks>
        /// <see langword="false"/> は値が見つからなかったことを表します。
        /// 実装上の不整合や利用できない状態は、例外として呼び出し側へ伝播する場合があります。
        /// </remarks>
        public bool TryGet<T>([MaybeNullWhen(false)] out T value);
    }
}
