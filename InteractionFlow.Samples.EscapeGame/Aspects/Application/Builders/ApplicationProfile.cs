using InteractionFlow.Core.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Builders;

public sealed class ApplicationProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        // 共有AccessはEscapeApplicationContextが生成します。現在はサービス登録を持ちません。
    }
}
