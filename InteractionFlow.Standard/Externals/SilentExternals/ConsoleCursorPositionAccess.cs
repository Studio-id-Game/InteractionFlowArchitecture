using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{

    /// <summary>
    /// 現在のコンソールカーソル位置を取得または変更する標準 SilentExternal 実装です。
    /// </summary>
    public class ConsoleCursorPositionAccess : SilentRequest<ConsoleCursorPosition, ConsoleCursorPosition>, IConsoleCursorPositionAccess
    {
        /// <summary>
        /// 依存ノードを保持するインスタンスを作成します。
        /// </summary>
        /// <param name="dependency">この SilentExternal が依存するフローノード。</param>
        public ConsoleCursorPositionAccess(params IDependencyNode[] dependency) : base(dependency)
        {
        }

        /// <summary>
        /// 現在のカーソル位置を取得または設定します。
        /// </summary>
        public ConsoleCursorPosition Position
        {
            get => new(Console.CursorLeft, Console.CursorTop);
            set
            {
                if (value.Left.HasValue)
                    Console.CursorLeft = value.Left.Value;

                if (value.Top.HasValue)
                    Console.CursorTop = value.Top.Value;
            }
        }

        /// <summary>
        /// 指定された座標だけを変更し、変更後のカーソル位置を返します。
        /// </summary>
        /// <param name="context">実行時のフローコンテキスト。</param>
        /// <param name="arguments">変更するカーソル位置。未指定の座標は変更しません。</param>
        /// <returns>変更後のカーソル位置。</returns>
        public override ValueTask<ConsoleCursorPosition> ExecuteAsync(IFlowContext context, ConsoleCursorPosition arguments)
        {
            Position = arguments;
            return new(Position);
        }

        /// <summary>
        /// この実装は保持状態を持たないため何もしません。
        /// </summary>
        public override void ForceResetMemoryState()
        {
        }
    }
}
