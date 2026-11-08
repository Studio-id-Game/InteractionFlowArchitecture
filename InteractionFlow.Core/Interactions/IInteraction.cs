using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// システム内部の目的を達成するための Interaction を表します。
    /// </summary>
    public interface IInteraction : IInteraction<IFlowContext>
    {
        /// <summary>
        /// 指定されたコンテキストで Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Interaction の終了結果。</returns>
        new Task<InteractionEnd> ExecuteAsync(IFlowContext context);

        /// <summary>
        /// 型付き契約からの実行を、従来の実行契約へ委譲します。
        /// </summary>
        /// <param name="context">Interaction に渡す Context。</param>
        /// <returns>Interaction の終了結果。</returns>
        Task<InteractionEnd> IInteraction<IFlowContext>.ExecuteAsync(IFlowContext context)
        {
            return ExecuteAsync(context);
        }

        /// <summary>
        /// Reaction が生成した終了結果から Interaction の終了結果を生成します。
        /// </summary>
        /// <param name="reactionEnd">Reaction が生成したフロー終了結果。</param>
        /// <returns>Interaction の終了結果。</returns>
        protected new static InteractionEnd GetEnd(ReactionEnd reactionEnd)
        {
            return new(reactionEnd);
        }
    }
}
