using BpmnToDcrConverter.BPMN;
using System;
using System.Collections.Generic;

namespace BpmnToDcrConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BpmnEvent startEvent = new BpmnEvent("a", BpmnEventType.Start);
            BpmnEvent endEvent = new BpmnEvent("b", BpmnEventType.End);

            BpmnActivity activity1 = new BpmnActivity("0", "Activity1");
            BpmnActivity activity2 = new BpmnActivity("1", "Activity2");
            BpmnActivity activity3 = new BpmnActivity("2", "Activity3");

            List<BpmnFlowElement> activities = new List<BpmnFlowElement> { startEvent, endEvent, activity1, activity2, activity3 };
            BpmnGraph graph = new BpmnGraph(activities);

            graph.AddArrow(BpmnFlowArrowType.Sequence, startEvent, activity1);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, activity2);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity2, activity3);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity3, endEvent);

            graph.TestGraphValidity();
        }
    }
}
