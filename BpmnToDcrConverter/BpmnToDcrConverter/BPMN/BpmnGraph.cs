using BpmnToDcrConverter.Bpmn.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BpmnToDcrConverter.Bpmn
{
    public class BpmnGraph
    {
        public string Id;

        List<BpmnPool> _pools = new List<BpmnPool>();

        public BpmnGraph(string id) {
            Id = id;
        }

        public BpmnGraph(IEnumerable<BpmnFlowElement> newFlowElements) : this(Guid.NewGuid().ToString("N"), newFlowElements) { }

        public BpmnGraph(string id, IEnumerable<BpmnFlowElement> newFlowElements) : this(id)
        {
            BpmnPool pool = new BpmnPool(new BpmnPoolLane(newFlowElements));
            AddPool(pool);
        }

        public BpmnGraph(string id, IEnumerable<BpmnPool> newPools) : this(id)
        {
            AddPools(newPools);
        }

        public void AddPools(IEnumerable<BpmnPool> newPools)
        {
            List<string> currentIds = GetAllIds();
            List<string> newIds = newPools.SelectMany(x => x.GetAllIds()).ToList();
            List<string> allIds = currentIds.Concat(newIds).ToList();

            List<string> duplicateIds = allIds.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new BpmnDuplicateIdException($"Multiple elements have the ids \"{exceptionString}\".");
            }

            _pools = _pools.Concat(newPools).ToList();
        }

        public void AddPool(BpmnPool pool)
        {
            AddPools(new[] { pool });
        }

        public List<BpmnFlowElement> GetAllFlowElements()
        {
            return _pools.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public List<string> GetAllIds()
        {
            return _pools.SelectMany(x => x.GetAllIds()).Concat(new[] { Id }).ToList();
        }

        public void TestGraphValidity()
        {
            foreach (BpmnFlowElement element in GetAllFlowElements())
            {
                element.TestArrowCountValidity();
                TestValidArrowReferences(element);
            }
        }

        private void TestValidArrowReferences(BpmnFlowElement element)
        {
            List<BpmnFlowArrow> allArrows = element.OutgoingArrows.Concat(element.IncomingArrows).ToList();
            HashSet<BpmnFlowElement> allElements = GetAllFlowElements().ToHashSet();

            foreach (BpmnFlowArrow arrow in allArrows)
            {
                if (!allElements.Contains(arrow.Element))
                {
                    throw new BpmnInvalidArrowException($"BPMN flow element with id \"{element.Id}\" has a reference to a flow element that isn't in the graph.");
                }
            }
        }

        public void AddArrow(BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to, string condition)
        {
            from.OutgoingArrows.Add(new BpmnFlowArrow(type, to, condition));
            to.IncomingArrows.Add(new BpmnFlowArrow(type, from, condition));
        }

        public void AddArrow(BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to)
        {
            AddArrow(type, from, to, "");
        }

        public BpmnFlowElement GetFlowElementFromId(string id)
        {
            List<BpmnFlowElement> allFlowElements = GetAllFlowElements();
            return allFlowElements.Where(x => x.Id == id).FirstOrDefault();
        }

        public BpmnGraph Copy()
        {
            BpmnGraph newGraph = new BpmnGraph(Id);

            // Copy all elements
            newGraph.AddPools(_pools.Select(x => x.Copy()));

            // Update arrow references
            List<BpmnFlowElement> allElements = newGraph.GetAllFlowElements();
            Dictionary<string, BpmnFlowElement> idToElementDict = allElements.ToDictionary(x => x.Id);
            foreach (BpmnFlowElement flowElement in allElements)
            {
                foreach (BpmnFlowArrow arrow in flowElement.OutgoingArrows)
                {
                    string elementId = arrow.Element.Id;
                    arrow.Element = idToElementDict[elementId];
                }

                foreach (BpmnFlowArrow arrow in flowElement.IncomingArrows)
                {
                    string elementId = arrow.Element.Id;
                    arrow.Element = idToElementDict[elementId];
                }
            }


            return newGraph;
        }

        public List<string> FindCycles()
        {
            BpmnGraph graphCopy = Copy();
            graphCopy.IsolateCycles();
            return graphCopy.GetAllFlowElements().Where(x => x.IncomingArrows.Count > 0 || x.OutgoingArrows.Count > 0).Select(x => x.Id).ToList();
        }

        private void IsolateCycles()
        {
            List<BpmnFlowElement> leaves = GetLeaves();
            while (leaves.Any())
            {
                // Remove incoming arrows from pointed to elements
                foreach (BpmnFlowElement flowElement in leaves)
                {
                    foreach (BpmnFlowArrow arrow in flowElement.OutgoingArrows)
                    {
                        BpmnFlowElement element = arrow.Element;
                        BpmnFlowArrow correspondingArrow = element.IncomingArrows.Where(x => x.Element.Id == flowElement.Id).FirstOrDefault();

                        element.IncomingArrows = element.IncomingArrows.Where(x => x != correspondingArrow).ToList();
                    }

                    foreach (BpmnFlowArrow arrow in flowElement.IncomingArrows)
                    {
                        BpmnFlowElement element = arrow.Element;
                        BpmnFlowArrow correspondingArrow = element.OutgoingArrows.Where(x => x.Element.Id == flowElement.Id).FirstOrDefault();

                        element.OutgoingArrows = element.OutgoingArrows.Where(x => x != correspondingArrow).ToList();
                    }

                    flowElement.OutgoingArrows.Clear();
                    flowElement.IncomingArrows.Clear();
                }

                leaves = GetLeaves();
            }
        }

        private List<BpmnFlowElement> GetLeaves()
        {
            List<BpmnFlowElement> leaves = GetAllFlowElements().Where(x => (x.OutgoingArrows.Count == 0 && x.IncomingArrows.Count > 0) || (x.IncomingArrows.Count == 0 && x.OutgoingArrows.Count > 0)).ToList();
            return leaves;
        }
    }

    public class BpmnPool
    {
        public string Id;
        public List<BpmnPoolLane> Lanes = new List<BpmnPoolLane>();

        public BpmnPool(string id)
        {
            Id = id;
        }

        public BpmnPool() : this(Guid.NewGuid().ToString("N")) { }

        public BpmnPool(string id, IEnumerable<BpmnPoolLane> lanes) : this(id)
        {
            Lanes = lanes.ToList();
        }

        public BpmnPool(IEnumerable<BpmnPoolLane> lanes) : this(Guid.NewGuid().ToString("N"))
        {
            Lanes = lanes.ToList();
        }

        public BpmnPool(BpmnPoolLane lane) : this(Guid.NewGuid().ToString("N"), new[] { lane }) { }

        public List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return Lanes.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public List<string> GetAllIds()
        {
            return Lanes.SelectMany(x => x.GetAllIds()).Concat(new[] { Id }).ToList();
        }

        public BpmnPool Copy()
        {
            BpmnPool newPool = new BpmnPool(Id);
            newPool.Lanes = Lanes.ConvertAll(x => x.Copy());
            return newPool;
        }
    }

    public class BpmnPoolLane
    {
        public string Id;
        public List<BpmnFlowElement> Elements = new List<BpmnFlowElement>();

        public BpmnPoolLane(string id)
        {
            Id = id;
        }

        public BpmnPoolLane() : this(Guid.NewGuid().ToString("N")) { }

        public BpmnPoolLane(string id, IEnumerable<BpmnFlowElement> flowElements) : this(id) {
            Elements = flowElements.ToList();
        }

        public BpmnPoolLane(IEnumerable<BpmnFlowElement> flowElements) : this(Guid.NewGuid().ToString("N"))
        {
            Elements = flowElements.ToList();
        }

        public List<BpmnFlowElement> GetFlowElementsFlat()
        {
            return Elements.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public List<string> GetAllIds()
        {
            return Elements.SelectMany(x => x.GetAllIds()).Concat(new[] { Id }).ToList();
        }

        public BpmnPoolLane Copy()
        {
            BpmnPoolLane newPoolLane = new BpmnPoolLane(Id);
            newPoolLane.Elements = Elements.ConvertAll(x => x.Copy());
            return newPoolLane;
        }
    }
}
