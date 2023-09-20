using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter
{
    public static class Utilities
    {
        public static string DcrArrowTypeToString(DcrFlowArrowType type)
        {
            switch (type)
            {
                case DcrFlowArrowType.Condition:
                    return "condition";
                case DcrFlowArrowType.Response:
                    return "response";
                case DcrFlowArrowType.Include:
                    return "include";
                case DcrFlowArrowType.Exclude:
                    return "exclude";
                case DcrFlowArrowType.Milestone:
                    return "milestone";
                default:
                    throw new Exception("Unhandled enum type.");
            }
        }
    }
}
