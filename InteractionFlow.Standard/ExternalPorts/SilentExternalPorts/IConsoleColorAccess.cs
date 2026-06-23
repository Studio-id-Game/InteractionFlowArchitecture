using System;

namespace InteractionFlow.Standard.ExternalPorts.SilentExternalPorts
{
    public interface IConsoleColorAccess : ISilentRequestPort<(ConsoleColor foreground, ConsoleColor background), (ConsoleColor? foreground, ConsoleColor? background)>
    {
        public ConsoleColor ForegroundColor { get; set; }

        public ConsoleColor BackgroundColor { get; set; }
    }
}
