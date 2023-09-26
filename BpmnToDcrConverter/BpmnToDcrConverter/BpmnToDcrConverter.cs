using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter
{
    public static class BpmnToDcrConverter
    {
        public static DcrGraph ConvertBpmnToDcr(BpmnGraph bpmnGraph)
        {
            BpmnStartEvent start = bpmnGraph.GetAllFlowElements()
                                            .Where(x => x is BpmnStartEvent)
                                            .Select(x => (BpmnStartEvent)x)
                                            .FirstOrDefault();

            start.ConvertToDcr();
            DcrGraph dcrGraph = new DcrGraph(start.ConversionResult.ReachableFlowElements);

            Dictionary<string, DcrFlowElement> idToDcrFlowElementDict = dcrGraph.GetFlowElementsFlat().ToDictionary(x => x.Id);
            IEnumerable<BpmnFlowElement> missingConversion = bpmnGraph.GetAllFlowElements().Where(x => x.DelayedConversion.Any());
            foreach (BpmnFlowElement flowElement in missingConversion)
            {
                List<DcrFlowElement> flowElementToDcrFlowElements = flowElement.ConversionResult.StartElements;
                List<BpmnFlowArrow> delayedArrows = flowElement.DelayedConversion;

                foreach (DcrFlowElement dcrFlowElement in flowElementToDcrFlowElements)
                {
                    foreach (BpmnFlowArrow arrow in delayedArrows)
                    {

                    }
                }
            }

            return dcrGraph;
        }
    }
}
