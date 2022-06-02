using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Core.Exceptions
{
    internal class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException()
        {
        }

        public CircuitBreakerOpenException(string? message) : base(message)
        {
        }

        public CircuitBreakerOpenException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected CircuitBreakerOpenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
