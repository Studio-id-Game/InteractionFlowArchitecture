using System;
using System.Globalization;
using System.Text;

namespace InteractionFlow.Samples.Notepad.Core.Entities
{
    public static class ConsoleCells
    {
        public static int Width(string text)
        {
            var total = 0;
            var elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext()) total += ElementWidth(elements.GetTextElement());
            return total;
        }

        public static string Slice(string text, int start, int width)
        {
            var result = new StringBuilder();
            var position = 0;
            var written = 0;
            var elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext() && written < width)
            {
                var element = elements.GetTextElement();
                var cells = ElementWidth(element);
                if (position >= start)
                {
                    if (written + cells > width) break;
                    result.Append(char.IsControl(element[0]) ? new string(' ', cells) : element);
                    written += cells;
                }
                else if (position + cells > start)
                {
                    var padding = Math.Min(width - written, position + cells - start);
                    result.Append(' ', padding);
                    written += padding;
                }
                position += cells;
            }
            return result.Append(' ', Math.Max(0, width - written)).ToString();
        }

        public static int OffsetAtColumn(string text, int column)
        {
            var width = 0;
            var offset = 0;
            var elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                width += ElementWidth(element);
                if (width > column) break;
                offset += element.Length;
            }
            return offset;
        }

        private static int ElementWidth(string element)
        {
            var scalar = char.IsSurrogatePair(element, 0) ? char.ConvertToUtf32(element, 0) : element[0];
            if (element[0] == '\t') return 4;
            if (element.IndexOf('\ufe0f') >= 0) return 2;
            if (scalar is >= 0x1100 and <= 0x115f or >= 0x2e80 and <= 0xa4cf
                or >= 0xac00 and <= 0xd7a3 or >= 0xf900 and <= 0xfaff
                or >= 0xfe10 and <= 0xfe6f or >= 0xff01 and <= 0xff60
                or >= 0xffe0 and <= 0xffe6 or >= 0x1f000 and <= 0x1faff or >= 0x20000 and <= 0x3ffff)
                return 2;
            return 1;
        }
    }
}
