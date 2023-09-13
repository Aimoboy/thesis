using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter
{
    public static class Converter
    {
        public static DcrGraph ConvertBpmnToDcr(BpmnGraph bpmnGraph)
        {
            BpmnEvent start = bpmnGraph.GetFlowElements()
                                       .Where(x => x is BpmnEvent)
                                       .Select(x => (BpmnEvent)x)
                                       .Where(x => x.Type == BpmnEventType.Start)
                                       .FirstOrDefault();

            List<DcrFlowElement> flowElements = start.Convert().Item1;
            DcrGraph dcrGraph = new DcrGraph(flowElements);

            return dcrGraph;
        }
    }
}
