using System;
using System.IO;

namespace InteractionFlow.Standard.FileSystem.Externals
{
    // 呼び出し側が確定した絶対ルートを使う。ルートの再取得や再解決は行わない。
    internal static class FileSystemPath
    {
        internal static string Resolve(string rootPath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
                throw new InvalidOperationException("The ID must identify a relative path below the root.");

            var path = Path.GetFullPath(Path.Combine(rootPath, relativePath));
            var relative = Path.GetRelativePath(rootPath, path);
            if (relative == "." || relative == ".." || Path.IsPathRooted(relative) ||
                relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The target must be below the root: {path}");
            }

            return path;
        }

        internal static void RequireFile(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
                throw new IOException($"Expected a file: {path}");
        }

        internal static void RequireDirectory(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) == 0)
                throw new IOException($"Expected a directory: {path}");
        }

        internal static bool DirectoryIsPresent(string path)
        {
            try
            {
                RequireDirectory(path);
                return true;
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                // 利用できないドライブ・共有まで「保存データ0件」とは扱わない。
                RequireVolumeRoot(path);
                return false;
            }
        }

        internal static void RequireVolumeRoot(string path)
        {
            RequireDirectory(Path.GetPathRoot(path)!);
        }
    }
}
