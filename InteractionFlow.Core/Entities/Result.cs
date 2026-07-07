using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    public readonly struct Result
    {
        private readonly ResultException? exception;

        private Result(Exception? exception)
        {
            if (exception is null)
            {
                this.exception = null;
            }
            else
            {
                if (exception is ResultException resultException)
                {
                    this.exception = resultException;
                }
                else
                {
                    this.exception = new ResultException(exception);
                }
            }
        }

        public readonly bool Try([MaybeNullWhen(true)] out ResultException e)
        {
            if (exception == null)
            {
                e = default;
                return true;
            }
            else
            {
                e = exception;
                return false;
            }
        }

        public readonly void ThrowIfError()
        {
            if (exception != null)
                throw exception;
        }

        public static Result Success { get; } = new(null);

        public static implicit operator Result(Exception exception) => new(exception);
    }

    public readonly struct Result<TValue>
    {
        private readonly TValue? value;
        private readonly Result result;

        private Result(TValue value)
        {
            this.value = value;
            result = Result.Success;
        }

        private Result(Exception exception)
        {
            value = default;
            result = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public readonly Result WithoutValue => result;

        public readonly bool Try([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out ResultException e)
        {
            if (result.Try(out e))
            {
                value = this.value!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public readonly void ThrowIfError()
        {
            result.ThrowIfError();
        }

        public static implicit operator Result<TValue>(TValue value) => new(value);

        public static implicit operator Result<TValue>(Exception exception) => new(exception);
    }
}
