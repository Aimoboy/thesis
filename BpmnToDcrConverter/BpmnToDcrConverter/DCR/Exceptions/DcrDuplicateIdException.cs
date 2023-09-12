using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.Dcr.Exceptions
{
    public class DcrDuplicateIdException : Exception
    {
        public DcrDuplicateIdException() { }

        public DcrDuplicateIdException(string message) : base(message) { }

        public DcrDuplicateIdException(string message, Exception inner) : base(message, inner) { }
    }
}
