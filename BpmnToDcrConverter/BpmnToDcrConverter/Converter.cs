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
            BpmnStartEvent start = bpmnGraph.GetFlowElements()
                                            .Where(x => x is BpmnStartEvent)
                                            .Select(x => (BpmnStartEvent)x)
                                            .FirstOrDefault();

            List<DcrFlowElement> flowElements = start.ConvertToDcr().Item1;
            DcrGraph dcrGraph = new DcrGraph(flowElements);

            return dcrGraph;
        }
    }
}
