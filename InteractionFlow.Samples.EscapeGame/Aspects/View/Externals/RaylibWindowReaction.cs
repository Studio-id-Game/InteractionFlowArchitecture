using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using RL = Raylib_cs.Raylib;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;

internal sealed class RaylibWindowReaction : Reaction, IWindowReaction
{
    private static readonly Color background = new(18, 25, 38, 255);
    private static readonly Color muted = new(151, 168, 190, 255);
    private static readonly Color accent = new(104, 226, 191, 255);
    private static readonly string[] labels = ["Confirm  [Enter]", "Cancel  [Esc]", "Interact  [E]", "Wait  [Space]",
        "Pause  [P]", "Skip  [F]", "Retry  [R]", "Exit"];
    private readonly PresentationClock clock = new();
    private static readonly GameInfo<GameViewInfo> empty = new(GameState.Empty, new());
    private GameInfo<GameViewInfo> game = empty;
    private GameState State => game.State;
    private GameViewInfo Info => game.Info;
    private string message = FormatMessage(empty);
    private string[] messageLines = [];

    internal string Message => message;
    internal bool AcceptingInput => Info.WaitingForInput && Info.Mode == ExecutionMode.Active
        && (State.Stage == GameStage.Selecting && !State.Confirmed && State.Outcome == GameOutcome.Running
            || State.Stage == GameStage.Result && State.ResultInputEnabled && !State.RetryRequested);
    internal bool CanConfirm => AcceptingInput && State.Selection.HasValue;
    private GridPosition Enemy => State.Room.PatrolAt(State.EnemyPatrolIndex);

    public override void ForceResetMemoryState()
    {
        clock.Reset();
        game = empty;
        message = FormatMessage(game);
        messageLines = [];
    }

    public ReactionEnd Open(IViewContext context)
    {
        var window = context.GetRequired<WindowState>();
        if (window.IsOpen) throw new InvalidOperationException("Window is already open.");
        RL.InitWindow(RaylibLayout.Width, RaylibLayout.Height, "InteractionFlow - Turn-based escape");
        if (!RL.IsWindowReady()) throw new InvalidOperationException("Window initialization failed.");
        window.MarkOpened();
        RL.SetExitKey(KeyboardKey.Null);
        RL.SetTargetFPS(60);
        return DrawFrame(context);
    }

    public ReactionEnd DrawFrame(IViewContext context)
    {
        context.GetRequired<WindowState>().VerifyAccess();
        PrepareFrame(context.View.ReadGameState(), RL.GetFrameTime());

        RL.BeginDrawing();
        try
        {
            RL.ClearBackground(background);
            DrawHeader();
            DrawBoard();
            DrawStatus();
            DrawButtons();
            RL.DrawText("Choose an action, then confirm.  Each action gives the guard one move.", 32, 686, 17, muted);
        }
        finally { RL.EndDrawing(); }

        if (Info.Event is { } presentation && clock.IsComplete)
            context.View.TryCompletePresentation(presentation.Key);
        return GetEnd();
    }

    // このReactionだけが使う表示の記憶。同時点のゲーム状態とセッション情報を不変の組として受け取ります。
    internal void PrepareFrame(GameInfo<GameViewInfo> next, double deltaSeconds)
    {
        if (!ReferenceEquals(game, next))
        {
            var nextMessage = FormatMessage(next);
            if (message != nextMessage)
            {
                message = nextMessage;
                messageLines = [];
            }
            game = next;
        }
        clock.Update(game, deltaSeconds);
    }

    // 操作結果を優先し、それ以外は同じ不変状態のイベント・選択から導出します。
    private static string FormatMessage(GameInfo<GameViewInfo> game)
    {
        var feedback = game.Info.Feedback switch
        {
            InputFeedback.SelectionCanceled => "Selection canceled",
            InputFeedback.InvalidSelection => "That action is not valid",
            InputFeedback.NoUsableMechanism => "No usable mechanism (switch and key are required for door)",
            InputFeedback.StageDoesNotAcceptSelection => "This stage does not accept selection",
            _ => null
        };
        if (feedback is not null) return feedback;

        var state = game.State;
        if (state.RetryRequested) return "Retrying";
        if (state.Confirmed) return "Action confirmed";

        return game.Info.Event?.Kind switch
        {
            GameEventKind.Initial => "Find the key, activate the switch, unlock the door",
            GameEventKind.PlayerMove => "Player: Move",
            GameEventKind.Switch => "Player: Switch",
            GameEventKind.Unlock => "Player: Unlock",
            GameEventKind.Wait => "Player: Wait",
            GameEventKind.EnemyMove => "Guard moves",
            GameEventKind.Result => state.Outcome == GameOutcome.Won ? "Escaped! R to retry" : "Caught! R to retry",
            _ => state.Selection is { } action ? "Selected " + action.Kind + "; Enter to confirm"
                : state.Stage is GameStage.Selecting or GameStage.Stopping ? "Choose an action, then confirm"
                : "Preparing room"
        };
    }

    public ReactionEnd Close(ILifetimeContext context, Result previous)
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

    private void DrawHeader()
    {
        RL.DrawText("ONE ROOM / ONE WAY OUT", 32, 26, 30, accent);
        RL.DrawText("Find the key. Activate the switch. Unlock the exit.", 32, 70, 19, muted);
        RL.DrawText($"TURN {State.Turn:00}", 784, 30, 26, Color.RayWhite);
    }

    private void DrawBoard()
    {
        for (var y = 0; y < State.Room.Height; y++)
        for (var x = 0; x < State.Room.Width; x++)
        {
            var bounds = GetTileBounds(x, y);
            var wall = State.Room.TileAt(x, y) == '#';
            RL.DrawRectangleRec(bounds, wall ? new Color(43, 57, 76, 255) : new Color(29, 40, 56, 255));
            if (wall) RL.DrawRectangle((int)bounds.X + 7, (int)bounds.Y + 7, (int)bounds.Width - 14, 4, new Color(57, 73, 94, 255));
        }
        DrawMark(State.Room.Exit, "EXIT", new Color(84, 184, 152, 255));
        if (!State.KeyCollected) DrawMark(State.Room.Key, "KEY", Color.Gold);
        DrawMark(State.Room.Switch, State.SwitchActivated ? "ON" : "SW", State.SwitchActivated ? accent : Color.Orange);
        DrawMark(State.Room.Door, State.DoorUnlocked ? "OPEN" : "LOCK", State.DoorUnlocked ? accent : new Color(191, 137, 86, 255));

        if (State.Selection is GameAction action)
        {
            var bounds = GetTileBounds(action.Target.X, action.Target.Y);
            RL.DrawRectangleLinesEx(bounds, 3, Color.Gold);
        }

        var player = GetTileCenter(State.Player);
        var enemy = GetTileCenter(Enemy);
        if (Info.Event is { } presentation)
        {
            var t = clock.DurationSeconds <= 0 || Info.SkipRequested ? 1f
                : (float)Math.Clamp(clock.ElapsedSeconds / clock.DurationSeconds, 0, 1);
            var position = Vector2.Lerp(
                GetTileCenter(presentation.From), GetTileCenter(presentation.To), t * t * (3 - 2 * t));
            if (presentation.Kind == GameEventKind.PlayerMove) player = position;
            if (presentation.Kind == GameEventKind.EnemyMove) enemy = position;
        }
        RL.DrawCircleV(enemy, 22, new Color(239, 105, 116, 255));
        RL.DrawText("G", (int)enemy.X - 8, (int)enemy.Y - 12, 24, background);
        RL.DrawCircleV(player, 21, new Color(103, 187, 249, 255));
        RL.DrawText("P", (int)player.X - 8, (int)player.Y - 12, 24, background);

        if (Info.Mode == ExecutionMode.Paused)
        {
            RL.DrawRectangle(RaylibLayout.BoardX, RaylibLayout.BoardY, 7 * RaylibLayout.Cell, 5 * RaylibLayout.Cell, new Color(12, 18, 29, 180));
            RL.DrawText("PAUSED", 205, 270, 38, Color.Gold);
            RL.DrawText("P to resume", 220, 321, 20, Color.RayWhite);
        }
    }

    private static void DrawMark(GridPosition position, string text, Color color)
    {
        var center = GetTileCenter(position);
        RL.DrawRectangle((int)center.X - 29, (int)center.Y - 20, 58, 40, new Color(color.R, color.G, color.B, (byte)65));
        RL.DrawText(text, (int)center.X - RL.MeasureText(text, 17) / 2, (int)center.Y - 9, 17, color);
    }

    private void DrawStatus()
    {
        RL.DrawRectangle(600, 120, 328, 380, new Color(25, 35, 50, 255));
        RL.DrawText("SESSION", 622, 140, 17, muted);
        var title = State.Stage == GameStage.Result ? State.Outcome == GameOutcome.Won ? "ESCAPED!" : "CAUGHT!"
            : Info.Mode == ExecutionMode.Paused ? "Take a breath" : "Plan your next move";
        RL.DrawText(title, 622, 172, 24, State.Stage == GameStage.Result ? Color.Gold : Color.RayWhite);
        RL.DrawText($"Key       {(State.HasKey ? "carrying" : State.KeyCollected ? "used" : "not found")}", 622, 218, 18, muted);
        RL.DrawText($"Switch    {(State.SwitchActivated ? "active" : "inactive")}", 622, 250, 18, muted);
        RL.DrawText($"Door      {(State.DoorUnlocked ? "open" : "locked")}", 622, 282, 18, muted);
        RL.DrawText($"Selection: {State.Selection?.Kind.ToString() ?? "none"}", 622, 330, 18, Color.Gold);
        DrawMessage();
        var next = State.Room.PatrolAt((State.EnemyPatrolIndex + 1) % State.Room.PatrolLength);
        var direction = next.X > Enemy.X ? "right" : next.X < Enemy.X ? "left"
            : next.Y > Enemy.Y ? "down" : "up";
        RL.DrawText($"Guard next: {direction}  [{State.EnemyPatrolIndex + 1}/4]", 622, 467, 17, muted);
        RL.DrawText("MOVE: arrows / WASD       CONFIRM: Enter", 32, 521, 19, muted);
    }

    private void DrawMessage()
    {
        if (messageLines.Length == 0)
        {
            var lines = new List<string>();
            var line = "";
            foreach (var word in message.Split(' '))
            {
                var next = line.Length == 0 ? word : line + " " + word;
                if (RL.MeasureText(next, 18) > 280 && line.Length != 0)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = next;
            }
            lines.Add(line);
            messageLines = lines.ToArray();
        }
        var y = 372;
        foreach (var line in messageLines)
        {
            RL.DrawText(line, 622, y, 18, Color.RayWhite);
            y += 23;
        }
    }

    private void DrawButtons()
    {
        var active = Info.Mode == ExecutionMode.Active;
        for (var i = 0; i < labels.Length; i++)
        {
            var enabled = i switch
            {
                0 => CanConfirm,
                1 or 2 or 3 => AcceptingInput && State.Stage == GameStage.Selecting,
                4 => State.Stage is GameStage.Selecting or GameStage.PlayerPresenting or GameStage.EnemyPresenting,
                5 => active && Info.Event is not null
                    && State.Stage is GameStage.PlayerPresenting or GameStage.EnemyPresenting,
                6 => AcceptingInput && State.Stage == GameStage.Result,
                _ => true
            };
            var bounds = RaylibLayout.GetButtonBounds(i);
            var hover = enabled && RL.CheckCollisionPointRec(RL.GetMousePosition(), bounds);
            RL.DrawRectangleRec(bounds, !enabled ? new Color(30, 40, 54, 255)
                : hover ? new Color(58, 92, 119, 255) : new Color(39, 64, 86, 255));
            var label = i == 4 && Info.Mode == ExecutionMode.Paused ? "Resume  [P]" : labels[i];
            RL.DrawText(label, (int)bounds.X + 18, (int)bounds.Y + 12, 19, enabled ? Color.RayWhite : new Color(96, 113, 135, 255));
        }
    }

    private static Rectangle GetTileBounds(int x, int y)
        => new(RaylibLayout.BoardX + x * RaylibLayout.Cell + 2, RaylibLayout.BoardY + y * RaylibLayout.Cell + 2,
            RaylibLayout.Cell - 4, RaylibLayout.Cell - 4);

    private static Vector2 GetTileCenter(GridPosition position)
        => new(RaylibLayout.BoardX + (position.X + 0.5f) * RaylibLayout.Cell,
            RaylibLayout.BoardY + (position.Y + 0.5f) * RaylibLayout.Cell);
}
