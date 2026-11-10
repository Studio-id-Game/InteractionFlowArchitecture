namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

// 全体停止は、通常の両実行経路と共通Handlerから利用します。
public interface ILifetimeAccess
{
    bool ShutdownRequested { get; }
    void RequestShutdown();
}
