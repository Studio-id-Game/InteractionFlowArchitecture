namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

// 全体停止は、通常の両実行経路と共通Handlerから利用します。
public interface ILifetimeAccess
{
    /// <summary>
    /// アプリ全体の停止要求が記録され、通常の進行と新規セッションの確保を終了する状態かどうかを取得します。
    /// </summary>
    bool ShutdownRequested { get; }

    /// <summary>
    /// アプリ全体の停止を記録し、全セッションを停止して入力・再開・提示完了の待機を解除します。
    /// 保持中の制御要求も破棄し、繰り返し呼び出した場合は何もしません。
    /// </summary>
    void RequestShutdown();
}
