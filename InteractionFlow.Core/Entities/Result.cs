using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    public readonly struct Result
    {
        public class InvalidException : Exception
        {
            public InvalidException() : base("Invalid Result Exception")
            {
            }

            public InvalidException(string message) : base(message)
            {
            }
        }

        private readonly Exception? exception;
        private readonly bool isValid;

        public Result(bool isValid)
        {
            this.isValid = isValid;
            exception = isValid ? null : new InvalidException();
        }

        public Result(Exception exception)
        {
            this.exception = exception;
            isValid = false;
        }

        public readonly bool IsValid => isValid;

        public readonly Exception? Exception
        {
            get
            {
                if (IsValid) return null;
                return exception ?? new InvalidException();
            }
        }

        public bool Try([MaybeNullWhen(true)] out Exception e)
        {
            if (IsValid)
            {
                e = default;
                return true;
            }
            else
            {
                e = exception!;
                return false;
            }
        }

        public void ThrowIfError()
        {
            if (!IsValid)
                throw Exception!;
        }

        public Result Or(Result right)
        {
            return IsValid ? this : right;
        }

        public Result And(Result right)
        {
            if (Try(out var e1))
            {
                if (right.Try(out var e2))
                {
                    return true;
                }
                else
                {
                    return e2;
                }
            }
            else
            {
                if (right.Try(out var e2))
                {
                    return e1;
                }
                else
                {
                    return new AggregateException(e1, e2);
                }
            }
        }

        public static implicit operator bool(Result result) => result.IsValid;

        public static implicit operator Result(bool isValid) => new(isValid);

        public static implicit operator Result(Exception exception) => new(exception);

        public static bool operator true(Result result) => result;

        public static bool operator false(Result result) => !result;

        public static Result operator |(Result left, Result right)
        {
            return left.Or(right);
        }

        public static Result operator &(Result left, Result right)
        {
            return left.And(right);
        }
    }

    public readonly struct Result<TValue>
    {
        private readonly TValue? value;
        private readonly Result result;

        public Result(TValue value)
        {
            this.value = value;
            result = true;
        }

        public Result(Exception exception)
        {
            value = default;
            result = exception;
        }

        public readonly TValue? Value => value;

        public readonly Exception? Exception => result.Exception;

        public readonly bool IsValid => result.IsValid;

        public bool Try([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out Exception e)
        {
            if (IsValid)
            {
                e = default;
                value = Value!;
                return true;
            }
            else
            {
                e = result.Exception!;
                value = default;
                return false;
            }
        }

        public void ThrowIfError()
        {
            if (!IsValid)
                throw Exception!;
        }

        public Result<TValue> Or(Result<TValue> right)
        {
            return IsValid ? this : right;
        }

        public static implicit operator bool(Result<TValue> result) => result.IsValid;

        public static implicit operator Result<TValue>(TValue value) => new(value);

        public static implicit operator Result<TValue>(Exception exception) => new(exception);

        public static bool operator true(Result<TValue> result) => result;

        public static bool operator false(Result<TValue> result) => !result;

        public static Result<TValue> operator |(Result<TValue> left, Result<TValue> right)
        {
            return left.Or(right);
        }
    }
}
