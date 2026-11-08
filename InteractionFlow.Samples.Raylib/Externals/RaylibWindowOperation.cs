using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using Raylib_cs;
using RL = Raylib_cs.Raylib;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class RaylibWindowOperation : Operation, IWindowOperation
{
    public RaylibWindowOperation()
    {
    }

    public override void ForceResetMemoryState()
    {
    }

    public void Poll(CounterFlowContext context)
    {
        context.GetRequired<WindowState>().VerifyAccess();
        var close = RL.WindowShouldClose();
        var cancel = false;
        CounterCommand? command = null;

        if (!close && RL.IsMouseButtonPressed(MouseButton.Left))
        {
            var mouse = RL.GetMousePosition();

            if (RL.CheckCollisionPointRec(mouse, RaylibLayout.Increment))
                command = CounterCommand.Increment;
            else if (RL.CheckCollisionPointRec(mouse, RaylibLayout.Exit))
                command = CounterCommand.Exit;
            else if (RL.CheckCollisionPointRec(mouse, RaylibLayout.Cancel))
                cancel = true;
        }

        var frame = context.GetRequired<FrameState>();
        frame.Command = command;
        frame.CloseRequested = close;
        frame.CancelRequested = cancel;
    }
}
