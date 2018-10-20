using System;
using System.IO;

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

    public interface ITypeTransition<TIn, TOut>
    {
        TOut DoTransit(TIn innerObject);
    }

    public interface ISupportTypeTransition<TIn>
    {
        TOut Transit<TOut>(ITypeTransition<TIn, TOut> transition);
    }

    public interface IDataWritingStrategy : ISupportTypeTransition<IDataWritingStrategy>
    {
        void Write(Stream outputStream);
    }

    public interface IDataRelayStrategy : ISupportTypeTransition<IDataRelayStrategy>
    {
        void Relay(Stream inputStream, Stream outputStream);
    }

    public static class Strategy
    {
        private class CopyStrategy : IDataRelayStrategy
        {
            public void Relay(Stream inputStream, Stream outputStream)
            {
                inputStream.CopyTo(outputStream);
            }

            public TOut Transit<TOut>(ITypeTransition<IDataRelayStrategy, TOut> transition)
            {
                return transition.DoTransit(this);
            }
        }

        public static IDataRelayStrategy Copy()
        {
            return new CopyStrategy();
        }
    }

    public static class Transition
    {
        private class FromFileTransition : ITypeTransition<IDataRelayStrategy, IDataWritingStrategy>
        {
            private class FromFileStrategy : IDataWritingStrategy
            {
                private IDataRelayStrategy _relayStrategy;
                private string _filePath;

                public FromFileStrategy(IDataRelayStrategy relayStrategy, string filePath)
                {
                    _relayStrategy = relayStrategy;
                    _filePath = filePath;
                }

                public void Write(Stream outputStream)
                {
                    using (var inputStream = File.Open(_filePath, FileMode.Open))
                    {
                        _relayStrategy.Relay(inputStream, outputStream);
                    }
                }

                public TOut Transit<TOut>(ITypeTransition<IDataWritingStrategy, TOut> transition)
                {
                    return transition.DoTransit(this);
                }
            }

            private string _filePath;

            public FromFileTransition(string filePath)
            {
                _filePath = filePath;
            }

            public IDataWritingStrategy DoTransit(IDataRelayStrategy source)
            {
                return new FromFileStrategy(source, _filePath);
            }
        }

        private class WriteFileTransition : ITypeTransition<IDataWritingStrategy, Result>
        {
            private string _filePath;

            public WriteFileTransition(string filePath)
            {
                _filePath = filePath;
            }

            public Result DoTransit(IDataWritingStrategy source)
            {
                try
                {
                    using (var outputStream = File.Open(_filePath, FileMode.Create))
                    {
                        source.Write(outputStream);
                    }
                    return new Result(true, null);
                }
                catch (Exception ex)
                {
                    return new Result(false, ex);
                }
            }
        }
        public static ITypeTransition<IDataRelayStrategy, IDataWritingStrategy> FromFile(string filePath)
        {
            return new FromFileTransition(filePath);
        }
        public static ITypeTransition<IDataWritingStrategy, Result> WriteFile(string filePath)
        {
            return new WriteFileTransition(filePath);
        }
    }
}