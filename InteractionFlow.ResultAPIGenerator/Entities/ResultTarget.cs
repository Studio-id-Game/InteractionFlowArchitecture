using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Self と Other の方向付きの組を表します。</summary>
internal sealed class ResultTarget(
    ResultTypePattern self,
    ResultTypePattern other,
    bool appliesToSelfOnlyTemplates)
{
    public ResultTypePattern Self { get; } = self;
    public ResultTypePattern Other { get; } = other;

    /// <summary>Self だけを使うテンプレートに適用できるかを表します。</summary>
    public bool AppliesToSelfOnlyTemplates { get; } = appliesToSelfOnlyTemplates;

    /// <summary>Self、Other の順に型引数名を取り出し、共有名をまとめます。</summary>
    public ImmutableArray<string> TypeParameterNames { get; } = CollectTypeParameterNames(self, other);

    private static ImmutableArray<string> CollectTypeParameterNames(ResultTypePattern self, ResultTypePattern other)
    {
        var names = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in self.TypeArgumentNames.Concat(other.TypeArgumentNames))
        {
            if (seen.Add(name))
                names.Add(name);
        }
        return names.ToImmutable();
    }
}
