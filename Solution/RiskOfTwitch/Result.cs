using System;

namespace RiskOfTwitch
{
    public sealed class Result<T>
    {
        public bool IsSuccess { get; }

        public Exception Exception { get; }

        readonly T _value;

        public T Value
        {
            get
            {
                if (!IsSuccess)
                    throw new ArgumentException("Result is not success, cannot get value");

                return _value;
            }
        }

        Result(T value, Exception exception, bool isSuccess)
        {
            _value = value;
            Exception = exception;
            IsSuccess = isSuccess;
        }

        public Result(T value) : this(value, null, true)
        {
        }

        public Result(Exception exception) : this(default, exception, false)
        {
        }

        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value);
        }
    }
}
