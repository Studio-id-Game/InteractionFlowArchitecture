using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using Raylib_cs;
using System;
using RL = Raylib_cs.Raylib;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class RaylibWindowReaction : Reaction, IWindowReaction
{
    public RaylibWindowReaction()
    {
    }

    public override void ForceResetMemoryState()
    {
    }

    public ReactionEnd Open(IFlowContext context)
    {
        var window = context.GetRequired<WindowState>();

        if (window.IsOpen)
            throw new InvalidOperationException("Window is already open.");

        RL.InitWindow(720, 420, "InteractionFlow - Raylib counter");

        if (!RL.IsWindowReady())
            throw new InvalidOperationException("Window initialization failed.");

        window.MarkOpened();
        RL.SetTargetFPS(60);

        return Draw(context);
    }

    public ReactionEnd Close(IFlowContext context, Result result)
    {
        try
        {
            var window = context.GetRequired<WindowState>();

            if (window.IsOpen)
            {
                window.VerifyAccess();
                RL.CloseWindow();
                window.MarkClosed();
            }
        }
        catch (Exception error)
        {
            result = Combine(result, error);
        }

        try
        {
            if (result.Try(out var failure))
                Console.WriteLine("Window closed.");
            else
                Console.Error.WriteLine(failure);
        }
        catch (Exception error)
        {
            result = Combine(result, error);
        }

        return GetEnd(result);
    }

    private static Result Combine(Result result, Exception error)
        => result.Try(out var previous) ? error : new AggregateException(previous, error);

    public ReactionEnd Draw(IFlowContext context)
    {
        context.GetRequired<WindowState>().VerifyAccess();
        context.GetRequired<CounterState>().Snapshot(
            out var count,
            out var message,
            out var acceptingInput);

        RL.BeginDrawing();

        try
        {
            RL.ClearBackground(new Color(24, 29, 42, 255));
            RL.DrawText("InteractionFlow / Raylib", 40, 30, 28, Color.RayWhite);
            RL.DrawText($"Count: {count}", 40, 90, 38, Color.SkyBlue);
            RL.DrawText(message, 40, 150, 20, Color.RayWhite);

            // 入力待ち・処理中のどちらでも描画が継続していることを示します。
            var x = 40 + (int)((Math.Sin(RL.GetTime() * 3) + 1) * 280);
            RL.DrawCircle(x, 205, 9, Color.Gold);

            DrawButton(RaylibLayout.Increment, "Run +1", acceptingInput);
            DrawButton(RaylibLayout.Cancel, "Cancel", true);
            DrawButton(RaylibLayout.Exit, "Exit", acceptingInput);

            RL.DrawText(
                "Run: 1 second reaction / repeated clicks are ignored while busy",
                40, 330, 16, Color.LightGray);

            RL.DrawText(
                "Cancel: interrupt and retry / Esc or window close: shut down",
                40, 360, 16, Color.LightGray);
        }
        finally
        {
            RL.EndDrawing();
        }

        return GetEnd();
    }

    private static void DrawButton(Rectangle bounds, string text, bool enabled)
    {
        var hovered = enabled && RL.CheckCollisionPointRec(RL.GetMousePosition(), bounds);
        RL.DrawRectangleRec(bounds, !enabled ? Color.DarkGray : hovered ? Color.Blue : Color.DarkBlue);
        RL.DrawText(text, (int)bounds.X + 30, (int)bounds.Y + 17, 22, enabled ? Color.White : Color.Gray);
    }
}
