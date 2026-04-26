using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    public readonly struct ConsoleInputKeyInfo
    {
        public readonly ConsoleKeyInfo key;

        public ConsoleInputKeyInfo(ConsoleKeyInfo key)
        {
            this.key = key;
        }
    }
}
