using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class UserObject(string? id) : IEquatable<UserObject>
    {
        private readonly string? id = id;

        public string Id => id ?? "Unknown User";

        public override bool Equals(object? obj)
        {
            return obj is UserObject token && Equals(token);
        }

        public bool Equals(UserObject other)
        {
            return id == other.id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id);
        }

        public static bool operator ==(UserObject left, UserObject right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UserObject left, UserObject right)
        {
            return !(left == right);
        }
    }
}
