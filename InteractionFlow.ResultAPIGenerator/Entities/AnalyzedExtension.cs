using System;
using System.Collections.Immutable;
using System.Linq;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>生成先クラス単位で生成に渡す分析結果です。</summary>
internal sealed class AnalyzedExtension(
    ExtensionClass destination,
    ImmutableArray<AnalyzedCandidate> candidates,
    ImmutableArray<HandwrittenMember> handwrittenMembers) : IEquatable<AnalyzedExtension>
{
    public ExtensionClass Destination { get; } = destination;
    public ImmutableArray<AnalyzedCandidate> Candidates { get; } =
        candidates.IsDefault ? ImmutableArray<AnalyzedCandidate>.Empty : candidates;
    public ImmutableArray<HandwrittenMember> HandwrittenMembers { get; } =
        handwrittenMembers.IsDefault ? ImmutableArray<HandwrittenMember>.Empty : handwrittenMembers;

    public bool Equals(AnalyzedExtension? other) =>
        ReferenceEquals(this, other) ||
        (other is not null && Destination.Equals(other.Destination)
            && Candidates.SequenceEqual(other.Candidates)
            && HandwrittenMembers.SequenceEqual(other.HandwrittenMembers));

    public override bool Equals(object? obj) => Equals(obj as AnalyzedExtension);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Destination.GetHashCode();
            foreach (var candidate in Candidates)
                hash = hash * 31 + candidate.GetHashCode();
            foreach (var member in HandwrittenMembers)
                hash = hash * 31 + member.GetHashCode();
            return hash;
        }
    }
}
