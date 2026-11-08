using InteractionFlow.Samples.Raylib.Entities.Contexts;
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
    private class ViewState
    {
        private int prevCount = 0;
        public float Dump { get; private set; } = 0;
        public Color CircleColor => Color.Lerp(Color.Red, Color.Gold, 1f - Dump);

        public void Update(int count, float elapsedSeconds)
        {
            if (count == prevCount)
            {
                // 60 FPS で毎フレーム 0.9 倍だった減衰を、経過秒数へ換算します。
                Dump *= MathF.Pow(0.9f, elapsedSeconds * 60f);
            }
            else
            {
                Dump = 1f;
            }

            prevCount = count;
        }
    }

    private ViewState viewState;

    public RaylibWindowReaction()
    {
        viewState = new();
    }

    public override void ForceResetMemoryState()
    {
        viewState = new();
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
        context.GetRequired<ICounterDisplayAccess>().Snapshot(
            out var count,
            out var message,
            out var acceptingInput);

        viewState.Update(count, RL.GetFrameTime());

        RL.BeginDrawing();

        try
        {
            RL.ClearBackground(new Color(24, 29, 42, 255));

            // 入力待ち・処理中のどちらでも描画が継続していることを示します。
            var x = (int)(280 * (Math.Sin(RL.GetTime() * 3) + 1));
            var y = (int)(20 * viewState.Dump * (Math.Sin(RL.GetTime() * 100) + 1));
            RL.DrawCircle(40 + x, 205 + y, 9 + count * 5, viewState.CircleColor);

            RL.DrawText("InteractionFlow / Raylib", 40, 30, 28, Color.RayWhite);
            RL.DrawText($"Count: {count}", 40, 90, 38, Color.SkyBlue);
            RL.DrawText(message, 40, 150, 20, Color.RayWhite);

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
