using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Externals.Reactions
{
    /// <summary>
    /// <see cref="Exception"/> を扱う例外ハンドリング Reaction のデフォルト実装基底クラスです。
    /// </summary>
    /// <param name="dependency">この Reaction が依存するフローノード。</param>
    public abstract class ExceptionHandling(params IDependencyNode[] dependency) : ExceptionHandling<Exception>(dependency)
    {
    }

    /// <summary>
    /// 指定した例外型を扱う例外ハンドリング Reaction のデフォルト実装基底クラスです。
    /// </summary>
    /// <typeparam name="TException">この Reaction が処理する例外の型。</typeparam>
    /// <param name="dependency">この Reaction が依存するフローノード。</param>
    public abstract class ExceptionHandling<TException>(params IDependencyNode[] dependency) : Reaction(dependency), IExceptionPort<TException>
        where TException : Exception
    {
        /// <summary>
        /// 例外をフロー終了結果へ変換せず、そのまま再送出するかどうかを取得または設定します。
        /// </summary>
        public bool ThrowException { get; set; } = false;

        /// <summary>
        /// 指定された例外を処理し、設定に応じて再送出または派生クラスの処理へ委譲します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理する例外。</param>
        /// <returns>例外処理後のフロー終了結果。</returns>
        /// <exception cref="Exception"><see cref="ThrowException"/> が <see langword="true"/> の場合、指定された例外を再送出します。</exception>
        public ValueTask<ReactionEnd> HandleExceptionAsync(IFlowContext context, TException exception)
        {
            if (ThrowException)
            {
                throw exception;
            }
            else
            {
                return HandleExceptionCoreAsync(context, exception);
            }
        }

        /// <summary>
        /// 例外をフロー終了結果へ変換する派生クラス固有の処理を実行します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理する例外。</param>
        /// <returns>例外処理後のフロー終了結果。</returns>
        protected abstract ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, TException exception);
    }
}
