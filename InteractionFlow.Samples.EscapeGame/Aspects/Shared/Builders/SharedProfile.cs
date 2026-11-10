using InteractionFlow.Core.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Builders;

public sealed class SharedProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        // 共有データと契約の構成境界を表します。現在はサービス登録を持ちません。
    }
}
