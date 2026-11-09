using InteractionFlow.Core.Entities;
using System;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal static class ContextAccessValidation
{
    internal static void Validate(object value)
    {
        if (value is IGameplayAccess or IGameViewAccess or IApplicationLifetimeAccess || IsEntry(value))
            throw new ArgumentException("Fixed game access contracts and Entry values cannot be added.", nameof(value));
    }

    private static bool IsEntry(object value)
    {
        for (var type = value.GetType(); type is not null; type = type.BaseType)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Entry<>))
                return true;
        return false;
    }
}
