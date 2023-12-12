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

        public int X = 0;
        public int Y = 0;
        public int Width = 0;
        public int Height = 0;

        public string ArrowCondition = "";

        public DcrFlowElement(string id)
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

        public List<string> GetIds()
        {
            return GetFlowElementsFlat().Select(x => x.Id).ToList();
        }

        public abstract List<DcrFlowElement> GetFlowElementsFlat();
    }

    public class DcrActivity : DcrFlowElement
    {
        public string Name;
        public string Role;

        public bool Included;
        public bool Executed;
        public bool Pending;

        public DataType DataType;

        public DcrActivity(string id, string name, string role, bool included, bool executed, bool pending, DataType dataType) : base(id)
        {
            Name = name;
            Role = role;

            Included = included;
            Executed = executed;
            Pending = pending;
            DataType = dataType;
        }

        public DcrActivity(string id, string name) : this(id, name, "", true, false, false, DataType.Unknown) { }

        public override List<DcrFlowElement> GetFlowElementsFlat()
        {
            return new List<DcrFlowElement> { this };
        }
    }

    public class DcrNesting : DcrFlowElement
    {
        public string Name;
        public List<DcrFlowElement> Elements;

        public DcrNesting(string id, string name, IEnumerable<DcrFlowElement> elements) : base(id)
        {
            Name = name;
            Elements = elements.ToList();
        }

        public DcrNesting(string id, IEnumerable<DcrFlowElement> elements) : this(id, "", elements) { }

        public override List<DcrFlowElement> GetFlowElementsFlat()
        {
            return Elements.SelectMany(x => x.GetFlowElementsFlat()).Concat(new[] { this }).ToList();
        }
    }

    public class DcrSubProcess : DcrFlowElement
    {
        public string Name;
        public List<DcrFlowElement> Elements;

        public bool Included;
        public bool Executed;
        public bool Pending;

        public string StartActivityId;

        public DcrSubProcess(string id, string name, IEnumerable<DcrFlowElement> elements, bool included, bool executed, bool pending, string startActivityId) : base(id)
        {
            Name = name;
            Elements = elements.ToList();

            Included = included;
            Executed = executed;
            Pending = pending;

            StartActivityId = startActivityId;
        }

        public DcrSubProcess(string id, IEnumerable<DcrFlowElement> elements) : this(id, "", elements, true, false, false, "") { }

        public override List<DcrFlowElement> GetFlowElementsFlat()
        {
            return Elements.SelectMany(x => x.GetFlowElementsFlat()).Concat(new[] { this }).ToList();
        }
    }

    public class DcrFlowArrow
    {
        public string Id;
        public DcrFlowArrowType Type;
        public DcrFlowElement Element;
        public string Condition;

        public DcrFlowArrow(DcrFlowArrowType type, DcrFlowElement element, string condition)
        {
            Id = "Relation_" + Guid.NewGuid().ToString("N");
            Type = type;
            Element = element;
            Condition = condition;
        }

        public DcrFlowArrow(DcrFlowArrowType type, DcrFlowElement element) : this(type, element, "") { }
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
