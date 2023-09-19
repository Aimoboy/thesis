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

        public bool Included;
        public bool Executed;
        public bool Pending;

        public DcrActivity(string id, string name, bool included, bool executed, bool pending) : base(id)
        {
            Name = name;

            Included = included;
            Executed = executed;
            Pending = pending;
        }

        public DcrActivity(string id, string name) : this(id, name, true, false, false) { }

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

    public class DcrFlowArrow
    {
        public string Id;
        public DcrFlowArrowType Type;
        public DcrFlowElement Element;

        public DcrFlowArrow(DcrFlowArrowType type, DcrFlowElement element)
        {
            Id = "Relation_" + Guid.NewGuid().ToString("N");
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
