using System;

namespace InteractionFlow.Core.Entities;

/// <summary>Entry の解決が展開上限に到達したことを表します。</summary>
public sealed class EntryResolutionLimitException : InvalidOperationException
{
    internal EntryResolutionLimitException(Type requestedType, int depthLimit, bool cycleDetected, int diagnosticDepth)
        : base($"Entry resolution reached its expansion limit ({depthLimit}). Requested type: {requestedType.FullName}. "
            + (cycleDetected ? "A cycle was detected within the diagnostic range."
                : "No cycle was detected within the diagnostic range; this does not establish absence of cycles."))
    {
        RequestedType = requestedType;
        DepthLimit = depthLimit;
        CycleDetected = cycleDetected;
        DiagnosticDepth = diagnosticDepth;
    }

    /// <summary>要求された値の型。</summary>
    public Type RequestedType { get; }
    /// <summary>解決時の展開上限。</summary>
    public int DepthLimit { get; }
    /// <summary>追加診断の範囲内で循環を検出したか。</summary>
    public bool CycleDetected { get; }
    /// <summary>追加診断で展開した Entry の数。</summary>
    public int DiagnosticDepth { get; }
}
