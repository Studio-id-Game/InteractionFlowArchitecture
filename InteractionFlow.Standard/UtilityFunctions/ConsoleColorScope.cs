using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using System;

namespace InteractionFlow.Standard.UtilityFunctions
{
    public readonly struct ConsoleColorScope : IHasFunctionState<ConsoleColorSet>
    {
        public ConsoleColorSet State
        {
            get => new(Console.ForegroundColor, Console.BackgroundColor);
            set
            {
                Console.ForegroundColor = value.Foreground;
                Console.BackgroundColor = value.Background;
            }
        }
    }
}
