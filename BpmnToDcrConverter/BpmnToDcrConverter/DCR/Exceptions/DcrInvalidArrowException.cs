using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.Dcr.Exceptions
{
    public class DcrInvalidArrowException : Exception
    {
        public DcrInvalidArrowException() { }

        public DcrInvalidArrowException(string message) : base(message) { }

        public DcrInvalidArrowException(string message, Exception inner) : base(message, inner) { }
    }
}
