using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Standard.Console.Entities;
using InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Console.Externals.Rules;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Console.Externals.Reactions
{
    /// <summary>
    /// 例外情報をコンソールへ出力する標準 Reaction 実装です。
    /// </summary>
    public class ConsoleExceptionHandling : ExceptionHandling, IConsoleReaction
    {
        /// <summary>
        /// 既定の例外表示状態でインスタンスを作成します。
        /// </summary>
        /// <param name="dependency">この Reaction が依存するフローノード。</param>
        public ConsoleExceptionHandling(params IDependencyNode[] dependency) : base(dependency)
        {
            ResetFields();

            if (State == null)
                throw new ArgumentNullException("state");
        }

        /// <summary>
        /// 例外表示に使用するコンソール状態を取得または設定します。
        /// </summary>
        public ConsoleState State { get; set; }

        /// <summary>
        /// 例外表示状態を既定値へ戻します。
        /// </summary>
        public override void ForceResetMemoryState()
        {
            ResetFields();
        }

        private void ResetFields()
        {
            ThrowException = false;
            State = ConsoleState.Default;
            State.Update(foregroundColor: ConsoleColor.Red);
        }

        /// <summary>
        /// 例外情報をコンソールへ出力し、フロー終了結果を返します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="exception">出力する例外。</param>
        /// <returns>例外表示後のフロー終了結果。</returns>
        protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
        {
            using (var cc = new ConsoleColorScope())
            {
                cc.State = State.ColorSet;
                if (State.WriteLine)
                {
                    global::System.Console.WriteLine();
                    global::System.Console.WriteLine($"* Exception: {exception.GetType().FullName}:");
                    global::System.Console.WriteLine($"\t{exception.Message},");
                    global::System.Console.Write($"\t{exception.Source};");
                }
                else
                {
                    global::System.Console.Write($"* Exception: {exception.GetType().FullName}: {exception.Message}, {exception.Source}; ");
                }
            }

            if (State.WriteLine)
            {
                global::System.Console.WriteLine();
            }

            return new(GetEnd(exception));
        }
    }
}
