using Raylib_cs;

namespace InteractionFlow.Samples.Raylib.Externals;

// 入力判定と描画で共有する、raylib 実装内部の配置情報です。
internal static class RaylibLayout
{
    public static readonly Rectangle Increment = new(40, 240, 190, 56);

    public static readonly Rectangle Cancel = new(250, 240, 190, 56);

    public static readonly Rectangle Exit = new(460, 240, 190, 56);
}
