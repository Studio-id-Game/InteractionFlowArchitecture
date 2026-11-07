using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Externals
{
    public sealed class PasswordInput(IConsoleLineInputPort lines, IConsoleKeyInputPort keys,
        IConsoleOutputPort output, IConsoleEnvironmentPort environment)
        : Operation(lines, keys, output, environment), IPasswordInputPort
    {
        public override void ForceResetMemoryState() { }

        public async Task<SecretPassword?> ReadAsync(IFlowContext context)
        {
            if (environment.IsInputRedirected)
            {
                var line = await lines.ReadLineAsync(context);
                if (line == null) return null;
                if (line.Length > 4096) throw new FormatException("Password exceeds 4096 characters.");
                return new SecretPassword(line.ToCharArray());
            }
            var buffer = new char[4096];
            var count = 0;
            try
            {
                while (true)
                {
                    var key = await keys.ReadKeyAsync(context);
                    string echo;
                    if (key.Key == ConsoleKey.Enter)
                    {
                        var end = await output.WriteLineAsync(context, "");
                        if (end.HasException) throw end.Exception!;
                        return new SecretPassword(buffer[..count]);
                    }
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (count == 0) continue;
                        buffer[--count] = '\0';
                        echo = "\b \b";
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        if (count == buffer.Length) continue;
                        buffer[count++] = key.KeyChar;
                        echo = "*";
                    }
                    else continue;
                    var written = await output.WriteAsync(context, echo);
                    if (written.HasException) throw written.Exception!;
                }
            }
            finally { Array.Clear(buffer); }
        }
    }
}
