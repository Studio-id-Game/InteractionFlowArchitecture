using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using RL = Raylib_cs.Raylib;

namespace InteractionFlow.Samples.EscapeGame.Externals;

internal sealed class RaylibWindowReaction : Reaction, IWindowReaction
{
    private static readonly Color background = new(18, 25, 38, 255);
    private static readonly Color muted = new(151, 168, 190, 255);
    private static readonly Color accent = new(104, 226, 191, 255);
    private static readonly string[] labels = ["Confirm  [Enter]", "Cancel  [Esc]", "Interact  [E]", "Wait  [Space]",
        "Pause  [P]", "Skip  [F]", "Retry  [R]", "Exit"];
    private PresentationKey? previousPresentation;
    private long previousControlRevision;
    private string? wrappedMessage;
    private string[] messageLines = [];

    public RaylibWindowReaction() { }

    public override void ForceResetMemoryState()
    {
        previousPresentation = null;
        previousControlRevision = 0;
        wrappedMessage = null;
        messageLines = [];
    }

    public ReactionEnd Open(WindowContext context)
    {
        var window = context.GetRequired<WindowState>();
        if (window.IsOpen) throw new InvalidOperationException("Window is already open.");
        RL.InitWindow(RaylibLayout.Width, RaylibLayout.Height, "InteractionFlow - Turn-based escape");
        if (!RL.IsWindowReady()) throw new InvalidOperationException("Window initialization failed.");
        window.MarkOpened();
        RL.SetExitKey(KeyboardKey.Null);
        RL.SetTargetFPS(60);
        return Draw(context);
    }

    public ReactionEnd Draw(WindowContext context)
    {
        context.GetRequired<WindowState>().VerifyAccess();
        var state = context.Display.Snapshot();
        var presentation = state.Presentation;
        if (presentation is not null)
        {
            var elapsed = previousPresentation == presentation.Key ? RL.GetFrameTime() : 0;
            presentation = context.Presentation.Advance(presentation.Key, elapsed, previousControlRevision);
        }
        previousPresentation = state.Presentation?.Key;
        previousControlRevision = state.ControlRevision;

        RL.BeginDrawing();
        try
        {
            RL.ClearBackground(background);
            DrawHeader(state);
            DrawBoard(state, presentation);
            DrawStatus(state);
            DrawButtons(state);
            RL.DrawText("Choose an action, then confirm.  Each action gives the guard one move.", 32, 686, 17, muted);
        }
        finally { RL.EndDrawing(); }

        if (presentation is not null
            && (presentation.SkipRequested || presentation.ElapsedSeconds >= presentation.DurationSeconds))
            context.Presentation.TryComplete(presentation.Key);
        return GetEnd();
    }

    public ReactionEnd Close(WindowContext context, Result previous)
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
        catch (Exception error) { previous = FailureCollector.Append(previous, error); }

        try
        {
            if (previous.Try(out var error)) Console.WriteLine("Escape game closed.");
            else Console.Error.WriteLine(error);
        }
        catch (Exception error) { previous = FailureCollector.Append(previous, error); }
        return GetEnd(previous);
    }

    private static void DrawHeader(GameSnapshot state)
    {
        RL.DrawText("ONE ROOM / ONE WAY OUT", 32, 26, 30, accent);
        RL.DrawText("Find the key. Activate the switch. Unlock the exit.", 32, 70, 19, muted);
        RL.DrawText($"TURN {state.Turn:00}", 784, 30, 26, Color.RayWhite);
    }

    private static void DrawBoard(GameSnapshot state, PresentationSnapshot? presentation)
    {
        for (var y = 0; y < state.Room.Height; y++)
        for (var x = 0; x < state.Room.Width; x++)
        {
            var bounds = Tile(x, y);
            var wall = state.Room.TileAt(x, y) == '#';
            RL.DrawRectangleRec(bounds, wall ? new Color(43, 57, 76, 255) : new Color(29, 40, 56, 255));
            if (wall) RL.DrawRectangle((int)bounds.X + 7, (int)bounds.Y + 7, (int)bounds.Width - 14, 4, new Color(57, 73, 94, 255));
        }
        DrawMark(state.Room.Exit, "EXIT", new Color(84, 184, 152, 255));
        if (!state.KeyCollected) DrawMark(state.Room.Key, "KEY", Color.Gold);
        DrawMark(state.Room.Switch, state.SwitchActivated ? "ON" : "SW", state.SwitchActivated ? accent : Color.Orange);
        DrawMark(state.Room.Door, state.DoorUnlocked ? "OPEN" : "LOCK", state.DoorUnlocked ? accent : new Color(191, 137, 86, 255));

        if (state.Selection is GameAction action)
        {
            var bounds = Tile(action.Target.X, action.Target.Y);
            RL.DrawRectangleLinesEx(bounds, 3, Color.Gold);
        }

        var player = Center(state.Player);
        var enemy = Center(state.Enemy);
        if (presentation is not null)
        {
            var t = presentation.DurationSeconds <= 0 || presentation.SkipRequested ? 1f
                : (float)Math.Clamp(presentation.ElapsedSeconds / presentation.DurationSeconds, 0, 1);
            var position = Vector2.Lerp(Center(presentation.From), Center(presentation.To), t * t * (3 - 2 * t));
            if (presentation.Kind == PresentationKind.PlayerMove) player = position;
            if (presentation.Kind == PresentationKind.EnemyMove) enemy = position;
        }
        RL.DrawCircleV(enemy, 22, new Color(239, 105, 116, 255));
        RL.DrawText("G", (int)enemy.X - 8, (int)enemy.Y - 12, 24, background);
        RL.DrawCircleV(player, 21, new Color(103, 187, 249, 255));
        RL.DrawText("P", (int)player.X - 8, (int)player.Y - 12, 24, background);

        if (state.Mode == ExecutionMode.Paused)
        {
            RL.DrawRectangle(RaylibLayout.BoardX, RaylibLayout.BoardY, 7 * RaylibLayout.Cell, 5 * RaylibLayout.Cell, new Color(12, 18, 29, 180));
            RL.DrawText("PAUSED", 205, 270, 38, Color.Gold);
            RL.DrawText("P to resume", 220, 321, 20, Color.RayWhite);
        }
    }

    private static void DrawMark(GridPosition position, string text, Color color)
    {
        var center = Center(position);
        RL.DrawRectangle((int)center.X - 29, (int)center.Y - 20, 58, 40, new Color(color.R, color.G, color.B, (byte)65));
        RL.DrawText(text, (int)center.X - RL.MeasureText(text, 17) / 2, (int)center.Y - 9, 17, color);
    }

    private void DrawStatus(GameSnapshot state)
    {
        RL.DrawRectangle(600, 120, 328, 380, new Color(25, 35, 50, 255));
        RL.DrawText("SESSION", 622, 140, 17, muted);
        var title = state.Stage == GameStage.Result ? state.Outcome == GameOutcome.Won ? "ESCAPED!" : "CAUGHT!"
            : state.Mode == ExecutionMode.Paused ? "Take a breath" : "Plan your next move";
        RL.DrawText(title, 622, 172, 24, state.Stage == GameStage.Result ? Color.Gold : Color.RayWhite);
        RL.DrawText($"Key       {(state.HasKey ? "carrying" : state.KeyCollected ? "used" : "not found")}", 622, 218, 18, muted);
        RL.DrawText($"Switch    {(state.SwitchActivated ? "active" : "inactive")}", 622, 250, 18, muted);
        RL.DrawText($"Door      {(state.DoorUnlocked ? "open" : "locked")}", 622, 282, 18, muted);
        RL.DrawText($"Selection: {state.Selection?.Kind.ToString() ?? "none"}", 622, 330, 18, Color.Gold);
        DrawWrapped(state.Message, 622, 372, 280);
        var next = state.Room.PatrolAt((state.EnemyPatrolIndex + 1) % state.Room.PatrolLength);
        var direction = next.X > state.Enemy.X ? "right" : next.X < state.Enemy.X ? "left"
            : next.Y > state.Enemy.Y ? "down" : "up";
        RL.DrawText($"Guard next: {direction}  [{state.EnemyPatrolIndex + 1}/4]", 622, 467, 17, muted);
        RL.DrawText("MOVE: arrows / WASD       CONFIRM: Enter", 32, 521, 19, muted);
    }

    private void DrawWrapped(string text, int x, int y, int width)
    {
        if (wrappedMessage != text)
        {
            var lines = new List<string>();
            var line = "";
            foreach (var word in text.Split(' '))
            {
                var next = line.Length == 0 ? word : line + " " + word;
                if (RL.MeasureText(next, 18) > width && line.Length != 0)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = next;
            }
            lines.Add(line);
            wrappedMessage = text;
            messageLines = lines.ToArray();
        }
        foreach (var line in messageLines)
        {
            RL.DrawText(line, x, y, 18, Color.RayWhite);
            y += 23;
        }
    }

    private static void DrawButtons(GameSnapshot state)
    {
        var active = state.Mode == ExecutionMode.Active;
        for (var i = 0; i < labels.Length; i++)
        {
            var enabled = i switch
            {
                0 => active && state.AcceptingInput && state.CanConfirm,
                1 or 2 or 3 => active && state.AcceptingInput && state.Stage == GameStage.Selecting,
                4 => state.Stage is GameStage.Selecting or GameStage.PlayerPresenting or GameStage.EnemyPresenting,
                5 => active && state.Presentation is not null
                    && state.Stage is GameStage.PlayerPresenting or GameStage.EnemyPresenting,
                6 => active && state.AcceptingInput && state.Stage == GameStage.Result,
                _ => true
            };
            var bounds = RaylibLayout.Button(i);
            var hover = enabled && RL.CheckCollisionPointRec(RL.GetMousePosition(), bounds);
            RL.DrawRectangleRec(bounds, !enabled ? new Color(30, 40, 54, 255)
                : hover ? new Color(58, 92, 119, 255) : new Color(39, 64, 86, 255));
            var label = i == 4 && state.Mode == ExecutionMode.Paused ? "Resume  [P]" : labels[i];
            RL.DrawText(label, (int)bounds.X + 18, (int)bounds.Y + 12, 19, enabled ? Color.RayWhite : new Color(96, 113, 135, 255));
        }
    }

    private static Rectangle Tile(int x, int y)
        => new(RaylibLayout.BoardX + x * RaylibLayout.Cell + 2, RaylibLayout.BoardY + y * RaylibLayout.Cell + 2,
            RaylibLayout.Cell - 4, RaylibLayout.Cell - 4);

    private static Vector2 Center(GridPosition position)
        => new(RaylibLayout.BoardX + (position.X + 0.5f) * RaylibLayout.Cell,
            RaylibLayout.BoardY + (position.Y + 0.5f) * RaylibLayout.Cell);
}
