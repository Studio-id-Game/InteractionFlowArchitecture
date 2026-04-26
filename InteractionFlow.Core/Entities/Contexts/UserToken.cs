using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public readonly struct UserToken : IEquatable<UserToken>

    {
        private readonly string name;

        public string Name => name ?? "Unknown User";

        public UserToken(string name)
        {
            this.name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is UserToken token && Equals(token);
        }

        public bool Equals(UserToken other)
        {
            return name == other.name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name);
        }

        public static bool operator ==(UserToken left, UserToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UserToken left, UserToken right)
        {
            return !(left == right);
        }
    }
}
