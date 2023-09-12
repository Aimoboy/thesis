using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter.Dcr
{
    public abstract class DcrFlowElement
    {
        public string Id;

        public List<DcrFlowArrow> OutgoingArrows = new List<DcrFlowArrow>();
        public List<DcrFlowArrow> IncomingArrows = new List<DcrFlowArrow>();

        public List<string> GetIds()
        {
            return GetFlowElementsFlat().Select(x => x.Id).ToList();
        }

        public abstract List<DcrFlowElement> GetFlowElementsFlat();
    }

    public class DcrActivity : DcrFlowElement
    {
        public string Name;

        public bool Included;
        public bool Executed;
        public bool Pending;

        public DcrActivity(string name, bool included, bool executed, bool pending)
        {
            Name = name;

            Included = included;
            Executed = executed;
            Pending = pending;
        }

        public DcrActivity(string name) : this(name, true, false, false) { }

        public override List<DcrFlowElement> GetFlowElementsFlat()
        {
            return new List<DcrFlowElement> { this };
        }
    }

    public class DcrGroup : DcrFlowElement
    {
        public string Name;
        public List<DcrFlowElement> Activities;

        public DcrGroup(string name, IEnumerable<DcrFlowElement> activities)
        {
            Name = name;
            Activities = activities.ToList();
        }

        public override List<DcrFlowElement> GetFlowElementsFlat()
        {
            return Activities.SelectMany(x => x.GetFlowElementsFlat()).Concat(new[] { this }).ToList();
        }
    }

    public class DcrFlowArrow
    {
        public DcrFlowArrowType Type;
        public DcrFlowElement Element;

        public DcrFlowArrow(DcrFlowArrowType type, DcrFlowElement element)
        {
            Type = type;
            Element = element;
        }
    }

    public enum DcrFlowArrowType
    {
        Condition,
        Response,
        Include,
        Exclude,
        Milestone
    }
}
