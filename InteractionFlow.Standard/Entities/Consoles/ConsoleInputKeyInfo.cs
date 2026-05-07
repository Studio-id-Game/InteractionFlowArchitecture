using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsoleInputKeyInfo(ConsoleKeyInfo key)
    {
        public readonly ConsoleKeyInfo key = key;
    }
}
