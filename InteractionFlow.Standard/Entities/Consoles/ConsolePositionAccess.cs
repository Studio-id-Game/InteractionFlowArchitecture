using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsolePositionAccess(Func<(int, int), (int, int)> update)
    {
        public readonly Func<(int, int), (int, int)> update = update;
    }
}
