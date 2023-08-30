using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter.BPMN
{
    public class BpmnGraph
    {
        List<BpmnFlowElement> _flowElements;

        public BpmnGraph(IEnumerable<BpmnFlowElement> flowElements)
        {
            List<int> duplicateIds = flowElements.GroupBy(x => x.Id).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new Exception($"Multiple flow elements with the ids \"{exceptionString}\" are given.");
            }

            _flowElements = flowElements.ToList();
        }

        public void AddFlowElements(IEnumerable<BpmnFlowElement> flowElements)
        {
            HashSet<int> ids = _flowElements.Select(x => x.Id).ToHashSet();

            foreach (BpmnFlowElement element in flowElements)
            {
                if (ids.Contains(element.Id))
                {
                    throw new Exception($"A flow element with id \"{element.Id}\" already exists.");
                }
            }

            _flowElements = _flowElements.Concat(flowElements).ToList();
        }

        public List<BpmnFlowElement> GetFlowElements()
        {
            return _flowElements;
        }

        public void TestGraphValidity()
        {
            foreach (BpmnFlowElement element in _flowElements)
            {
                element.TestArrowCountValidity();
            }
        }
    }
}
