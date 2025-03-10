using System;

namespace Arcor2.ClientSdk.Communication
{
    public class Arcor2ConnectionException : Exception
    {
        public Arcor2ConnectionException() : base() { }
        public Arcor2ConnectionException(string message) : base(message) { }
        public Arcor2ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
