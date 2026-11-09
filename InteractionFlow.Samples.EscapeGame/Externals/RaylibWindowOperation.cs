using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using Raylib_cs;
using RL = Raylib_cs.Raylib;

namespace InteractionFlow.Samples.EscapeGame.Externals;

internal sealed class RaylibWindowOperation : Operation, IWindowOperation
{
    public RaylibWindowOperation() { }

    public override void ForceResetMemoryState() { }

    public void Poll(WindowContext context)
    {
        context.GetRequired<WindowState>().VerifyAccess();
        var snapshot = context.Display.Snapshot();
        var frame = context.GetRequired<FrameState>();
        frame.SessionId = snapshot.SessionId;
        frame.Presentation = snapshot.Presentation?.Key;
        frame.Command = null;
        frame.PauseRequested = false;
        frame.SkipRequested = false;
        frame.ShutdownRequested = RL.WindowShouldClose();

        var button = ClickedButton();
        if (frame.ShutdownRequested || button == 7)
        {
            frame.ShutdownRequested = true;
            return;
        }
        if (RL.IsKeyPressed(KeyboardKey.P) || button == 4)
        {
            frame.PauseRequested = true;
            return;
        }
        if (snapshot.Mode != ExecutionMode.Active) return;

        if (RL.IsKeyPressed(KeyboardKey.F) || button == 5)
            frame.SkipRequested = true;
        else if (RL.IsKeyPressed(KeyboardKey.Escape) || button == 1)
            frame.Command = GameCommand.CancelSelection;
        else if (RL.IsKeyPressed(KeyboardKey.R) || button == 6)
            frame.Command = GameCommand.Retry;
        else if (RL.IsKeyPressed(KeyboardKey.E) || button == 2)
            frame.Command = GameCommand.Interact;
        else if (RL.IsKeyPressed(KeyboardKey.Space) || button == 3)
            frame.Command = GameCommand.Wait;
        else if (Pressed(KeyboardKey.Up, KeyboardKey.W)) frame.Command = GameCommand.Up;
        else if (Pressed(KeyboardKey.Right, KeyboardKey.D)) frame.Command = GameCommand.Right;
        else if (Pressed(KeyboardKey.Down, KeyboardKey.S)) frame.Command = GameCommand.Down;
        else if (Pressed(KeyboardKey.Left, KeyboardKey.A)) frame.Command = GameCommand.Left;
        else if (RL.IsKeyPressed(KeyboardKey.Enter) || button == 0) frame.Command = GameCommand.Confirm;
    }

    private static bool Pressed(KeyboardKey first, KeyboardKey second)
        => RL.IsKeyPressed(first) || RL.IsKeyPressed(second);

    private static int ClickedButton()
    {
        if (!RL.IsMouseButtonPressed(MouseButton.Left)) return -1;
        var mouse = RL.GetMousePosition();
        for (var index = 0; index < 8; index++)
            if (RL.CheckCollisionPointRec(mouse, RaylibLayout.Button(index))) return index;
        return -1;
    }
}
