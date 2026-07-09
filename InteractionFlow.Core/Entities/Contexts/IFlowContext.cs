namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// ProgramFlow や Interaction の実行中に受け渡されるコンテキストを表します。
    /// </summary>
    public interface IFlowContext : IFlowContextValue
    {
        /// <summary>
        /// このコンテキストに紐づくユーザー情報を取得します。
        /// </summary>
        public UserObject User { get; }

        /// <summary>
        /// このコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation { get; }
    }
}
