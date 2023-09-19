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

        public ConversionResult ConversionResult;

        public BpmnFlowElement(string id)
        {
            Id = id;
            ConversionResult = null;
        }

        public void SetSize(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public abstract void TestArrowCountValidity();

        public List<string> GetIds()
        {
            return GetFlowElementsFlat().Select(x => x.Id).ToList();
        }

        public virtual List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return new List<BpmnFlowElement> { this };
        }

        public abstract void ConvertToDcr();
    }

    public class BpmnActivity : BpmnFlowElement
    {
        public string Name;

        public BpmnActivity(string id, string name) : base(id)
        {
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

        public override void ConvertToDcr()
        {
            if (ConversionResult != null)
            {
                return;
            }

            BpmnFlowElement nextElement = OutgoingArrows.FirstOrDefault().Element;
            nextElement.ConvertToDcr();

            DcrActivity activity = new DcrActivity(Id, Name);
            activity.SetSize(X, Y, Width, Height);
            foreach (DcrFlowElement element in nextElement.ConversionResult.StartElements)
            {
                activity.OutgoingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Condition, element));
                element.IncomingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Condition, activity));
            }

            activity.OutgoingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Exclude, activity));
            activity.IncomingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Exclude, activity));

            ConversionResult = new ConversionResult
            {
                ReachableFlowElements = nextElement.ConversionResult.ReachableFlowElements.Concat(new[] { activity }).ToList(),
                StartElements = new List<DcrFlowElement> { activity }
            };
        }
    }

    public class BpmnStartEvent : BpmnFlowElement
    {
        public BpmnStartEvent(string id) : base(id) { }

        public override void TestArrowCountValidity()
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

        public override void ConvertToDcr()
        {
            BpmnFlowElement nextElement = OutgoingArrows.FirstOrDefault().Element;
            nextElement.ConvertToDcr();

            ConversionResult = nextElement.ConversionResult;
        }
    }

    public class BpmnEndEvent : BpmnFlowElement
    {
        public BpmnEndEvent(string id) : base(id) { }

        public override void TestArrowCountValidity()
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

        public override void ConvertToDcr()
        {
            ConversionResult = new ConversionResult
            {
                ReachableFlowElements = new List<DcrFlowElement>(),
                StartElements = new List<DcrFlowElement>()
            };
        }
    }

    public class BpmnExclusiveGateway : BpmnFlowElement
    {
        public BpmnExclusiveGateway(string id) : base(id) { }

        public override void TestArrowCountValidity()
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
        }

        public override void ConvertToDcr()
        {
            if (ConversionResult != null)
            {
                return;
            }

            // Convert each path
            List<BpmnFlowElement> nextElements = OutgoingArrows.ConvertAll(x => x.Element);
            foreach (BpmnFlowElement element in nextElements)
            {
                element.ConvertToDcr();
            }

            // Add exclusive arrows
            foreach (BpmnFlowElement element in nextElements)
            {
                List<DcrFlowElement> reachableElements = element.ConversionResult.ReachableFlowElements;
                foreach (DcrFlowElement newDcrElement in element.ConversionResult.StartElements)
                {
                    List<BpmnFlowElement> otherElements = nextElements.Except(new[] { element }).ToList();
                    foreach (BpmnFlowElement otherElement in otherElements)
                    {
                        List<DcrFlowElement> elementsToExclude = otherElement.ConversionResult.ReachableFlowElements.Except(reachableElements).ToList();
                        foreach (DcrFlowElement elementToExclude in elementsToExclude)
                        {
                            newDcrElement.OutgoingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Exclude, elementToExclude));
                            elementToExclude.IncomingArrows.Add(new DcrFlowArrow(DcrFlowArrowType.Exclude, newDcrElement));
                        }
                    }
                }
            }

            ConversionResult = new ConversionResult
            {
                ReachableFlowElements = nextElements.SelectMany(x => x.ConversionResult.ReachableFlowElements).Distinct().ToList(),
                StartElements = nextElements.SelectMany(x => x.ConversionResult.StartElements).ToList()
            };
        }
    }

    public class BpmnParallelGateway : BpmnFlowElement
    {
        public BpmnParallelGateway(string id) : base(id) { }

        public override void TestArrowCountValidity()
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

        public override void ConvertToDcr()
        {
            if (ConversionResult != null)
            {
                return;
            }

            // Convert each path
            List<BpmnFlowElement> nextElements = OutgoingArrows.ConvertAll(x => x.Element);
            foreach (BpmnFlowElement element in nextElements)
            {
                element.ConvertToDcr();
            }

            ConversionResult = new ConversionResult
            {
                ReachableFlowElements = nextElements.SelectMany(x => x.ConversionResult.ReachableFlowElements).Distinct().ToList(),
                StartElements = nextElements.SelectMany(x => x.ConversionResult.StartElements).ToList()
            };
        }
    }

    public class BpmnSubProcess : BpmnFlowElement
    {
        public List<BpmnFlowElement> flowElements;

        public BpmnSubProcess(string id) : base(id)
        {
            flowElements = new List<BpmnFlowElement>();
        }

        public BpmnSubProcess(string id, IEnumerable<BpmnFlowElement> newFlowElements) : base(id)
        {
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

        public override void ConvertToDcr()
        {
            throw new NotImplementedException();
        }
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

    public class ConversionResult
    {
        public List<DcrFlowElement> ReachableFlowElements;
        public List<DcrFlowElement> StartElements;
    }
}
