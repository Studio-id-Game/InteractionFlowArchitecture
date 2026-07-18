using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
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
        INotepadUserDataStoragePort notepadUserDataFiles,
        INotepadDataStoragePort notepadDataFiles,
        INotepadUserDataPersistencePort notepadUserDataPersistence,
        INotepadDataPersistencePort notepadDataPersistence) :
        Interaction(
            exceptionPort,
            cancellationPort,
            consoleReaction,
            consoleCursorPositionAccess,
            consoleOperation,
            notepadUserDataFiles,
            notepadDataFiles,
            notepadUserDataPersistence,
            notepadDataPersistence)
    {
        private class ConsoleTextWriter(IConsoleCursorPositionAccess consoleCursorPositionAccess, IConsoleWriter consoleReaction, IConsoleOperation consoleOperation)
        {
            const string LineClear = "                                                                       ";
            const string Separater = "----------";

            private int currentLine = 0;
            private int currentLeft = 0;

            private List<string> textLines = [];

            public IReadOnlyList<string> TextLines => textLines;

            public async Task InitAsync(IFlowContext context, NotepadData notepad)
            {
                textLines = [notepad.Title, .. notepad.Text.Split('\n').Select(e => e.Trim('\r'))];

                await WriteLine(context, Separater);
            }

            public async Task<bool> Next(IFlowContext context)
            {
                await ReWriteAsync(context);

                using var scope = consoleOperation.GetStateScope();
                scope.State.Update(writeLine: false, cancelWaitTime: 0);

                var key = await consoleOperation.WaitUserKeyAsync(context, true);

                switch (key.key.Key)
                {
                    case ConsoleKey.Escape:
                        MoveToEndLine();
                        return false;

                    case ConsoleKey.UpArrow:
                        MoveLine(-1);
                        break;

                    case ConsoleKey.DownArrow:
                        MoveLine(1);
                        break;

                    case ConsoleKey.LeftArrow:
                        MoveRight(-1);
                        break;

                    case ConsoleKey.RightArrow:
                        MoveRight(1);
                        break;

                    case ConsoleKey.Enter:
                        Enter();
                        break;

                    case ConsoleKey.Backspace:
                        Backspace();
                        break;

                    case ConsoleKey.Delete:
                        if (MoveRight(1))
                            Backspace();
                        break;
                    default:
                        var ch = key.key.KeyChar;
                        if (ch != '\0' && ch != '\b')
                        {
                            var text = textLines[currentLine].ToList();

                            if (ch == '\t')
                            {
                                text.InsertRange(currentLeft, "    ");
                                textLines[currentLine] = new string([.. text]);
                                MoveRight(4);
                            }
                            else
                            {
                                text.Insert(currentLeft, ch);
                                textLines[currentLine] = new string([.. text]);
                                MoveRight(1);
                            }

                        }
                        break;
                }

                return true;
            }

            private async Task ReWriteAsync(IFlowContext context)
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

            private void MoveRightTo(int value)
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

            private bool MoveLine(int value)
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

            private bool MoveRight(int value)
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

            private void Backspace()
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

            private void Enter()
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

            private void MoveToEndLine()
            {
                var value = textLines.Count + 1 - currentLine;
                currentLine += value;

                var top = consoleCursorPositionAccess.Position.Top.GetValueOrDefault();
                consoleCursorPositionAccess.Position = new(0, top + value);
            }

            private async Task<ReactionEnd> WriteLine(IFlowContext context, string text)
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);
                return await consoleReaction.Write(context, new ConsoleOutput(text));
            }

            private async Task<ReactionEnd> Write(IFlowContext context, string text)
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: false);
                return await consoleReaction.Write(context, new ConsoleOutput(text));
            }
        }

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            await WriteLine(context, "# Note Edit");

            await Write(context, "> Loading User data...");

            return await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context).ThenAsync(async userData =>
            {
                var detaKeySelect = new ConsoleSelectNotepadData(
                    consoleReaction,
                    consoleCursorPositionAccess,
                    consoleOperation,
                    notepadDataFiles,
                    notepadDataPersistence);

                await WriteLine(context, "- Select exist note name:");
                var (select, notepadDataKey) = await detaKeySelect.GetSelectAsync(context, userData, false);

                if (notepadDataKey.IsEmpty)
                {
                    return new Exception("Cancel.");
                }
                else
                {
                    return notepadDataKey.AsResult();
                }
            })
                .ThenAsync(notepadDataKey =>
                {
                    return Task.FromResult(notepadDataFiles.GetOrCreate(notepadDataKey));
                })
                .ThenAsync(async notepadEntry =>
                {
                    var notepadDataKey = notepadEntry.PersistenceId;

                    return await notepadEntry.Load(notepadDataPersistence)
                        .ThenErrorAsync(e => Task.FromResult<Result<NotepadData>>(new Exception($"Can not load note as '{notepadDataKey.UserKey.Name}/{notepadDataKey.NoteId}'")))
                        .ThenAsync(notepad => Task.FromResult((notepad, notepadEntry).AsResult()));
                })
                .ThenAsync(async e =>
                {
                    var (notepad, notepadEntry) = e;
                    var notepadDataKey = notepadEntry.PersistenceId;

                    var consoleWriter = new ConsoleTextWriter(consoleCursorPositionAccess, consoleReaction, consoleOperation);
                    await consoleWriter.InitAsync(context, notepad);

                    while (await consoleWriter.Next(context)) { }

                    notepad.Title = consoleWriter.TextLines[0];
                    notepad.Text = string.Join(Environment.NewLine, consoleWriter.TextLines.ToArray()[1..]);

                    await WriteLine(context, "> Save note ...");
                    await Task.Delay(500);

                    return await notepadEntry.Save(notepadDataPersistence)
                        .ThenErrorAsync(e => Task.FromResult<Result>(new Exception($"Can not saved note as '{notepadDataKey.UserKey.Name}/{notepadDataKey.NoteId}'")))
                        .ThenAsync(() => Task.FromResult(notepadDataKey.AsResult()));
                })
                .ResolveAsync(
                onSuccess: async notepadDataKey =>
                {
                    return await WriteLine(context, $"> Note Edit End : Saved note as '{notepadDataKey.UserKey.Name}/{notepadDataKey.NoteId}'");
                },
                onFailure: async e =>
                {
                    return await WriteLine(context, $"> Note Edit Error : {e.Message}");
                });
        }

        private async Task<ReactionEnd> WriteLine(IFlowContext context, string text)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: true);
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }

        private async Task<ReactionEnd> Write(IFlowContext context, string text)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: false);
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
