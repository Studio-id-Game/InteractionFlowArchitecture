using System;
using System.Collections.Generic;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public abstract class Entry<TValue>(TValue? value) : IDisposable
    {
        public TValue? Value { get; protected set; } = value;

        public void Dispose()
        {
            if (Value != null && Value is IDisposable disposable)
            {
                disposable.Dispose();
                Value = default;
            }

            GC.SuppressFinalize(this);
        }

        public override string? ToString()
        {
            return Value?.ToString() ?? "(Null)";
        }

        public bool ValueEqualsTo(Entry<TValue> other)
        {
            if (Value == null && other.Value == null)
            {
                return true;
            }

            if (Value == null || other.Value == null)
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        public bool ValueEqualsTo(TValue? other)
        {
            if (Value == null && other == null)
            {
                return true;
            }

            if (Value == null || other == null)
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other);
        }
    }
}
