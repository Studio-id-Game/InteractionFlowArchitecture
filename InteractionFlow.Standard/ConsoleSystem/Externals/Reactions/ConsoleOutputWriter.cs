using InteractionFlow.Standard.ConsoleSystem.Entities;
using System;
using System.IO;
using System.Runtime.ExceptionServices;

namespace InteractionFlow.Standard.ConsoleSystem.Externals.Reactions
{
    // 通常出力と、Core の専用基底を継承する例外・キャンセル通知で共有する出力手順。
    internal static class ConsoleOutputWriter
    {
        internal static void Write(string text, ConsoleTextStyle style)
        {
            lock (ConsoleSynchronization.OutputGate)
            {
                WriteCore(Console.Out, text,
                    appendNewLine: false, redirected: Console.IsOutputRedirected, style: style);
            }
        }

        internal static void WriteLine(string text, ConsoleTextStyle style)
        {
            lock (ConsoleSynchronization.OutputGate)
            {
                WriteCore(Console.Out, text,
                    appendNewLine: true, redirected: Console.IsOutputRedirected, style: style);
            }
        }

        internal static Exception? WriteErrorLine(string text)
        {
            lock (ConsoleSynchronization.OutputGate)
            {
                try
                {
                    Console.Error.WriteLine(text);
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            }
        }

        internal static Exception? Combine(Exception? first, Exception? second)
        {
            if (first is null) return second;
            if (second is null) return first;
            return new AggregateException(first, second);
        }

        private static void WriteCore(
            TextWriter writer, string text, bool appendNewLine, bool redirected, ConsoleTextStyle style)
        {
            Exception? failure = null;
            ConsoleColor? foreground = null;
            ConsoleColor? background = null;

            try
            {
                if (!redirected)
                {
                    if (style.Foreground.HasValue)
                    {
                        foreground = Console.ForegroundColor;
                        Console.ForegroundColor = style.Foreground.Value;
                    }
                    if (style.Background.HasValue)
                    {
                        background = Console.BackgroundColor;
                        Console.BackgroundColor = style.Background.Value;
                    }
                }

                if (appendNewLine) writer.WriteLine(text);
                else writer.Write(text);
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                // 一方の復元に失敗しても、他方の復元を必ず試みる。
                try
                {
                    if (background.HasValue) Console.BackgroundColor = background.Value;
                }
                catch (Exception e)
                {
                    failure = Combine(failure, e);
                }

                try
                {
                    if (foreground.HasValue) Console.ForegroundColor = foreground.Value;
                }
                catch (Exception e)
                {
                    failure = Combine(failure, e);
                }
            }

            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
