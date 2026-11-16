using InteractionFlow.Core.ResultAPI.Attributes;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// コードジェネレーターで生成される拡張メソッドのテンプレートです。
/// </summary>
[ResultAPITarget(typeof(Result), null)]
[ResultAPITarget(typeof(Result<>), "T")]
[ResultAPITarget(typeof(Result<,>), "T, TError")]
[ResultAPITarget(typeof(Result<>), "T", typeof(Result<>), "R", twoWay: false)]
[ResultAPITarget(typeof(Result<>), "T", typeof(Result<,>), "R, RError", twoWay: false)]
[ResultAPITarget(typeof(Result<,>), "T, TError", typeof(Result<,>), "R, RError", twoWay: false)]
[ResultAPITarget(typeof(Result<,>), "T, TError", typeof(Result<,>), "T, RError", twoWay: false)]
[ResultAPITarget(typeof(Result<,>), "T, TError", typeof(Result<,>), "R, TError", twoWay: false)]
[ResultAPITarget(typeof(Result), null, typeof(Result<>), "T", twoWay: true)]
[ResultAPITarget(typeof(Result), null, typeof(Result<,>), "T, TError", twoWay: true)]
[ResultAPITarget(typeof(Result<>), "T", typeof(Result<,>), "T, TError", twoWay: true)]
public class ResultTemplate : ResultTemplateBase
{
    /// <summary>
    /// 成功時はコールバックの結果を返し、失敗時は失敗を引き継ぎます。
    /// </summary>
    [ResultAPIFunc]
    protected static Other Then(Self self, OnSuccessFunc<Other> onSuccess)
    {
        if (self.IsSuccess)
        {
            return onSuccess.Invoke(self);
        }
        else
        {
            return Other.CreateFailure(self);
        }
    }

    /// <summary>
    /// 失敗時はコールバックの結果を返し、成功時は成功を引き継ぎます。
    /// </summary>
    [ResultAPIFunc]
    protected static Other ThenError(Self self, OnErrorFunc<Other> onError)
    {
        if (self.IsSuccess)
        {
            return Other.CreateSuccess(self);
        }
        else
        {
            return onError.Invoke(self);
        }
    }

    /// <summary>
    /// 結果に応じたコールバックの戻り値を返します。
    /// </summary>
    [ResultAPIFunc]
    protected static TResolved Resolve<TResolved>(Self self, OnSuccessFunc<TResolved> onSuccess, OnErrorFunc<TResolved> onError)
    {
        if (self.IsSuccess)
        {
            return onSuccess.Invoke(self);
        }
        else
        {
            return onError.Invoke(self);
        }
    }

    /// <summary>
    /// 成功時のみコールバックを実行し、元の結果を返します。
    /// </summary>
    [ResultAPIFunc]
    protected static Self IfSuccess(Self self, OnSuccessAction onSuccess)
    {
        if (self.IsSuccess)
        {
            onSuccess.Invoke(self);
        }

        return self;
    }

    /// <summary>
    /// 失敗時のみコールバックを実行し、元の結果を返します。
    /// </summary>
    [ResultAPIFunc]
    protected static Self IfError(Self self, OnErrorAction onError)
    {
        if (!self.IsSuccess)
        {
            onError.Invoke(self);
        }

        return self;
    }

    /// <summary>
    /// 結果に応じたコールバックを実行し、元の結果を返します。
    /// </summary>
    [ResultAPIFunc]
    protected static Self IfElse(Self self, OnSuccessAction onSuccess, OnErrorAction onError)
    {
        if (self.IsSuccess)
        {
            onSuccess.Invoke(self);
        }
        else
        {
            onError.Invoke(self);
        }

        return self;
    }
}
