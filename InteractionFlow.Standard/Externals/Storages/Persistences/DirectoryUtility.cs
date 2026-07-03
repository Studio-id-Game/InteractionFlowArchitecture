using System;
using System.IO;

namespace InteractionFlow.Standard.Externals.Storages.Persistences
{
    public static class DirectoryUtility
    {
        /// <summary>
        /// root より下のディレクトリのみを再帰的に作成する。
        /// root 自体は作成しない。
        /// </summary>
        public static void CreateDirectories(string root, string target)
        {
            root = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            target = Path.GetFullPath(target)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"Route directory does not exist: {root}");

            // target が route 配下か確認
            string relative = Path.GetRelativePath(root, target);

            if (relative.StartsWith("..") || Path.IsPathRooted(relative))
                throw new InvalidOperationException(
                    $"Target directory is outside the route.\nRoute : {root}\nTarget: {target}");

            string current = root;

            foreach (string part in relative.Split(
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar))
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                current = Path.Combine(current, part);

                if (!Directory.Exists(current))
                    Directory.CreateDirectory(current);
            }
        }
    }
}
