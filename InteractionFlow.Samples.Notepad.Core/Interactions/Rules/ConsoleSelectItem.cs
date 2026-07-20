using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Console.Entities;
using InteractionFlow.Standard.Console.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Console.ExternalPorts.SilentExternalPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions.Rules
{
    internal readonly struct ConsoleSelectItem<T>(
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        Dictionary<string, T> items
        )
    {
        public IReadOnlyDictionary<string, T> Items => items;

        public async Task<KeyValuePair<string, T>> GetSelectAsync(IFlowContext context)
        {
            int index = 0;
            var keys = Items.Keys.OrderBy(e => e, StringComparer.Ordinal).ToArray();

            using var reactionScope = consoleReaction.GetStateScope();
            reactionScope.State.Update(writeLine: true);

            using var operationScope = consoleOperation.GetStateScope();
            operationScope.State.Update(writeLine: false);

            do
            {
                index = Math.Clamp(index, 0, keys.Length - 1);

                string keyList = "";

                for (var i = 0; i < keys.Length; i++)
                {
                    if (i == index)
                    {
                        keyList += $"  >  {keys[i]}  \n";
                    }
                    else
                    {
                        keyList += $"  - {keys[i]}  \n";
                    }
                }

                await Write(context, keyList.Trim('\n'));

                var input = await consoleOperation.WaitUserKeyAsync(context);

                var inputKey = input.key.Key;

                if (inputKey == ConsoleKey.Enter)
                {
                    var key = keys[index];
                    return new KeyValuePair<string, T>(key, Items[key]);
                }
                else if (inputKey == ConsoleKey.UpArrow || inputKey == ConsoleKey.LeftArrow)
                {
                    if (index > 0)
                    {
                        index--;
                    }
                }
                else if (inputKey == ConsoleKey.DownArrow || inputKey == ConsoleKey.RightArrow)
                {
                    if (index < keys.Length - 1)
                    {
                        index++;
                    }
                }
                else if (TryGetIndex(input, index, out var newIndex))
                {
                    index = newIndex;
                }

                await MoveToHead(keys);

                bool TryGetIndex(ConsoleInputKeyInfo key, int currentIndex, out int newIndex)
                {
                    var c = key.key.KeyChar;

                    var sort = keys
                        .Select((Item, Index) => (Item, Index))
                        .Select(e => (e.Index, HitIndex: e.Item.IndexOf(c, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(e => e.HitIndex)
                        .ToArray();


                    foreach (var item in sort)
                    {
                        if (item.HitIndex < 0)
                            continue;

                        newIndex = item.Index;
                        return true;
                    }

                    newIndex = 0;
                    return false;
                }
            }
            while (true);
        }

        private Task MoveToHead(string[] keys)
        {
            var top = consoleCursorPositionAccess.Position.Top.GetValueOrDefault();
            consoleCursorPositionAccess.Position = new(0, top - keys.Length);
            return Task.CompletedTask;
        }

        private async Task Write(IFlowContext context, string text)
        {
            await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
