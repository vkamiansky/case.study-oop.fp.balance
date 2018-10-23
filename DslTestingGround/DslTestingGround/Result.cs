using System;

namespace DslTestingGround
{
    public class Result
    {
        public bool IsSuccess { get; private set; }
        public Exception Exception { get; private set; }

        public Result(bool isSuccess, Exception exception)
        {
            IsSuccess = isSuccess;
            Exception = exception;
        }
    }
}