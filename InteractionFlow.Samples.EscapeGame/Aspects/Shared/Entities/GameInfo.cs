using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

/// <summary>
/// ゲーム状態と、用途ごとに必要な付帯情報をまとめます。
/// </summary>
public sealed record GameInfo<T>(GameState State, T Info);
