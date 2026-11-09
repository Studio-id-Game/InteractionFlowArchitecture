using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace InteractionFlow.Core.Entities;

internal static class EntryResolver
{
    internal const int MaximumDepth = 100;

    private sealed class ReferenceComparer : IEqualityComparer<IEntry>
    {
        internal static readonly ReferenceComparer Instance = new();
        public bool Equals(IEntry? x, IEntry? y) => ReferenceEquals(x, y);
        public int GetHashCode(IEntry value) => RuntimeHelpers.GetHashCode(value);
    }

    internal static bool TryResolve<T>(IEntry root, bool reportMissing,
        [MaybeNullWhen(false)] out T value, out Exception? error)
    {
        var current = root;
        var depth = 0;
        value = default;
        error = null;

        while (true)
        {
            // Context の格納用 Box は利用者が構成した Entry の深さに含めません。
            if (!current.IsContextBox && depth++ == MaximumDepth)
            {
                error = Diagnose(root, typeof(T));
                return false;
            }

            var content = current.UntypedValue;
            if (content is T found)
            {
                value = found;
                return true;
            }

            if (content is IEntry nested)
            {
                current = nested;
                continue;
            }

            if (reportMissing)
                error = new EntryValueNotFoundException(content is null
                    ? $"Entry value is null. Requested type: {typeof(T).FullName}."
                    : $"Entry value type mismatch. Requested type: {typeof(T).FullName}. Actual type: {content.GetType().FullName}.");
            return false;
        }
    }

    private static EntryResolutionLimitException Diagnose(IEntry root, Type requestedType)
    {
        // 解決失敗は確定済み。再探索の診断結果で成功へ戻すことはしません。
        var visited = new HashSet<IEntry>(ReferenceComparer.Instance);
        var current = root;
        var depth = 0;
        while (true)
        {
            if (!visited.Add(current))
                return new(requestedType, MaximumDepth, true, depth);
            if (!current.IsContextBox && depth == MaximumDepth)
                break;
            if (!current.IsContextBox) depth++;
            if (current.UntypedValue is not IEntry nested) break;
            current = nested;
        }
        return new(requestedType, MaximumDepth, false, depth);
    }
}
