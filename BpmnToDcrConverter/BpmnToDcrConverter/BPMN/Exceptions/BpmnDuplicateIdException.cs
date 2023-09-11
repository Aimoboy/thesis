using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.BPMN.Exceptions
{
    public class BpmnDuplicateIdException : Exception
    {
        public BpmnDuplicateIdException() { }

        public BpmnDuplicateIdException(string message) : base(message) { }

        public BpmnDuplicateIdException(string message, Exception inner) : base(message, inner) { }
    }
}
