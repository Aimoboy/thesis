using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BpmnToDcrConverter.Dcr
{
    public static class JsonExporter
    {
        public static void ExportToFile(DcrGraph dcrGraph, string path)
        {
            Root jsonRoot = DcrGraphToJsonTypes(dcrGraph);
            string jsonString = JsonSerializer.Serialize(jsonRoot, new JsonSerializerOptions { WriteIndented = true, IgnoreNullValues = true });

            File.WriteAllText(path, jsonString);
        }

        private static Root DcrGraphToJsonTypes(DcrGraph dcrGraph)
        {
            List<DcrFlowElement> allFlowElements = dcrGraph.GetFlowElementsFlat();

            List<DcrActivity> activities = allFlowElements.Where(x => x is DcrActivity).Select(x => (DcrActivity)x).ToList();
            List<DcrNesting> nestings = allFlowElements.Where(x => x is DcrNesting).Select(x => (DcrNesting)x).ToList();

            // TODO: set parents
            List<Event> convertedActivities = activities.Select(x => new Event
            {
                id = x.Id,
                label = x.Name,
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

            DCRModel dcrModel = new DCRModel
            {
                events = events,
                rules = rules,
                roles = new List<Role>()
            };

            return new Root
            {
                DCRModel = new List<DCRModel> { dcrModel }
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

    public class DCRModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public List<Role> roles { get; set; }
        public List<Event> events { get; set; }
        public List<Rule> rules { get; set; }
    }

    public class Root
    {
        public List<DCRModel> DCRModel { get; set; }
    }

}
