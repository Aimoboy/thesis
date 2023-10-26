using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace BpmnToDcrConverter
{
    public class GraphTrace
    {
        public string Title;
        public string Description;
        public string InitTime;
        public GraphTraceType Type;
        public GraphTraceEndState EndState;
        public List<TraceElement> TraceElements = new List<TraceElement>();

        public GraphTrace(string title, string description, string initTime, GraphTraceType type, GraphTraceEndState endState, List<TraceElement> traceElements)
        {
            Title = title;
            Description = description;
            InitTime = initTime;
            Type = type;
            EndState = endState;
            TraceElements = traceElements;
        }

        public GraphTrace(string title, string description, GraphTraceType type, GraphTraceEndState endState, List<TraceElement> traceElements) : this(title,
                                                                                                                                                       description,
                                                                                                                                                       DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
                                                                                                                                                       type,
                                                                                                                                                       endState,
                                                                                                                                                       traceElements) { }

        public XmlDocument ToXml()
        {
            XmlDocument doc = new XmlDocument();

            XmlElement log = doc.CreateElement("log");
            doc.AppendChild(log);

            XmlElement trace = doc.CreateElement("trace");
            trace.SetAttribute("id", "0");
            trace.SetAttribute("title", Title);
            trace.SetAttribute("description", Description);
            trace.SetAttribute("init", InitTime);
            trace.SetAttribute("type", TraceTypeToString(Type));

            if (EndState == GraphTraceEndState.Accepting)
            {
                trace.SetAttribute("isAccepting", "true");
            }

            if (EndState == GraphTraceEndState.NotAccepting)
            {
                trace.SetAttribute("isAccepting", "false");
            }

            trace.SetAttribute("percentage", "0.00");
            trace.SetAttribute("isScenario", "true");
            trace.SetAttribute("businessType", "none");

            log.AppendChild(trace);

            foreach (TraceElement traceElement in TraceElements)
            {
                traceElement.AddToXML(doc, trace);
            }

            return doc;
        }

        private static string TraceTypeToString(GraphTraceType type)
        {
            switch (type)
            {
                case GraphTraceType.None:
                    return "None";
                case GraphTraceType.Required:
                    return "Required";
                case GraphTraceType.Optional:
                    return "Optional";
                case GraphTraceType.Forbidden:
                    return "Forbidden";
                default:
                    throw new Exception("Unhandled case.");
            }
        }
    }

    public abstract class TraceElement
    {
        public abstract void AddToXML(XmlDocument doc, XmlElement parent);
    }

    public class TraceActivity : TraceElement
    {
        public string Id;
        public string Role;

        public TraceActivity(string id, string role)
        {
            Id = id;
            Role = role;
        }

        public override void AddToXML(XmlDocument doc, XmlElement parent)
        {
            XmlElement elementXml = doc.CreateElement("event");

            elementXml.SetAttribute("id", Id);
            elementXml.SetAttribute("role", Role);

            parent.AppendChild(elementXml);
        }
    }

    public class TraceTransaction : TraceElement
    {
        public string Id;
        public string Role;
        public DataType DataType;
        public string Data;

        public TraceTransaction(string id, string role, DataType type, string data)
        {
            Id = id;
            Role = role;
            DataType = type;
            Data = data;
        }

        public override void AddToXML(XmlDocument doc, XmlElement parent)
        {
            XmlElement transactionXml = doc.CreateElement("transaction");
            transactionXml.SetAttribute("id", Id);
            transactionXml.SetAttribute("role", Role);

            XmlElement eventXml = doc.CreateElement("event");
            eventXml.SetAttribute("id", Id);
            eventXml.SetAttribute("data", Data);
            eventXml.SetAttribute("type", DataTypeToString(DataType));
            eventXml.SetAttribute("isNull", "false");

            transactionXml.AppendChild(eventXml);
            parent.AppendChild(transactionXml);
        }

        private string DataTypeToString(DataType type)
        {
            return type switch
            {
                DataType.Integer => "int",
                DataType.Float => "float",
                DataType.Unknown => throw new Exception("Parsing somehow failed making the DataType unknown."),
                _ => throw new Exception("Unhandled case.")
            };
        }
    }

    public enum GraphTraceType
    {
        None,
        Required,
        Optional,
        Forbidden
    }

    public enum GraphTraceEndState
    {
        Accepting,
        NotAccepting,
        DontCare
    }
}
