using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsolePositionAccess
    {
        public readonly Func<(int, int), (int, int)> update;

        public ConsolePositionAccess(Func<(int, int), (int, int)> update)
        {
            this.update = update;
        }
    }
}
