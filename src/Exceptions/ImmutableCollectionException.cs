using System;

namespace Foundation.ObjectService.Exceptions
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class ImmutableCollectionException : Exception
    {
        private string _exceptionMessage = string.Empty;

        public string ExceptionMessage { get { return _exceptionMessage; } set { _exceptionMessage = value; } }

        public ImmutableCollectionException() : base() { }

        public ImmutableCollectionException(string exceptionMessage) : base(exceptionMessage)
        {
            _exceptionMessage = exceptionMessage;
        }

        public ImmutableCollectionException(string exceptionMessage, string message) : base(message)
        {
            _exceptionMessage = exceptionMessage;
        }

        public ImmutableCollectionException(string exceptionMessage, string message, Exception innerException) : base(message, innerException)
        {
            _exceptionMessage = exceptionMessage;
        }
    }
#pragma warning restore 1591
}