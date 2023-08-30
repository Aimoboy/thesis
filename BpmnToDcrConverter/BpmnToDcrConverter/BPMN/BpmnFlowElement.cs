using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.BPMN
{
    public abstract class BpmnFlowElement
    {
        public int Id;
    }

    public class BpmnActivity : BpmnFlowElement
    {
        public string Name;

        public BpmnActivity(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class BpmnEvent : BpmnFlowElement
    {
        public BpmnEventType Type;

        public BpmnEvent(int id, BpmnEventType type)
        {
            Id = id;
            Type = type;
        }
    }

    public class BpmnGateway : BpmnFlowElement
    {
        public BpmnGatewayType Type;

        public BpmnGateway(int id, BpmnGatewayType type)
        {
            Id = id;
            Type = type;
        }
    }

    public enum BpmnEventType
    {
        Start,
        End
    }

    public enum BpmnGatewayType
    {
        Or,
        And
    }
}
