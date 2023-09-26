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

            List<DcrActivity> activities = allFlowElements.Where(x => x is DcrActivity).Select(x => (DcrActivity)x).ToList();
            List<DcrNesting> nestings = allFlowElements.Where(x => x is DcrNesting).Select(x => (DcrNesting)x).ToList();

            // TODO: set parents
            List<Event> convertedActivities = activities.Select(x => new Event
            {
                id = x.Id,
                label = x.Name,
                included = x.Included,
                pending = x.Pending,
                executed = x.Executed
            }).ToList();

            List<Event> convertedNestings = nestings.Select(x => new Event
            {
                id = x.Id,
                label = x.Name,
                type = "nesting"
            }).ToList();

            List<Event> events = convertedActivities.Concat(convertedNestings).ToList();

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
