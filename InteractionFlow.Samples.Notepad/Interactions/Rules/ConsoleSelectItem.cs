using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions.Rules
{
    internal readonly struct ConsoleSelectItem<T>(
        IConsoleReaction consoleReaction,
        IConsoleOperation consoleOperation,
        Dictionary<string, T> items
        )
    {
        public IReadOnlyDictionary<string, T> Items => items;

        public async Task<KeyValuePair<string, T>> GetSelectAsync(IFlowContext context)
        {
            int index = 0;
            var keys = Items.Keys.Order().ToArray();

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

                var input = await consoleOperation.UserOperateKeyInfoAsync(context);

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
                else if (TryGetIndex(input, out var newIndex))
                {
                    index = newIndex;
                }

                await MoveToHead(context, keys);

                bool TryGetIndex(ConsoleInputKeyInfo key, out int index)
                {
                    var c = key.key.KeyChar;

                    foreach (var item in keys.Index())
                    {
                        if (item.Item[0] == c)
                        {
                            index = item.Index;
                            return true;
                        }
                    }

                    index = -1;
                    return false;
                }
            }
            while (true);
        }

        private async Task MoveToHead(IFlowContext context, string[] keys)
        {
            await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess((lefttop) =>
            {
                return (0, lefttop.Item2 - keys.Length);
            }));
        }

        private async Task Write(IFlowContext context, string text)
        {
            await consoleReaction.ReactToUserAsync(context, new ConsoleOutput(text));
        }
    }
}
