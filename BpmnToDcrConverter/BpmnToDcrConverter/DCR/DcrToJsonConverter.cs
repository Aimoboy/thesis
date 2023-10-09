using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BpmnToDcrConverter.Dcr
{
    public static class DcrToJsonConverter
    {
        public static string GetJsonString(DcrGraph dcrGraph)
        {
            DcrJsonModel jsonModel = DcrGraphToJsonTypes(dcrGraph);
            return JsonSerializer.Serialize(jsonModel, new JsonSerializerOptions { IgnoreNullValues = true });
        }

        private static DcrJsonModel DcrGraphToJsonTypes(DcrGraph dcrGraph)
        {
            List<DcrFlowElement> allFlowElements = dcrGraph.GetFlowElementsFlat();
            List<Event> events = GetEventsFromDcrGraph(dcrGraph);

            List<Rule> rules = new List<Rule>();
            foreach (DcrFlowElement element in allFlowElements)
            {
                foreach (DcrFlowArrow arrow in element.OutgoingArrows)
                {
                    Rule rule = new Rule
                    {
                        type = Utilities.DcrArrowTypeToString(arrow.Type),
                        source = element.Id,
                        target = arrow.Element.Id,
                        guard = arrow.Condition != "" ? arrow.Condition : null
                    };

                    rules.Add(rule);
                }
            }

            return new DcrJsonModel
            {
                title = dcrGraph.Name,
                events = events,
                rules = rules,
                roles = new List<Role>()
            };
        }

        private static List<Event> GetEventsFromDcrGraph(DcrGraph dcrGraph)
        {
            List<Event> events = new List<Event>();

            List<DcrFlowElement> dcrElements = dcrGraph.GetFlowElements();
            foreach (DcrFlowElement dcrElement in dcrElements)
            {
                events = events.Concat(GetEventsFromDcrFlowElement(dcrElement, null)).ToList();
            }

            return events;
        }

        private static List<Event> GetEventsFromDcrFlowElement(DcrFlowElement dcrElement, string parent)
        {
            if (dcrElement is DcrActivity)
            {
                DcrActivity activity = (DcrActivity)dcrElement;
                Event @event = new Event
                {
                    id = activity.Id,
                    label = activity.Name,
                    included = activity.Included,
                    pending = activity.Pending,
                    executed = activity.Executed,
                    roles = activity.Role,
                    datatype = GetDataTypeString(activity.DataType)
                };

                if (parent != null)
                {
                    @event.parent = parent;
                }

                return new List<Event>() { @event };
            }

            if (dcrElement is DcrNesting)
            {
                DcrNesting nesting = (DcrNesting)dcrElement;
                Event @event = new Event
                {
                    id = nesting.Id,
                    label = nesting.Name,
                    type = "nesting"
                };

                if (parent != null)
                {
                    @event.parent = parent;
                }
                
                List<Event> events = nesting.Elements.SelectMany(x => GetEventsFromDcrFlowElement(x, nesting.Id)).ToList();
                events.Add(@event);

                return events;
            }

            if (dcrElement is DcrSubProcess)
            {
                DcrSubProcess subProcess = (DcrSubProcess)dcrElement;
                Event @event = new Event
                {
                    id = subProcess.Id,
                    label = subProcess.Name,
                    included = subProcess.Included,
                    executed = subProcess.Executed,
                    pending = subProcess.Pending,
                    type = "subprocess"
                };

                if (parent != null)
                {
                    @event.parent = parent;
                }

                List<Event> events = subProcess.Elements.SelectMany(x => GetEventsFromDcrFlowElement(x, subProcess.Id)).ToList();
                events.Add(@event);

                return events;
            }

            throw new Exception($"Missing case for {dcrElement.GetType()}.");
        }

        private static string GetDataTypeString(DataType type)
        {
            switch (type)
            {
                case DataType.Unknown:
                    return null;
                case DataType.Integer:
                    return "int";
                case DataType.Float:
                    return "float";
                default:
                    throw new Exception("Type is missing a case");
            }
        }
    }

    public class Role
    {
        public string title { get; set; }
        public string description { get; set; }
        public string specification { get; set; }
    }

    public class Event
    {
        public string id { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public string purpose { get; set; }
        public string guide { get; set; }
        public string type { get; set; }
        public string roles { get; set; }
        public string datatype { get; set; }
        public string parent { get; set; }
        public bool included { get; set; }
        public bool pending { get; set; }
        public bool executed { get; set; }
    }

    public class Rule
    {
        public string type { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string description { get; set; }
        public string duration { get; set; }
        public string guard { get; set; }
    }

    public class DcrJsonModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public List<Role> roles { get; set; }
        public List<Event> events { get; set; }
        public List<Rule> rules { get; set; }
    }
}
