using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    // Cursor offsets are UTF-16 indices at text-element boundaries. Row 0 is the title.
    public sealed class NoteDraft
    {
        private readonly List<string> lines;
        private int preferredColumn;
        public IReadOnlyList<string> Lines => lines;
        public int Row { get; private set; }
        public int Column { get; private set; }

        public NoteDraft(NotepadData? original)
        {
            lines = [original?.Title ?? "", .. (original?.Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')];
        }

        public bool Insert(string text)
        {
            if (text.IndexOfAny(['\r', '\n']) >= 0) return false;
            if (Row == 0 && lines[0].Length + text.Length > NotepadRules.MaxTitleLength) return false;
            if (Row > 0 && BodyLength() + text.Length > NotepadRules.MaxTextLength) return false;
            lines[Row] = lines[Row].Insert(Column, text);
            Column = BoundaryAtOrAfter(lines[Row], Column + text.Length);
            preferredColumn = ConsoleCells.Width(lines[Row][..Column]);
            return true;
        }

        public void Left()
        {
            if (Column > 0) Column = Previous(lines[Row], Column);
            else if (Row > 1) { Row--; Column = lines[Row].Length; }
            preferredColumn = ConsoleCells.Width(lines[Row][..Column]);
        }

        public void Right()
        {
            if (Column < lines[Row].Length) Column = Next(lines[Row], Column);
            else if (Row > 0 && Row < lines.Count - 1) { Row++; Column = 0; }
            preferredColumn = ConsoleCells.Width(lines[Row][..Column]);
        }

        public void Vertical(int delta)
        {
            Row = Math.Max(0, Math.Min(lines.Count - 1, Row + delta));
            Column = ConsoleCells.OffsetAtColumn(lines[Row], preferredColumn);
        }

        public void Home(bool document = false)
        {
            if (document) Row = 0;
            Column = preferredColumn = 0;
        }

        public void End(bool document = false)
        {
            if (document) Row = lines.Count - 1;
            Column = lines[Row].Length;
            preferredColumn = ConsoleCells.Width(lines[Row]);
        }

        public bool NewLine()
        {
            if (Row == 0) { Row = 1; Column = preferredColumn = 0; return true; }
            if (BodyLength() == NotepadRules.MaxTextLength) return false;
            var tail = lines[Row][Column..];
            lines[Row] = lines[Row][..Column];
            lines.Insert(++Row, tail);
            Column = preferredColumn = 0;
            return true;
        }

        public void Backspace()
        {
            if (Column > 0)
            {
                var start = Previous(lines[Row], Column);
                lines[Row] = lines[Row].Remove(start, Column - start);
                Column = start;
            }
            else if (Row > 1)
            {
                var tail = lines[Row];
                lines.RemoveAt(Row--);
                Column = lines[Row].Length;
                lines[Row] += tail;
                Column = BoundaryAtOrBefore(lines[Row], Column);
            }
            preferredColumn = ConsoleCells.Width(lines[Row][..Column]);
        }

        public void Delete()
        {
            if (Column < lines[Row].Length)
                lines[Row] = lines[Row].Remove(Column, Next(lines[Row], Column) - Column);
            else if (Row > 0 && Row < lines.Count - 1)
            {
                lines[Row] += lines[Row + 1];
                lines.RemoveAt(Row + 1);
            }
            Column = BoundaryAtOrBefore(lines[Row], Column);
            preferredColumn = ConsoleCells.Width(lines[Row][..Column]);
        }

        public NotepadData ToNote() => new(lines[0], string.Join("\n", lines.Skip(1)));
        private int BodyLength() => lines.Skip(1).Sum(line => line.Length) + lines.Count - 2;
        private static int Previous(string text, int offset)
            => StringInfo.ParseCombiningCharacters(text).LastOrDefault(index => index < offset);
        private static int Next(string text, int offset)
            => StringInfo.ParseCombiningCharacters(text)
                .Where(index => index > offset).DefaultIfEmpty(text.Length).First();
        private static int BoundaryAtOrBefore(string text, int offset)
            => offset == text.Length ? offset : StringInfo.ParseCombiningCharacters(text)
                .LastOrDefault(index => index <= offset);
        private static int BoundaryAtOrAfter(string text, int offset)
            => StringInfo.ParseCombiningCharacters(text)
                .Where(index => index >= offset).DefaultIfEmpty(text.Length).First();
    }
}
