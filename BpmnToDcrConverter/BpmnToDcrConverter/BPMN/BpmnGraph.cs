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

        public List<BpmnFlowElement> GetElementCollectionFromId(string id)
        {
            List<List<BpmnFlowElement>> results = _pools.ConvertAll(x => x.GetElementCollectionFromId(id));
            foreach (List<BpmnFlowElement> lst in results)
            {
                if (lst != null)
                {
                    return lst;
                }
            }

            return null;
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

        public List<BpmnFlowElement> GetElementCollectionFromId(string id)
        {
            List<List<BpmnFlowElement>> results = Lanes.ConvertAll(x => x.GetElementCollectionFromId(id));
            foreach (List<BpmnFlowElement> lst in results)
            {
                if (lst != null)
                {
                    return lst;
                }
            }

            return null;
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

        public List<BpmnFlowElement> GetElementCollectionFromId(string id)
        {
            if (Elements.Select(x => x.Id).Contains(id))
            {
                return Elements;
            }

            return null;
        }
    }
}
