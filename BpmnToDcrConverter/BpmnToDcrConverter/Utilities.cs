using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter
{
    public static class Utilities
    {
        public static string DcrArrowTypeToString(DcrFlowArrowType type)
        {
            switch (type)
            {
                case DcrFlowArrowType.Condition:
                    return "condition";
                case DcrFlowArrowType.Response:
                    return "response";
                case DcrFlowArrowType.Include:
                    return "include";
                case DcrFlowArrowType.Exclude:
                    return "exclude";
                case DcrFlowArrowType.Milestone:
                    return "milestone";
                default:
                    throw new Exception("Unhandled enum type.");
            }
        }

        public static void AddBpmnArrow(BpmnFlowElement from, BpmnFlowElement to, BpmnFlowArrowType arrowType, string condition)
        {
            from.OutgoingArrows.Add(new BpmnFlowArrow(arrowType, to, condition));
            to.IncomingArrows.Add(new BpmnFlowArrow(arrowType, from, condition));
        }

        public static void RemoveBpmnArrow(BpmnFlowElement from, BpmnFlowArrow arrow)
        {
            from.OutgoingArrows = from.OutgoingArrows.Where(x => x.Element != arrow.Element).ToList();
            arrow.Element.IncomingArrows = arrow.Element.IncomingArrows.Where(x => x.Element != from).ToList();
        }

        public static void AddDcrArrow(DcrFlowElement from, DcrFlowElement to, DcrFlowArrowType arrowType, string condition)
        {
            from.OutgoingArrows.Add(new DcrFlowArrow(arrowType, to, condition));
            to.IncomingArrows.Add(new DcrFlowArrow(arrowType, from, condition));
        }

        public static void RemoveIdFromBpmnCollection(string id, List<BpmnFlowElement> collection)
        {
            BpmnFlowElement match = collection.Where(x => x.Id == id).FirstOrDefault();
            if (match != null)
            {
                match.RemoveAllArrows();
                collection.Remove(match);
            }

            foreach (BpmnFlowElement element in collection)
            {
                element.DeleteElementFromId(id);
            }
        }

        public static string EscapeStringForApi(string str)
        {
            return str.Replace("\"", "\\\"");
        }
    }
}
