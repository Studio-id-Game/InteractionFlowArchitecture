using InteractionFlow.Core.Entities.Architectures;
using System.Text;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// <see cref="IDependencyNode"/> 依存ツリーの可視化を提供するクラスです。
    /// </summary>
    public static class DependencyTreeView
    {
        /// <summary>
        /// <paramref name="root"/> を根とした依存ツリーを可視化するインデント付き文字列を構築します。
        /// </summary>
        /// <param name="root">依存ツリーの根となる <see cref="IDependencyNode"/></param>
        /// <returns>依存ツリーを可視化するインデント付き文字列</returns>
        public static string GetDependencyTreeText(IDependencyNode root)
        {
            var builder = new StringBuilder();
            AppendDependencyTreeText(builder, root, 0);
            return builder.ToString();
        }

        private static void AppendDependencyTreeText(StringBuilder builder, IDependencyNode node, int depth)
        {
            builder.Append(' ', depth * 4);
            builder.AppendLine(node.ToString());

            foreach (var dependency in node.Dependency.Span)
            {
                AppendDependencyTreeText(builder, dependency, depth + 1);
            }
        }
    }
}
