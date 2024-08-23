using System;

namespace RiskOfTwitch
{
    public sealed class Result<TResult>
    {
        public bool IsError { get; }

        public bool IsResult => !IsError;

        readonly TResult _resultValue;
        public TResult Value => IsResult ? _resultValue : throw new InvalidOperationException("Result is not success, no value available");

        readonly Exception _exception;
        public Exception Exception => IsError ? _exception : null;

        public Result(TResult value)
        {
            IsError = false;
            _resultValue = value;
        }

        public Result(Exception exception)
        {
            IsError = true;
            _exception = exception;
        }

        public void Match(Action<TResult> onResult, Action<Exception> onException)
        {
            if (onResult is null)
                throw new ArgumentNullException(nameof(onResult));

            if (onException is null)
                throw new ArgumentNullException(nameof(onException));

            if (IsError)
            {
                onException(_exception);
            }
            else
            {
                onResult(_resultValue);
            }
        }

        public static implicit operator Result<TResult>(TResult value)
        {
            return new Result<TResult>(value);
        }
    }
}
