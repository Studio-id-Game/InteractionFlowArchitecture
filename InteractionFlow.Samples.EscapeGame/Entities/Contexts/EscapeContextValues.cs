using InteractionFlow.Core.Entities;
using System;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 非公開契約も登録禁止に含め、Withから役割の境界を迂回する経路を作りません。
internal static class EscapeContextValues
{
    internal static void ValidateAdditional(object value)
    {
        if (value is IGameInputReader or IGameInputWriter or IGameSessionAccess or IGameWorldAccess
            or IGameDisplayAccess or IGamePresentationAccess or IGameControlAccess || IsEntry(value))
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
