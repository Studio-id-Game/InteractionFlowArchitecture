using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ReactionPorts
{
    public static class IConsoleReactionExtention
    {
        public static StateScope<TReaction, ConsoleState> GetStateScope<TReaction>(this TReaction @this)
            where TReaction : class, IConsoleReaction
        {
            return @this.State.GetScope(@this, (e, value) => e.State = value);
        }
    }
}
