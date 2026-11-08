using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities.Contexts;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface IWindowOperation : IOperationPort
{
    // フレーム入力は呼び出しスレッドで同期的に完了します。
    // FrameState の全入力を毎回上書きし、コマンドがなければ Command を null に戻します。
    void Poll(CounterFlowContext context);
}
