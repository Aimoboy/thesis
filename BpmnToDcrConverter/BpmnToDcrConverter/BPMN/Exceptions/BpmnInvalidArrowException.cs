using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.BPMN.Exceptions
{
    public class BpmnInvalidArrowException : Exception
    {
        public BpmnInvalidArrowException() { }

        public BpmnInvalidArrowException(string message) : base(message) { }

        public BpmnInvalidArrowException(string message, Exception inner) : base(message, inner) { }
    }
}
