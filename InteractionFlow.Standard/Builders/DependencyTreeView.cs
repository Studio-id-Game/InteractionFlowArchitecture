using InteractionFlow.Core.Entities.Architectures;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// <see cref="IDependencyNode"/> 依存ツリーの可視化を提供するクラスです。
    /// </summary>
    public static class DependencyTreeView
    {
        /// <summary>
        /// 循環参照ノードの表示名へ付加する既定の接尾文字列を表します。
        /// </summary>
        public const string DefaultCircularReferenceMarker = "@Cycle";

        /// <summary>
        /// <paramref name="root"/> を根とした依存ツリーを可視化する Markdown ツリー文字列を構築します。
        /// </summary>
        /// <param name="root">依存ツリーの根となる <see cref="IDependencyNode"/></param>
        /// <returns>依存ツリーを可視化する Markdown ツリー文字列</returns>
        /// <remarks>
        /// 現在の再帰経路に同じノードが現れた場合は循環マーカーを付け、そのノードから先の再帰を停止します。
        /// 別の分岐から参照される共有ノードは、それぞれの分岐で表示します。
        /// </remarks>
        public static string GetDependencyTreeText(IDependencyNode root)
        {
            return GetDependencyTreeText(root, DefaultCircularReferenceMarker);
        }

        /// <summary>
        /// <paramref name="root"/> を根とした依存ツリーを、指定された循環マーカーで可視化する Markdown ツリー文字列を構築します。
        /// </summary>
        /// <param name="root">依存ツリーの根となる <see cref="IDependencyNode"/></param>
        /// <param name="circularReferenceMarker">循環参照ノードの表示名へ付加する接尾文字列。</param>
        /// <returns>依存ツリーを可視化する Markdown ツリー文字列</returns>
        /// <remarks>
        /// 現在の再帰経路に同じノードが現れた場合は循環マーカーを付け、そのノードから先の再帰を停止します。
        /// 別の分岐から参照される共有ノードは、それぞれの分岐で表示します。
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="circularReferenceMarker"/> が <see langword="null"/> の場合。
        /// </exception>
        public static string GetDependencyTreeText(IDependencyNode root, string circularReferenceMarker)
        {
            if (circularReferenceMarker == null)
            {
                throw new ArgumentNullException(nameof(circularReferenceMarker));
            }

            var builder = new StringBuilder();
            var currentPath = new HashSet<IDependencyNode>(DependencyNodeReferenceComparer.Instance);
            AppendDependencyTreeText(builder, root, 0, currentPath, circularReferenceMarker);
            return builder.ToString();
        }

        private static void AppendDependencyTreeText(
            StringBuilder builder,
            IDependencyNode node,
            int depth,
            HashSet<IDependencyNode> currentPath,
            string circularReferenceMarker)
        {
            builder.Append(' ', depth * 2);
            builder.Append("- ");

            if (!currentPath.Add(node))
            {
                builder.Append(node.Name);
                builder.AppendLine(circularReferenceMarker);
                return;
            }

            builder.AppendLine(node.Name);

            try
            {
                foreach (var dependency in node.Dependency.Span)
                {
                    AppendDependencyTreeText(builder, dependency, depth + 1, currentPath, circularReferenceMarker);
                }
            }
            finally
            {
                currentPath.Remove(node);
            }
        }

        private sealed class DependencyNodeReferenceComparer : IEqualityComparer<IDependencyNode>
        {
            public static DependencyNodeReferenceComparer Instance { get; } = new();

            private DependencyNodeReferenceComparer()
            {
            }

            public bool Equals(IDependencyNode? x, IDependencyNode? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IDependencyNode obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
