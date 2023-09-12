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

            BpmnFlowElement firstElement = start.OutgoingArrows.FirstOrDefault().Element;

            throw new NotImplementedException();
        }
    }
}
