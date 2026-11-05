using System;
using System.IO;

namespace InteractionFlow.Standard.FileSystem.Externals.Storages.Persistences
{
    /// <summary>
    /// ルートパスを基準としたパスの検証とディレクトリ作成を提供するユーティリティです。
    /// </summary>
    public static class DirectoryUtility
    {
        /// <summary>
        /// 指定されたパスを正規化し、ルートパス自身またはその配下にあることを検証します。
        /// </summary>
        /// <param name="root">基準となるルートパス。</param>
        /// <param name="target">検証する対象パス。</param>
        /// <param name="allowRoot">対象としてルートパス自身を許可する場合は <see langword="true"/>。</param>
        /// <returns>正規化された対象パス。</returns>
        /// <exception cref="InvalidOperationException">
        /// 対象がルートパス外にある場合、または許可されていないルートパス自身を表す場合に発生します。
        /// </exception>
        public static string GetPathInRoot(string root, string target, bool allowRoot = true)
        {
            var fullRoot = Path.GetFullPath(root);
            var fullTarget = Path.GetFullPath(target);
            var relative = Path.GetRelativePath(fullRoot, fullTarget);

            if (Path.IsPathRooted(relative) ||
                relative == ".." ||
                relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Target path is outside the root.\nRoot  : {fullRoot}\nTarget: {fullTarget}");
            }

            if (!allowRoot && relative == ".")
            {
                throw new InvalidOperationException(
                    $"Target path must be below the root.\nRoot  : {fullRoot}\nTarget: {fullTarget}");
            }

            return fullTarget;
        }

        /// <summary>
        /// 既存のルートパス配下に限定して、対象ディレクトリを作成します。
        /// ルートパス自身は作成しません。
        /// </summary>
        /// <param name="root">作成を許可するルートディレクトリ。</param>
        /// <param name="target">作成対象のディレクトリ。</param>
        /// <exception cref="DirectoryNotFoundException">ルートディレクトリが存在しない場合に発生します。</exception>
        /// <exception cref="InvalidOperationException">作成対象がルート配下ではない場合に発生します。</exception>
        public static void CreateDirectories(string root, string target)
        {
            var fullRoot = Path.GetFullPath(root);

            if (!Directory.Exists(fullRoot))
            {
                throw new DirectoryNotFoundException($"Root directory does not exist: {fullRoot}");
            }

            Directory.CreateDirectory(GetPathInRoot(fullRoot, target));
        }
    }
}
