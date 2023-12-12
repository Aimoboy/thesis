using BpmnToDcrConverter.Bpmn.Exceptions;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace BpmnToDcrConverter.Bpmn
{
    public abstract class BpmnFlowElement
    {
        public string Id;
        public List<BpmnFlowArrow> OutgoingArrows = new List<BpmnFlowArrow>();
        public List<BpmnFlowArrow> IncomingArrows = new List<BpmnFlowArrow>();

        public int X = 0;
        public int Y = 0;
        public int Width = 0;
        public int Height = 0;

        public bool Visited = false;
        public List<BpmnFlowArrow> DelayedConversion = new List<BpmnFlowArrow>();

        public BpmnFlowElement(string id)
        {
            Id = id;
        }

        public void SetSize(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public abstract void TestValidity();

        public List<string> GetAllIds()
        {
            return GetFlowElementsFlat().Select(x => x.Id).ToList();
        }

        public virtual List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return new List<BpmnFlowElement> { this };
        }

        protected abstract BpmnFlowElement CopyElement();

        public BpmnFlowElement Copy()
        {
            BpmnFlowElement copiedElement = CopyElement();
            copiedElement.Id = Id;
            copiedElement.X = X;
            copiedElement.Y = Y;
            copiedElement.Width = Width;
            copiedElement.Height = Height;

            copiedElement.OutgoingArrows = OutgoingArrows.ConvertAll(x => x.Copy());
            copiedElement.IncomingArrows = IncomingArrows.ConvertAll(x => x.Copy());

            return copiedElement;
        }

        public virtual List<BpmnFlowElement> GetElementCollectionFromId(string id)
        {
            return null;
        }

        public virtual void DeleteElementFromId(string id)
        {

        }

        public void RemoveAllArrows()
        {
            foreach (BpmnFlowArrow arrow in OutgoingArrows)
            {
                Utilities.RemoveBpmnArrow(this, arrow);
            }

            foreach (BpmnFlowArrow fromArrow in IncomingArrows)
            {
                BpmnFlowArrow toArrow = fromArrow.Element.OutgoingArrows.Where(x => x.Element == this).FirstOrDefault();
                Utilities.RemoveBpmnArrow(fromArrow.Element, toArrow);
            }
        }
    }

    public class BpmnActivity : BpmnFlowElement
    {
        public string Name;

        public BpmnActivity(string id, string name) : base(id)
        {
            Name = name;
        }

        public override void TestValidity()
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

        protected override BpmnFlowElement CopyElement()
        {
            BpmnActivity copiedElement = new BpmnActivity(Id, Name);
            return copiedElement;
        }
    }

    public class BpmnStartEvent : BpmnFlowElement
    {
        public BpmnStartEvent(string id) : base(id) { }

        public override void TestValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount != 1)
            {
                throw new BpmnInvalidArrowException($"The START event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 1.");
            }

            if (incomingArrowCount != 0)
            {
                throw new BpmnInvalidArrowException($"The START event with id \"{Id}\" has {incomingArrowCount} incoming arrows, but it has to have 1.");
            }
        }

        protected override BpmnFlowElement CopyElement()
        {
            BpmnStartEvent copiedElement = new BpmnStartEvent(Id);
            return copiedElement;
        }
    }

    public class BpmnEndEvent : BpmnFlowElement
    {
        public BpmnEndEvent(string id) : base(id) { }

        public override void TestValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount != 0)
            {
                throw new BpmnInvalidArrowException($"The END event with id \"{Id}\" has {outgoingArrowCount} outgoing arrows, but it has to have 0.");
            }

            if (incomingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The END event with id \"{Id}\" has 0 incoming arrows, but it has to have at least 1.");
            }
        }

        protected override BpmnFlowElement CopyElement()
        {
            BpmnEndEvent copiedElement = new BpmnEndEvent(Id);
            return copiedElement;
        }
    }

    public class BpmnExclusiveGateway : BpmnFlowElement
    {
        public string DefaultPath { get; private set; }

        public BpmnExclusiveGateway(string id) : base(id)
        {
            DefaultPath = "";
        }

        public BpmnExclusiveGateway(string id, string defaultPath) : base(id)
        {
            DefaultPath = defaultPath;
        }

        public override void TestValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The OR gateway with id \"{Id}\" has 0 outgoing arrows, but it has to have at least 1.");
            }

            if (incomingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The OR gateway with id \"{Id}\" has 0 incoming arrows, but it has to have at least 1.");
            }

            int outgoingArrowsWithoutConditionCount = OutgoingArrows.Where(x => x.Condition == "").Count();
            if (outgoingArrowsWithoutConditionCount > 1)
            {
                throw new BpmnInvalidArrowException($"The OR gateway with id \"{Id}\" has {outgoingArrowsWithoutConditionCount} outgoing arrows without a condition, but it can have a maximum of 1");
            }
        }

        protected override BpmnFlowElement CopyElement()
        {
            BpmnExclusiveGateway copiedElement = new BpmnExclusiveGateway(Id, DefaultPath);
            return copiedElement;
        }
    }

    public class BpmnParallelGateway : BpmnFlowElement
    {
        public BpmnParallelGateway(string id) : base(id) { }

        public override void TestValidity()
        {
            int outgoingArrowCount = OutgoingArrows.Count;
            int incomingArrowCount = IncomingArrows.Count;

            if (outgoingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The AND gateway with id \"{Id}\" has 0 outgoing arrows, but it has to have at least 1.");
            }

            if (incomingArrowCount == 0)
            {
                throw new BpmnInvalidArrowException($"The AND gateway with id \"{Id}\" has 0 incoming arrows, but it has to have at least 1.");
            }
        }

        protected override BpmnFlowElement CopyElement()
        {
            BpmnParallelGateway copiedElement = new BpmnParallelGateway(Id);
            return copiedElement;
        }
    }

    public class BpmnSubProcess : BpmnFlowElement
    {
        public List<BpmnFlowElement> FlowElements { get; private set; }

        public string StartEventId;

        public BpmnSubProcess(string id) : base(id)
        {

        }

        public BpmnSubProcess(string id, IEnumerable<BpmnFlowElement> newFlowElements) : base(id)
        {
            SetFlowElements(newFlowElements);
        }

        public void SetFlowElements(IEnumerable<BpmnFlowElement> newFlowElements)
        {
            List<string> duplicateIds = newFlowElements.GroupBy(x => x.Id).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new BpmnDuplicateIdException($"Multiple flow elements with the ids \"{exceptionString}\" are given in the sub-process with id \"{Id}\".");
            }

            FlowElements = newFlowElements.ToList();
            StartEventId = FlowElements.OfType<BpmnStartEvent>().First().Id;
        }

        public override void TestValidity()
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

            foreach (BpmnFlowElement flowElement in FlowElements)
            {
                flowElement.TestValidity();
            }
        }

        public override List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return FlowElements.SelectMany(x => x.GetFlowElementsFlat()).Concat(new[] { this }).ToList();
        }

        protected override BpmnFlowElement CopyElement()
        {
            BpmnSubProcess copiedElement = new BpmnSubProcess(Id);
            copiedElement.FlowElements = FlowElements.ConvertAll(x => x.Copy());
            return copiedElement;
        }

        public override List<BpmnFlowElement> GetElementCollectionFromId(string id)
        {
            if (FlowElements.Select(x => x.Id).Contains(id))
            {
                return FlowElements;
            }

            List<List<BpmnFlowElement>> results = FlowElements.ConvertAll(x => x.GetElementCollectionFromId(id));
            foreach (List<BpmnFlowElement> lst in results)
            {
                if (lst != null)
                {
                    return lst;
                }
            }

            return null;
        }

        public override void DeleteElementFromId(string id)
        {
            Utilities.RemoveIdFromBpmnCollection(id, FlowElements);
        }
    }

    public class BpmnFlowArrow
    {
        public BpmnFlowArrowType Type;
        public BpmnFlowElement Element;
        public string Condition;

        public BpmnFlowArrow(BpmnFlowArrowType type, BpmnFlowElement element, string condition)
        {
            Type = type;
            Element = element;
            Condition = condition;
        }

        public BpmnFlowArrow Copy()
        {
            BpmnFlowArrow newArrow = new BpmnFlowArrow(Type, Element, Condition);
            return newArrow;
        }
    }

    public enum BpmnFlowArrowType
    {
        Message,
        Sequence
    }
}
