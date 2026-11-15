using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、Self/Other の置き換えパターンを指定します。
/// <see cref="ResultAPITemplateAttribute"/> でマークされたテンプレートクラスと、
/// <see cref="ResultAPIExtensionAttribute"/> でマークされた拡張クラスでのみ有効です。 
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class ResultAPITargetAttribute : Attribute
{
    /// <summary>
    /// Self to Other のパターンを候補に追加します。
    /// </summary>
    /// <param name="self"> Self を置き換える <see cref="ResultAPIResultAttribute"/> でマークされた型を指定します。 </param>
    /// <param name="selfArg"> <paramref name="self"/> の型引数定義名を "," 区切りで指定します。 </param>
    /// <param name="other"> Other を置き換える <see cref="ResultAPIResultAttribute"/> でマークされた型を指定します。</param>
    /// <param name="otherArg"> <paramref name="other"/> の型引数定義名を "," 区切りで指定します。 <paramref name="selfArg"/> と同じ名前の型引数はリンクします。 </param>
    /// <param name="twoWay"> Other と Self を入れ変えたパターンも候補に加えます </param>
    public ResultAPITargetAttribute(Type self, string? selfArg, Type other, string? otherArg, bool twoWay)
    {
        Self = self;
        SelfArg = selfArg;
        Other = other;
        OtherArg = otherArg;
        TwoWay = twoWay;
    }

    /// <summary>
    /// Self to Self のパターンを候補に追加します。
    /// </summary>
    /// <param name="self"> Self を置き換える <see cref="ResultAPIResultAttribute"/> でマークされた型を指定します。 </param>
    /// <param name="selfArg"> <paramref name="self"/> の型引数定義名を "," 区切りで指定します。 </param>
    public ResultAPITargetAttribute(Type self, string? selfArg)
    {
        Self = self;
        SelfArg = selfArg;
        Other = self;
        OtherArg = selfArg;
        TwoWay = false;
    }

    /// <summary>
    /// Self を置き換える <see cref="ResultAPIResultAttribute"/> でマークされた型を指定します。
    /// </summary>
    public Type Self { get; }
    /// <summary>
    /// <see cref="Self"/> の型引数定義名を "," 区切りで指定します。
    /// </summary>
    public string? SelfArg { get; }
    /// <summary>
    /// Other を置き換える <see cref="ResultAPIResultAttribute"/> でマークされた型を指定します。
    /// </summary>
    public Type Other { get; }
    /// <summary>
    /// <see cref="Other"/> の型引数定義名を "," 区切りで指定します。 <see cref="SelfArg"/> と同じ名前の型引数はリンクします。
    /// </summary>
    public string? OtherArg { get; }
    /// <summary>
    /// Other と Self を入れ変えたパターンも候補に加えます
    /// </summary>
    public bool TwoWay { get; }
}
