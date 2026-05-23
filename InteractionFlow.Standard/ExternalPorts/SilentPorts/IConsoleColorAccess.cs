using System;

namespace InteractionFlow.Standard.ExternalPorts.SilentPorts
{
    public interface IConsoleColorAccess : ISilentRequestPort<(ConsoleColor foreground, ConsoleColor background), (ConsoleColor? foreground, ConsoleColor? background)>
    {
        public ConsoleColor ForegroundColor { get; set; }

        public ConsoleColor BackgroundColor { get; set; }
    }
}
