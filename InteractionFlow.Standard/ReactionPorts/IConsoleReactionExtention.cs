using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ReactionPorts
{
    public static class IConsoleReactionExtention
    {
        public static StateScope<TReaction, ConsoleState> GetStateScope<TReaction>(this TReaction @this, bool stateApplay = false)
            where TReaction : class, IConsoleReaction
        {
            if (stateApplay)
            {
                @this.OnStateApply();
                return @this.State.GetScope(@this, Setter);
                static void Setter(TReaction e, ConsoleState value)
                {
                    e.State = value;
                    e.OnStateApply();
                }
            }
            else
            {
                return @this.State.GetScope(@this, Setter);
                static void Setter(TReaction e, ConsoleState value)
                {
                    e.State = value;
                }
            }

        }
    }
}
