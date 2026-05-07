using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.Interactions.Rules;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteEdit(
        IExceptionPort exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleReaction consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles,
        INotepadDataFiles notepadDataFiles) :
        Interaction(exceptionPort, cancellationPort)
    {
        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            await TryCatchBlock(context, async context =>
            {
                await WriteLine(context, "# Note Edit - Enter exist note name:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Not found NotepadUserKey in context."));
                }

                var userDataResult = await notepadUserDataFiles.LoadFromPersistent(context);

                if (!userDataResult)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Not found NotepadUserData."));
                }

                var userData = userDataResult.Value!;

                var detaKeySelect = new ConsoleSelectNotepadData(consoleReaction, consoleOperation);

                var (select, dataKey) = await detaKeySelect.GetSelectAsync(context, userData);

                if (dataKey.IsEmpty)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Cancel."));
                }

                if (!context.TrySet(dataKey))
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not set NotepadDataKey in context."));
                }

                var loadResult = await notepadDataFiles.LoadFromPersistent(context);
                if (!loadResult)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not load NotepadData."));
                }

                var notepad = loadResult.Value!;
                List<string> textLines = [notepad.Title, .. notepad.Text.Split('\n').Select(e => e.Trim('\r'))];

                const string LineClear = "                                                                       ";
                const string Separater = "----------";

                await WriteLine(context, Separater);

                int currentLine = 0;
                int currentLeft = 0;

                var oldCancelWaitTime = consoleOperation.CancelWaitTime;
                consoleOperation.CancelWaitTime = 0;

                while (true)
                {
                    await ReWriteAsync();

                    var key = await consoleOperation.UserOperateKeyInfoAsync(context, true);

                    if (key.key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    else if (key.key.Key == ConsoleKey.UpArrow)
                    {
                        await MoveLine(-1);
                    }
                    else if (key.key.Key == ConsoleKey.DownArrow)
                    {
                        await MoveLine(1);
                    }
                    else if (key.key.Key == ConsoleKey.LeftArrow)
                    {
                        await MoveRight(-1);
                    }
                    else if (key.key.Key == ConsoleKey.RightArrow)
                    {
                        await MoveRight(1);
                    }
                    else if (key.key.Key == ConsoleKey.Enter)
                    {
                        await EnterAsync();
                    }
                    else if (key.key.Key == ConsoleKey.Backspace)
                    {
                        await BackspaceAsync();
                    }
                    else if (key.key.Key == ConsoleKey.Delete)
                    {
                        if (await MoveRight(1))
                        {
                            await BackspaceAsync();
                        }

                    }
                    else if (key.key.KeyChar != '\0')
                    {
                        var text = textLines[currentLine].ToList();

                        if (key.key.KeyChar == '\t')
                        {
                            text.InsertRange(currentLeft, "    ");
                            textLines[currentLine] = new string([.. text]);
                            await MoveRight(4);
                        }
                        else
                        {
                            text.Insert(currentLeft, key.key.KeyChar);
                            textLines[currentLine] = new string([.. text]);
                            await MoveRight(1);
                        }

                    }
                }

                consoleOperation.CancelWaitTime = oldCancelWaitTime;

                await MoveEnd();

                notepad.Title = textLines[0];
                notepad.Text = string.Join(Environment.NewLine, textLines[1..]);

                await WriteLine(context, "> Save note ...");
                var saveResult = await notepadDataFiles.SaveToPersistent(context);

                if (saveResult)
                {
                    return await WriteLine(context, $"> Saved note as '{dataKey.UserKey.Name}/{dataKey.NoteId}'");
                }
                else
                {
                    return await WriteLine(context, $"> Can not saved note as '{dataKey.UserKey.Name}/{dataKey.NoteId}'");
                }

                async Task MoveEnd()
                {
                    var value = textLines.Count + 1 - currentLine;
                    currentLine += value;
                    await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                    {
                        return (0, e.Item2 + value);
                    }));
                }

                async Task<bool> MoveLine(int value)
                {
                    var newLine = Math.Clamp(currentLine + value, -1, textLines.Count);
                    if (0 <= newLine && newLine < textLines.Count)
                    {
                        currentLine += value;
                        await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                        {
                            return (currentLeft = Math.Min(e.Item1, textLines[newLine].Length), e.Item2 + value);
                        }));

                        return true;
                    }

                    return false;
                }

                async Task<bool> MoveRight(int value)
                {
                    var newLeft = Math.Clamp(currentLeft + value, -1, textLines[currentLine].Length + 1);
                    if (newLeft < 0)
                    {
                        if (await MoveLine(-1))
                        {
                            await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                            {
                                return (currentLeft = textLines[currentLine].Length, e.Item2);
                            }));

                            return true;
                        }
                    }
                    else if (newLeft <= textLines[currentLine].Length)
                    {
                        await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                        {
                            return (currentLeft = e.Item1 + value, e.Item2);
                        }));

                        return true;
                    }
                    else if (await MoveLine(1))
                    {
                        await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                        {
                            return (currentLeft = 0, e.Item2);
                        }));

                        return true;
                    }

                    return false;
                }

                async Task MoveRightTo(int value)
                {
                    var newLeft = Math.Clamp(value, -1, textLines[currentLine].Length + 1);

                    if (0 <= newLeft && newLeft <= textLines[currentLine].Length)
                    {
                        await consoleReaction.ReactToUserAsync(context, new ConsolePositionAccess(e =>
                        {
                            return (currentLeft = value, e.Item2);
                        }));
                    }
                }

                async Task BackspaceAsync()
                {
                    if (currentLeft > 0)
                    {
                        await MoveRight(-1);
                        var text = textLines[currentLine].ToList();
                        text.RemoveAt(currentLeft);
                        textLines[currentLine] = new string([.. text]);
                    }
                    else if (currentLeft == 0 && currentLine > 1)
                    {
                        await MoveRight(-1);

                        textLines[currentLine] += textLines[currentLine + 1];
                        textLines.RemoveAt(currentLine + 1);
                    }
                }

                async Task EnterAsync()
                {
                    var text = textLines[currentLine];
                    if (text == "")
                    {
                        textLines.Insert(currentLine + 1, "");
                    }
                    else
                    {
                        var textA = text[..(currentLeft)];
                        var textB = text[currentLeft..];
                        textLines[currentLine] = textA;
                        textLines.Insert(currentLine + 1, textB);
                    }
                    await MoveLine(1);
                }

                async Task ReWriteAsync()
                {
                    var _currentLine = currentLine;
                    var _currentLeft = currentLeft;

                    await MoveLine(-_currentLine);
                    await MoveRightTo(0);

                    string[] lines = [.. textLines.Select(e => e + LineClear), Separater, LineClear];
                    await Write(context, string.Join(Environment.NewLine, lines));
                    currentLine = textLines.Count + 1; // 後置の Separater と LineClear のLine増加分も考慮
                    currentLeft = textLines.Last().Length;

                    await MoveLine(_currentLine - currentLine);
                    await MoveRightTo(_currentLeft);
                }
            });

            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> End of Edit"));
        }

        private async Task<FlowEndToken> WriteLine(IFlowContext context, string text)
        {
            using var scope = consoleReaction.State.Customize(e => consoleReaction.State = e);
            scope.Set(writeLine: true);
            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput(text));
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            using var scope = consoleReaction.State.Customize(e => consoleReaction.State = e);
            scope.Set(writeLine: false);
            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput(text));
        }
    }
}
