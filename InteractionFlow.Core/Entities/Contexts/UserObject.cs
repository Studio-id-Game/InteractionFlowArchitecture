using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローを実行しているユーザーを識別する値オブジェクトです。
    /// </summary>
    /// <param name="id">ユーザーを表す ID。<see langword="null"/> の場合は表示上 Unknown User として扱われます。</param>
    public class UserObject(string? id) : IEquatable<UserObject>
    {
        private readonly string? id = id;

        /// <summary>
        /// ユーザー ID を取得します。未指定の場合は Unknown User を返します。
        /// </summary>
        public string Id => id ?? "Unknown User";

        /// <summary>
        /// 指定したオブジェクトが同じユーザー ID を持つかどうかを判定します。
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト。</param>
        /// <returns>同じユーザー ID を持つ場合は <see langword="true"/>。</returns>
        public override bool Equals(object? obj)
        {
            return obj is UserObject token && Equals(token);
        }

        /// <summary>
        /// 指定したユーザーオブジェクトが同じ内部 ID を持つかどうかを判定します。
        /// </summary>
        /// <param name="other">比較対象のユーザーオブジェクト。</param>
        /// <returns>同じ内部 ID を持つ場合は <see langword="true"/>。</returns>
        public bool Equals(UserObject other)
        {
            return id == other.id;
        }

        /// <summary>
        /// 内部 ID からハッシュコードを取得します。
        /// </summary>
        /// <returns>内部 ID に基づくハッシュコード。</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(id);
        }

        /// <summary>
        /// 2 つのユーザーオブジェクトが同じ内部 ID を持つかどうかを判定します。
        /// </summary>
        /// <param name="left">左辺のユーザーオブジェクト。</param>
        /// <param name="right">右辺のユーザーオブジェクト。</param>
        /// <returns>同じ内部 ID を持つ場合は <see langword="true"/>。</returns>
        public static bool operator ==(UserObject left, UserObject right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 2 つのユーザーオブジェクトが異なる内部 ID を持つかどうかを判定します。
        /// </summary>
        /// <param name="left">左辺のユーザーオブジェクト。</param>
        /// <param name="right">右辺のユーザーオブジェクト。</param>
        /// <returns>異なる内部 ID を持つ場合は <see langword="true"/>。</returns>
        public static bool operator !=(UserObject left, UserObject right)
        {
            return !(left == right);
        }
    }
}
