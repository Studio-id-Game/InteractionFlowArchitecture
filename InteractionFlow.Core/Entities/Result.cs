using System;

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
            exception = null;
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

        public readonly Result<T> AsResultValue<T>(Func<T> value) => IsValid ? new Result<T>(value()) : new Result<T>(exception!);

        public readonly Result<T> AsResultValue<T>(T value) => IsValid ? new Result<T>(value) : new Result<T>(exception!);

        public readonly Result<T> AsResultValue<T>(in T value) => IsValid ? new Result<T>(value) : new Result<T>(exception!);

        public static Result Success { get; } = new(true);

        public static Result Error(Exception exception) => new(exception);

        public static Result<TValue> Error<TValue>(Exception exception) => new(exception);


        public static implicit operator bool(Result result) => result.IsValid;

        public static implicit operator Exception?(Result result) => result.Exception;

        public static implicit operator Result(bool isValid) => new(isValid);

        public static implicit operator Result(Exception exception) => new(exception);

        public static bool operator true(Result result) => result;

        public static bool operator false(Result result) => !result;

        public static Result operator |(Result left, Result right) => left ? left : right;

        public static Result operator &(Result left, Result right) => left ? right : left;
    }

    public readonly struct Result<TEntity>(TEntity? value, Result result)
    {
        private readonly TEntity? value = value;

        public Result(TEntity value) : this(value, true)
        {
        }

        public Result(Exception exception) : this(default, exception)
        {
        }

        public readonly TEntity? Value
        {
            get
            {
                if (IsValid) return value;
                throw Exception!;
            }
        }

        public Result<TEntity2> With<TEntity2>(TEntity2? newValue) => new(newValue, result);

        public readonly Exception? Exception => result.Exception;

        public readonly Result AsResult => result;

        public readonly bool IsValid => result.IsValid;

        public static implicit operator bool(Result<TEntity> result) => result.IsValid;

        public static implicit operator Exception?(Result<TEntity> result) => result.Exception;

        public static implicit operator Result<TEntity>(TEntity value) => new(value);

        public static implicit operator Result<TEntity>(Exception exception) => new(exception);

        public static bool operator true(Result<TEntity> result) => result;

        public static bool operator false(Result<TEntity> result) => !result;

        public static Result<TEntity> operator |(Result<TEntity> left, Result<TEntity> right) => left ? left : right;

        public static Result<TEntity> operator &(Result<TEntity> left, Result<TEntity> right) => left ? right : left;
    }
}
