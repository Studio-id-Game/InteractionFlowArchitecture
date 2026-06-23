using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class NoteEdit(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles,
        INotepadDataFiles notepadDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadUserDataFiles, notepadDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {

                await WriteLine(context, "# Note Edit - Select exist note name:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await consoleReaction.Write(context, new ConsoleOutput("> Not found NotepadUserKey in context."));
                }

                var userDataResult = await notepadUserDataFiles.LoadFromPersistentAsync(context);

                if (!userDataResult)
                {
                    return await consoleReaction.Write(context, new ConsoleOutput("> Not found NotepadUserData."));
                }

                var userData = userDataResult.Value!;

                var detaKeySelect = new ConsoleSelectNotepadData(consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadDataFiles);

                var (select, dataKey) = await detaKeySelect.GetSelectAsync(context, userData);

                if (dataKey.IsEmpty)
                {
                    return await consoleReaction.Write(context, new ConsoleOutput("> Cancel."));
                }

                if (!context.TrySet(dataKey))
                {
                    return await consoleReaction.Write(context, new ConsoleOutput("> Can not set NotepadDataKey in context."));
                }

                var loadResult = await notepadDataFiles.LoadFromPersistentAsync(context);
                if (!loadResult)
                {
                    return await consoleReaction.Write(context, new ConsoleOutput("> Can not load NotepadData."));
                }

                var notepad = loadResult.Value!;
                List<string> textLines = [notepad.Title, .. notepad.Text.Split('\n').Select(e => e.Trim('\r'))];

                const string LineClear = "                                                                       ";
                const string Separater = "----------";

                await WriteLine(context, Separater);


                int currentLine = 0;
                int currentLeft = 0;

                while (true)
                {
                    await ReWriteAsync();

                    using var scope = consoleOperation.GetStateScope();
                    scope.State.Update(writeLine: false, cancelWaitTime: 0);

                    var key = await consoleOperation.WaitUserKeyAsync(context, true);

                    if (key.key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    else if (key.key.Key == ConsoleKey.UpArrow)
                    {
                        MoveLine(-1);
                    }
                    else if (key.key.Key == ConsoleKey.DownArrow)
                    {
                        MoveLine(1);
                    }
                    else if (key.key.Key == ConsoleKey.LeftArrow)
                    {
                        MoveRight(-1);
                    }
                    else if (key.key.Key == ConsoleKey.RightArrow)
                    {
                        MoveRight(1);
                    }
                    else if (key.key.Key == ConsoleKey.Enter)
                    {
                        Enter();
                    }
                    else if (key.key.Key == ConsoleKey.Backspace)
                    {
                        Backspace();
                    }
                    else if (key.key.Key == ConsoleKey.Delete)
                    {
                        if (MoveRight(1))
                        {
                            Backspace();
                        }

                    }
                    else if (key.key.KeyChar != '\0')
                    {
                        var text = textLines[currentLine].ToList();

                        if (key.key.KeyChar == '\t')
                        {
                            text.InsertRange(currentLeft, "    ");
                            textLines[currentLine] = new string([.. text]);
                            MoveRight(4);
                        }
                        else
                        {
                            text.Insert(currentLeft, key.key.KeyChar);
                            textLines[currentLine] = new string([.. text]);
                            MoveRight(1);
                        }

                    }
                }

                MoveToEndLine();

                notepad.Title = textLines[0];
                notepad.Text = string.Join(Environment.NewLine, textLines.ToArray()[1..]);

                await WriteLine(context, "> Save note ...");
                await Task.Delay(500);
                var saveResult = await notepadDataFiles.SaveToPersistentAsync(context);

                if (saveResult)
                {
                    return await WriteLine(context, $"> Saved note as '{dataKey.UserKey.Name}/{dataKey.NoteId}'");
                }
                else
                {
                    return await WriteLine(context, $"> Can not saved note as '{dataKey.UserKey.Name}/{dataKey.NoteId}'");
                }

                async Task ReWriteAsync()
                {
                    var _currentLine = currentLine;
                    var _currentLeft = currentLeft;

                    MoveLine(-_currentLine);
                    MoveRightTo(0);

                    string[] lines = [.. textLines.Select(e => e + LineClear), Separater, LineClear];

                    using (var titleScope = consoleReaction.GetStateScope())
                    {
                        titleScope.State.Update(foregroundColor: ConsoleColor.Green);
                        await WriteLine(context, lines[0]);
                    }
                    await Write(context, string.Join(Environment.NewLine, lines[1..]));
                    currentLine = textLines.Count + 1; // 後置の Separater と LineClear のLine増加分も考慮
                    currentLeft = textLines.Last().Length;

                    MoveLine(_currentLine - currentLine);
                    MoveRightTo(_currentLeft);
                }

                void MoveRightTo(int value)
                {
                    var newLeft = Math.Clamp(value, -1, textLines[currentLine].Length + 1);

                    if (0 <= newLeft && newLeft <= textLines[currentLine].Length)
                    {
                        var pos = consoleCursorPositionAccess.Position;
                        currentLeft = value;
                        var top = pos.Top.GetValueOrDefault();
                        consoleCursorPositionAccess.Position = new(currentLeft, top);
                    }
                }

                bool MoveLine(int value)
                {
                    var newLine = Math.Clamp(currentLine + value, -1, textLines.Count);
                    if (0 <= newLine && newLine < textLines.Count)
                    {
                        currentLine += value;
                        var pos = consoleCursorPositionAccess.Position;
                        currentLeft = Math.Min(pos.Left.GetValueOrDefault(), textLines[newLine].Length);
                        var top = pos.Top.GetValueOrDefault() + value;
                        consoleCursorPositionAccess.Position = new(currentLeft, top);

                        return true;
                    }

                    return false;
                }

                bool MoveRight(int value)
                {
                    var newLeft = Math.Clamp(currentLeft + value, -1, textLines[currentLine].Length + 1);
                    var pos = consoleCursorPositionAccess.Position;
                    bool result = false;

                    if (newLeft < 0)
                    {
                        if (MoveLine(-1))
                        {
                            currentLeft = textLines[currentLine].Length;
                            result = true;
                        }
                    }
                    else if (newLeft <= textLines[currentLine].Length)
                    {
                        currentLeft = pos.Left.GetValueOrDefault() + value;
                        result = true;
                    }
                    else if (MoveLine(1))
                    {
                        currentLeft = 0;
                        result = true;
                    }

                    if (result)
                    {
                        var top = consoleCursorPositionAccess.Position.Top.GetValueOrDefault();
                        consoleCursorPositionAccess.Position = new(currentLeft, top);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                void Backspace()
                {
                    if (currentLeft > 0)
                    {
                        MoveRight(-1);
                        var text = textLines[currentLine].ToList();
                        text.RemoveAt(currentLeft);
                        textLines[currentLine] = new string([.. text]);
                    }
                    else if (currentLeft == 0 && currentLine > 1)
                    {
                        MoveRight(-1);

                        textLines[currentLine] += textLines[currentLine + 1];
                        textLines.RemoveAt(currentLine + 1);
                    }
                }

                void Enter()
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

                    MoveLine(1);
                }

                void MoveToEndLine()
                {
                    var value = textLines.Count + 1 - currentLine;
                    currentLine += value;

                    var top = consoleCursorPositionAccess.Position.Top.GetValueOrDefault();
                    consoleCursorPositionAccess.Position = new(0, top + value);
                }
            });

            return await consoleReaction.Write(context, new ConsoleOutput($"> End of Edit"));
        }

        private async Task<FlowEndToken> WriteLine(IFlowContext context, string text)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: true);
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: false);
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
