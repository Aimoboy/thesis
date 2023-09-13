﻿using BpmnToDcrConverter.Bpmn.Exceptions;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter.Bpmn
{
    public abstract class BpmnFlowElement
    {
        public string Id;
        public List<BpmnFlowArrow> OutgoingArrows = new List<BpmnFlowArrow>();
        public List<BpmnFlowArrow> IncomingArrows = new List<BpmnFlowArrow>();

        public abstract void TestArrowCountValidity();

        public List<string> GetIds()
        {
            return GetFlowElementsFlat().Select(x => x.Id).ToList();
        }

        public virtual List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return new List<BpmnFlowElement> { this };
        }

        public abstract Tuple<List<DcrFlowElement>, DcrFlowElement> Convert();
    }

    public class BpmnActivity : BpmnFlowElement
    {
        public string Name;

        public BpmnActivity(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount != 1)
            {
                throw new BpmnInvalidArrowException($"Activities must have exactly 1 outgoing arrow, the activity with name \"{Name}\" and id \"{Id}\" has {outgoingArrowCount} outgoing arrows.");
            }

            if (incomingArrowCount != 1)
            {
                throw new BpmnInvalidArrowException($"Activities must have exactly 1 incoming arrow, the activity with name \"{Name}\" and id \"{Id}\" has {incomingArrowCount} incoming arrows.");
            }
        }

        public override Tuple<List<DcrFlowElement>, DcrFlowElement> Convert()
        {
            BpmnFlowElement nextElement = OutgoingArrows.FirstOrDefault().Element;
            (List<DcrFlowElement> collection, DcrFlowElement nextElementConverted) = nextElement.Convert();

            DcrActivity activity = new DcrActivity(Id, Name, true, false, true);
            activity.OutgoingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Exclude, activity));

            if (nextElementConverted != null)
            {
                activity.OutgoingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Condition, nextElementConverted));
                nextElementConverted.IncomingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Condition, activity));
            }

            List<DcrFlowElement> newCollection = collection.Concat(new[] { activity }).ToList();
            return new Tuple<List<DcrFlowElement>, DcrFlowElement>(newCollection, activity);
        }
    }

    public class BpmnEvent : BpmnFlowElement
    {
        public BpmnEventType Type;

        public BpmnEvent(string id, BpmnEventType type)
        {
            Id = id;
            Type = type;
        }

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            switch (Type)
            {
                case BpmnEventType.Start:
                    if (outgoingArrowCount != 1)
                    {
                        throw new BpmnInvalidArrowException($"The START event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 1.");
                    }

                    if (incomingArrowCount != 0)
                    {
                        throw new BpmnInvalidArrowException($"The START event with id \"{Id}\" has {incomingArrowCount} incoming arrows, but it has to have 1.");
                    }
                    break;
                case BpmnEventType.End:
                    if (outgoingArrowCount != 0)
                    {
                        throw new BpmnInvalidArrowException($"The END event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 0.");
                    }

                    if (incomingArrowCount == 0)
                    {
                        throw new BpmnInvalidArrowException($"The END event with id \"{Id}\" has 0 incoming arrows, but it has to have at least 1.");
                    }
                    break;
            }
        }

        public override Tuple<List<DcrFlowElement>, DcrFlowElement> Convert()
        {
            switch (Type)
            {
                case BpmnEventType.End:
                    return new Tuple<List<DcrFlowElement>, DcrFlowElement>(new List<DcrFlowElement>(), null);
                case BpmnEventType.Start:
                    return OutgoingArrows.FirstOrDefault().Element.Convert();
                default:
                    throw new Exception("Unhandled enum case.");
            }
        }
    }

    public class BpmnGateway : BpmnFlowElement
    {
        public BpmnGatewayType Type;

        public BpmnGateway(string id, BpmnGatewayType type)
        {
            Id = id;
            Type = type;
        }

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            string gatewayString = Type switch
            {
                BpmnGatewayType.Or => "OR",
                BpmnGatewayType.And => "AND",
                _ => throw new Exception($"Missing case for enum {Type}.")
            };

            if (outgoingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The {gatewayString} gateway with id \"{Id}\" has 0 outgoing arrows, but it has to have at least 1.");
            }

            if (incomingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The {gatewayString} gateway with id \"{Id}\" has 0 incoming arrows, but it has to have at least 1.");
            }
        }

        public override Tuple<List<DcrFlowElement>, DcrFlowElement> Convert()
        {
            throw new NotImplementedException();
        }
    }

    public class BpmnSubProcess : BpmnFlowElement
    {
        public List<BpmnFlowElement> flowElements;

        public BpmnSubProcess(string id)
        {
            Id = id;
            flowElements = new List<BpmnFlowElement>();
        }

        public BpmnSubProcess(string id, IEnumerable<BpmnFlowElement> newFlowElements)
        {
            Id = id;

            List<string> duplicateIds = newFlowElements.GroupBy(x => x.Id).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new BpmnDuplicateIdException($"Multiple flow elements with the ids \"{exceptionString}\" are given in the sub-process with id \"{Id}\".");
            }

            flowElements = newFlowElements.ToList();
        }

        public override void TestArrowCountValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount != 1)
            {
                throw new BpmnInvalidArrowException($"Sub-processes must have exactly 1 outgoing arrow, the sub-process with id \"{Id}\" has {outgoingArrowCount} outgoing arrows.");
            }

            if (incomingArrowCount != 1)
            {
                throw new BpmnInvalidArrowException($"Sub-processes must have exactly 1 outgoing arrow, the sub-process with id \"{Id}\" has {incomingArrowCount} incoming arrows.");
            }

            foreach (BpmnFlowElement flowElement in flowElements)
            {
                flowElement.TestArrowCountValidity();
            }
        }

        public override List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return flowElements.SelectMany(x => x.GetFlowElementsFlat()).Concat(new[] { this }).ToList();
        }

        public override Tuple<List<DcrFlowElement>, DcrFlowElement> Convert()
        {
            throw new NotImplementedException();
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
