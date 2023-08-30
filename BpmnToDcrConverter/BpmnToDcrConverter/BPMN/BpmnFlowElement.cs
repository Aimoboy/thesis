using System;
using System.Collections.Generic;
using System.Text;

namespace BpmnToDcrConverter.BPMN
{
    public abstract class BpmnFlowElement
    {
        public int Id;
        public List<BpmnFlowArrow> OutgoingArrows = new List<BpmnFlowArrow>();
        public List<BpmnFlowArrow> IngoingArrows = new List<BpmnFlowArrow>();

        public abstract void TestArrowCountValidity();
    }

    public class BpmnActivity : BpmnFlowElement
    {
        public string Name;

        public BpmnActivity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int ingoingArrowCount = IngoingArrows.Count;

            if (outgoingArrowCount != 1)
            {
                throw new Exception($"Activities must have exactly 1 outgoing arrow, the activity with name \"{Name}\" and id \"{Id}\" has {outgoingArrowCount} outgoing arrows.");
            }

            if (ingoingArrowCount != 1)
            {
                throw new Exception($"Activities must have exactly 1 ingoing arrow, the activity with name \"{Name}\" and id \"{Id}\" has {ingoingArrowCount} ingoing arrows.");
            }
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

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int ingoingArrowCount = IngoingArrows.Count;

            switch (Type)
            {
                case BpmnEventType.Start:
                    if (outgoingArrowCount != 1)
                    {
                        throw new Exception($"The START event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 1.");
                    }

                    if (ingoingArrowCount != 0)
                    {
                        throw new Exception($"The START event with id \"{Id}\" has {ingoingArrowCount} ingoing arrows, but it has to have 1.");
                    }
                    break;
                case BpmnEventType.End:
                    if (outgoingArrowCount != 0)
                    {
                        throw new Exception($"The END event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 0.");
                    }

                    if (ingoingArrowCount == 0)
                    {
                        throw new Exception($"The END event with id \"{Id}\" has 0 ingoing arrows, but it has to have at least 1.");
                    }
                    break;
            }
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

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int ingoingArrowCount = IngoingArrows.Count;

            string gatewayString = Type switch
            {
                BpmnGatewayType.Or => "OR",
                BpmnGatewayType.And => "AND",
                _ => throw new Exception($"Missing case for enum {Type}.")
            };

            if (outgoingArrowCount == 0)
            {
                throw new Exception($"The {gatewayString} gateway with id \"{Id}\" has 0 outgoing arrows, but it has to have at least 1.");
            }

            if (ingoingArrowCount == 0)
            {
                throw new Exception($"The {gatewayString} gateway with id \"{Id}\" has 0 ingoing arrows, but it has to have at least 1.");
            }
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

    public class BpmnFlowArrow
    {
        public BpmnFlowArrowType Type;
        public BpmnFlowElement Element;

        public BpmnFlowArrow(BpmnFlowArrowType type, BpmnFlowElement element)
        {
            Type = type;
            Element = element;
        }
    }

    public enum BpmnFlowArrowType
    {
        Message,
        Sequence
    }
}
