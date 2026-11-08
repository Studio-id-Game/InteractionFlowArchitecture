using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// 指定された Context 型で実行される Interaction を表します。
    /// </summary>
    /// <typeparam name="TContext">実行に必要な Context の型。</typeparam>
    public interface IInteraction<in TContext> : IInteractionNode
        where TContext : IFlowContext
    {
        /// <summary>指定された Context で Interaction を実行します。</summary>
        /// <param name="context">Interaction に渡す Context。</param>
        /// <returns>Reaction に基づく Interaction の終了結果。</returns>
        Task<InteractionEnd> ExecuteAsync(TContext context);

        /// <summary>Reaction の終了結果から Interaction の終了結果を生成します。</summary>
        /// <param name="reactionEnd">Reaction が生成した終了結果。</param>
        /// <returns>Interaction の終了結果。</returns>
        protected static InteractionEnd GetEnd(ReactionEnd reactionEnd)
        {
            return new(reactionEnd);
        }
    }
}
