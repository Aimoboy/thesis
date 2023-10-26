using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace BpmnToDcrConverter
{
    public static class GraphTraceParser
    {
        private static readonly Parser<Tuple<string, string>> KeyValueParser =
            from key in Parse.LetterOrDigit.AtLeastOnce().Text().Token()
            from equ in Parse.Char('=').Token()
            from value in Parse.LetterOrDigit.Or(Parse.Char('_')).Or(Parse.Char('-')).AtLeastOnce().Text().Token()
            select new Tuple<string, string>(key, value);

        private static readonly Parser<List<TraceParseDefinition>> DefinitionParser =
            from firstBracket in Parse.Char('{').Token()
            from pairs in KeyValueParser.DelimitedBy(Parse.Char(',').Token())
            from secondBracket in Parse.Char('}').Token()
            select pairs.Select(x => new TraceParseDefinition(x.Item1, x.Item2)).ToList();

        public static readonly Parser<Tuple<string, string>> ActivityParser =
            from id in Parse.LetterOrDigit.AtLeastOnce().Text()
            from input in (from leftBracket in Parse.Char('(').Token()
                           from minus in Parse.String("-").Text().Optional()
                           from num in Parse.Digit.AtLeastOnce().Text()
                           from rest in (from dot in Parse.Char('.')
                                         from num in Parse.Digit.AtLeastOnce()
                                         select "." + num).Text().Optional()
                           from rightBracket in Parse.Char(')').Token()
                           select minus.GetOrElse("") + num + rest.GetOrElse("")).Optional()
            select new Tuple<string, string>(id, input.GetOrElse(""));

        private static readonly Parser<TraceParseTrace> TraceParser =
            from firstBracket in Parse.Char('{').Token()
            from secondBracket in Parse.Char('(').Token()
            from title in Parse.LetterOrDigit.Or(Parse.Char(' ')).Many().Text()
            from firstColon in Parse.Char(':').Token()
            from description in Parse.LetterOrDigit.Or(Parse.Char(' ')).Many().Text()
            from secondColon in Parse.Char(':').Token()
            from type in Parse.LetterOrDigit.Many().Text().Token()
            from thirdColon in Parse.Char(':').Token()
            from endState in Parse.LetterOrDigit.Or(Parse.Char(' ')).Many().Text().Token()
            from thirdBracket in Parse.Char(')').Token()
            from semiColon in Parse.Char(';').Token()
            from ids in ActivityParser.DelimitedBy(Parse.Char(',').Token()).Optional().Select(x => x.GetOrElse(new List<Tuple<string, string>>()))
            from fourthBracket in Parse.Char('}').Token()
            select new TraceParseTrace
            {
                Title = title,
                Description = description,
                Type = type,
                EndState = endState,
                Activities = ids.ToList()
            };

        public static readonly Parser<TraceParseResult> TraceResultParser =
            from firstBracket in Parse.Char('{').Token()
            from definitions in DefinitionParser.Token().Optional()
            from traces in TraceParser.Token().AtLeastOnce()
            from secondBracket in Parse.Char('}').Token().End()
            select new TraceParseResult
            {
                Definitions = definitions.GetOrElse(new List<TraceParseDefinition>()),
                Traces = traces.ToList()
            };

        private static readonly Parser<DataType> ParseInteger =
            from minus in Parse.String("-").Optional()
            from num in Parse.Digit.AtLeastOnce()
            select DataType.Integer;

        private static readonly Parser<DataType> ParseDecimal =
            from minus in Parse.Char('-').Optional()
            from num in Parse.Digit.AtLeastOnce()
            from dot in Parse.Char('.').Optional()
            from rest in Parse.Digit.AtLeastOnce()
            select DataType.Float;

        public static readonly Parser<DataType> DataTypeParser =
            ParseInteger.Or(ParseDecimal);
    }

    public class TraceParseResult
    {
        public List<TraceParseDefinition> Definitions;
        public List<TraceParseTrace> Traces;

        public List<GraphTrace> ToGraphTraces()
        {
            Dictionary<string, string> keyValueDict = Definitions.ToDictionary(x => x.Key, x => x.Value);

            List<GraphTrace> graphTraces = new List<GraphTrace>();
            foreach (TraceParseTrace trace in Traces)
            {
                List<TraceElement> traceElements = trace.Activities.Select(x =>
                {
                    string id = keyValueDict.ContainsKey(x.Item1) switch
                    {
                        true => keyValueDict[x.Item1],
                        false => x.Item1
                    };

                    if (x.Item2 == "")
                    {
                        return (TraceElement)new TraceActivity(id, "");
                    }

                    return (TraceElement)new TraceTransaction(id, "", GraphTraceParser.DataTypeParser.Parse(x.Item2), x.Item2);
                }).ToList();

                GraphTraceType type = trace.Type.ToLower() switch
                {
                    "none" => GraphTraceType.None,
                    "required" => GraphTraceType.Required,
                    "optional" => GraphTraceType.Optional,
                    "forbidden" => GraphTraceType.Forbidden,
                    _ => throw new Exception($"Invalid trace string {trace.Type}.")
                };

                GraphTraceEndState endState = trace.EndState.ToLower() switch
                {
                    "accepting" => GraphTraceEndState.Accepting,
                    "not accepting" => GraphTraceEndState.NotAccepting,
                    "dont care" => GraphTraceEndState.DontCare,
                    _ => throw new Exception($"Invalid end state string {trace.EndState}.")
                };

                graphTraces.Add(
                    new GraphTrace(trace.Title, trace.Description, type, endState, traceElements)
                );
            }

            return graphTraces;
        }
    }

    public class TraceParseDefinition
    {
        public string Key;
        public string Value;

        public TraceParseDefinition(string key, string val)
        {
            Key = key;
            Value = val;
        }
    }

    public class TraceParseTrace
    {
        public string Title;
        public string Description;
        public string Type;
        public string EndState;
        public List<Tuple<string, string>> Activities;
    }
}
