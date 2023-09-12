using BpmnToDcrConverter.BPMN.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BpmnToDcrConverter.BPMN
{
    public class BpmnGraph
    {
        List<BpmnFlowElement> _flowElements;

        public BpmnGraph()
        {
            _flowElements = new List<BpmnFlowElement>();
        }

        public BpmnGraph(IEnumerable<BpmnFlowElement> flowElements)
        {
            List<string> allIds = flowElements.SelectMany(x => x.GetIds()).ToList();
            List<string> duplicateIds = allIds.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new BpmnDuplicateIdException($"Multiple flow elements with the ids \"{exceptionString}\" are given.");
            }

            _flowElements = flowElements.ToList();
        }

        public void AddFlowElements(IEnumerable<BpmnFlowElement> newFlowElements)
        {
            HashSet<string> allIds = _flowElements.SelectMany(x => x.GetIds()).ToHashSet();
            IEnumerable<string> newFlowElementIds = newFlowElements.SelectMany(x => x.GetIds());

            foreach (string id in newFlowElementIds)
            {
                if (allIds.Contains(id))
                {
                    throw new BpmnDuplicateIdException($"A flow element with id \"{id}\" already exists.");
                }
            }

            _flowElements = _flowElements.Concat(newFlowElements).ToList();
        }

        public List<BpmnFlowElement> GetFlowElements()
        {
            return _flowElements;
        }

        public List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return _flowElements.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public void TestGraphValidity()
        {
            foreach (BpmnFlowElement element in _flowElements)
            {
                element.TestArrowCountValidity();
                TestValidArrowReferences(element);
            }
        }

        private void TestValidArrowReferences(BpmnFlowElement element)
        {
            IEnumerable<BpmnFlowArrow> allArrows = element.OutgoingArrows.Concat(element.IncomingArrows);

            foreach (BpmnFlowArrow arrow in allArrows)
            {
                if (!_flowElements.Contains(arrow.Element))
                {
                    throw new BpmnInvalidArrowException($"BPMN flow element with id \"{element.Id}\" has a reference to a flow element that isn't in the graph.");
                }
            }
        }

        public void AddArrow(BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to)
        {
            from.OutgoingArrows.Add(new BpmnFlowArrow(type, to));
            to.IncomingArrows.Add(new BpmnFlowArrow(type, from));
        }

        public BpmnFlowElement GetFlowElementFromId(string id)
        {
            IEnumerable<BpmnFlowElement> allFlowElements = GetFlowElementsFlat();
            return allFlowElements.Where(x => x.Id == id).FirstOrDefault();
        }
    }
}
