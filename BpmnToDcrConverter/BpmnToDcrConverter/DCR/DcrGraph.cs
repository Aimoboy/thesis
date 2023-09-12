using BpmnToDcrConverter.Dcr.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BpmnToDcrConverter.Dcr
{
    public class DcrGraph
    {
        private List<DcrFlowElement> _flowElements;

        public DcrGraph()
        {
            _flowElements = new List<DcrFlowElement>();
        }

        public DcrGraph(IEnumerable<DcrFlowElement> flowElements)
        {
            List<string> duplicateIds = _flowElements.SelectMany(x => x.GetFlowElementsFlat())
                                                     .GroupBy(x => x.Id)
                                                     .Where(x => x.Count() > 1)
                                                     .Select(x => x.Key)
                                                     .ToList();

            foreach (string duplicateId in duplicateIds)
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new DcrDuplicateIdException($"Multiple flow elements with the ids \"{exceptionString}\" are given.");
            }

            _flowElements = flowElements.ToList();
        }

        public List<DcrFlowElement> GetFlowElements()
        {
            return _flowElements;
        }

        public List<DcrFlowElement> GetFlowElementsFlat()
        {
            return _flowElements.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public void AddFlowElements(IEnumerable<DcrFlowElement> newFlowElements)
        {
            HashSet<string> currentIds = GetFlowElementsFlat().Select(x => x.Id).ToHashSet();
            IEnumerable<string> newFlowElementIds = newFlowElements.SelectMany(x => x.GetIds());

            foreach (string id in newFlowElementIds)
            {
                if (currentIds.Contains(id))
                {
                    throw new DcrDuplicateIdException($"A flow element with id \"{id}\" already exists.");
                }
            }

            _flowElements = _flowElements.Concat(newFlowElements).ToList();
        }

        public void AddArrow(DcrFlowArrowType type, DcrFlowElement from, DcrFlowElement to)
        {
            from.OutgoingArrows.Add(new DcrFlowArrow(type, to));
            to.IncomingArrows.Add(new DcrFlowArrow(type, from));
        }
    }
}
