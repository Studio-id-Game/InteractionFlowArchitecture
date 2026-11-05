using System;
using System.IO;

namespace InteractionFlow.Standard.Tests.FileSystem
{
    internal sealed class FileSystemTestDirectory : IDisposable
    {
        private static readonly string Parent = Path.Combine(Path.GetTempPath(), "InteractionFlow.FileSystemTests");
        internal string Root { get; } = Path.Combine(Parent, Guid.NewGuid().ToString("N"));

        internal string PathFor(params string[] parts)
        {
            var path = Root;
            foreach (var part in parts) path = Path.Combine(path, part);
            return path;
        }

        public void Dispose()
        {
            var fullPath = Path.GetFullPath(Root);
            var relative = Path.GetRelativePath(Path.GetFullPath(Parent), fullPath);
            if (relative == "." || relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                throw new InvalidOperationException("Refusing to delete outside the owned test directory.");

            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
        }
    }
}
