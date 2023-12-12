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

            List<string> currentRoles = _pools.SelectMany(x => x.GetRoles()).Where(x => x != "").Select(x => x.ToLower()).ToList();
            List<string> newRoles = newPools.SelectMany(x => x.GetRoles()).Where(x => x != "").Select(x => x.ToLower()).ToList();
            List<string> allRoles = currentRoles.Concat(newRoles).ToList();

            List<string> duplicateIds = allIds.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new BpmnDuplicateIdException($"Multiple elements have the ids \"{exceptionString}\".");
            }

            List<string> duplicateRoles = allRoles.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateRoles.Any())
            {
                string exceptionString = string.Join(", ", duplicateRoles);
                throw new BpmnDuplicateIdException($"Multiple lanes have the roles \"{exceptionString}\".");
            }

            _pools = _pools.Concat(newPools).ToList();
        }

        public void AddPool(BpmnPool pool)
        {
            AddPools(new[] { pool });
        }

        public List<BpmnPool> GetPools()
        {
            return _pools;
        }

        public List<BpmnFlowElement> GetFirstLayerFlowElements()
        {
            return _pools.SelectMany(x => x.Lanes).SelectMany(x => x.Elements).ToList();
        }

        public List<BpmnFlowElement> GetAllFlowElementsFlat()
        {
            return _pools.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public List<string> GetAllIds()
        {
            return _pools.SelectMany(x => x.GetAllIds()).Concat(new[] { Id }).ToList();
        }

        public void TestGraphValidity()
        {
            foreach (BpmnFlowElement element in GetAllFlowElementsFlat())
            {
                element.TestValidity();
                TestValidArrowReferences(element);

                if (element is BpmnExclusiveGateway)
                {
                    TestXorGatewayArrows((BpmnExclusiveGateway)element);
                }
            }
        }

        private void TestXorGatewayArrows(BpmnExclusiveGateway bpmnXor)
        {
            if (bpmnXor.DefaultPath != "")
            {
                List<BpmnFlowArrow> defaultPaths = bpmnXor.OutgoingArrows.Where(x => x.Id == bpmnXor.DefaultPath).ToList();

                if (defaultPaths.Count == 0)
                {
                    throw new Exception($"BPMN exclusive gateway is marked as having a default path {bpmnXor.DefaultPath}, but his does not exist amongst its arrows.");
                }

                if (defaultPaths.Count > 1)
                {
                    throw new Exception($"BPMN exclusive gateway is marked as having a default path {bpmnXor.DefaultPath}, but multiple paths were found with this ID.");
                }
            }

            List<string> duplicateArrowIds = bpmnXor.OutgoingArrows.Select(x => x.Id).GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateArrowIds.Any())
            {
                string duplicateArrowIdsString = string.Join(",", duplicateArrowIds);
                throw new Exception($"BPMN exclusive gatewith with ID {bpmnXor.Id} has multiple arrows with ids \"{duplicateArrowIdsString}\".");
            }

            List<BpmnFlowArrow> notDefaultArrows = bpmnXor.OutgoingArrows.Where(x => x.Id != bpmnXor.DefaultPath).ToList();
            List<BpmnFlowArrow> notDefaultArrowsWithoutCondition = notDefaultArrows.Where(x => x.Condition == "").ToList();
            if (notDefaultArrowsWithoutCondition.Count > 1 && notDefaultArrowsWithoutCondition.Any())
            {
                string errorString = string.Join(",", notDefaultArrowsWithoutCondition.Select(x => x.Id));
                throw new Exception($"BPMN exclusive gatewith with ID {bpmnXor.Id} has arrows with IDs \"{errorString}\" without conditions");
            }
        }

        private void TestValidArrowReferences(BpmnFlowElement element)
        {
            List<BpmnFlowArrow> allArrows = element.OutgoingArrows.Concat(element.IncomingArrows).ToList();
            HashSet<BpmnFlowElement> allElements = GetAllFlowElementsFlat().ToHashSet();

            foreach (BpmnFlowArrow arrow in allArrows)
            {
                if (!allElements.Contains(arrow.Element))
                {
                    throw new BpmnInvalidArrowException($"BPMN flow element with id \"{element.Id}\" has a reference to a flow element that isn't in the graph.");
                }
            }
        }

        public void AddArrow(string id, BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to, string condition)
        {
            from.OutgoingArrows.Add(new BpmnFlowArrow(id, type, to, condition));
            to.IncomingArrows.Add(new BpmnFlowArrow(id, type, from, condition));
        }

        public void AddArrow(BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to, string condition)
        {
            AddArrow(Guid.NewGuid().ToString("N"), type, from, to, condition);
        }

        public void AddArrow(string id, BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to)
        {
            AddArrow(id, type, from, to, "");
        }

        public void AddArrow(BpmnFlowArrowType type, BpmnFlowElement from, BpmnFlowElement to)
        {
            AddArrow(Guid.NewGuid().ToString("N"), type, from, to, "");
        }

        public BpmnFlowElement GetFlowElementFromId(string id)
        {
            List<BpmnFlowElement> allFlowElements = GetAllFlowElementsFlat();
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

        public void DeleteElementFromId(string id)
        {
            foreach (BpmnPool pool in _pools)
            {
                pool.DeleteElementFromId(id);
            }
        }
    }

    public class BpmnPool
    {
        public string Id;
        public string Name;
        public List<BpmnPoolLane> Lanes = new List<BpmnPoolLane>();

        public BpmnPool(string id, string name, IEnumerable<BpmnPoolLane> lanes)
        {
            Id = id;
            Name = name;
            Lanes = lanes.ToList();
        }

        public BpmnPool(string id) : this (id, "", new List<BpmnPoolLane>()) { }

        public BpmnPool(string id, string name) : this(id, name, new List<BpmnPoolLane>()) { }

        public BpmnPool() : this(Guid.NewGuid().ToString("N"), "", new List<BpmnPoolLane>()) { }

        public BpmnPool(string id, IEnumerable<BpmnPoolLane> lanes) : this(id, "", lanes) { }

        public BpmnPool(IEnumerable<BpmnPoolLane> lanes) : this(Guid.NewGuid().ToString("N"), "", lanes) { }

        public BpmnPool(BpmnPoolLane lane) : this(Guid.NewGuid().ToString("N"), "", new[] { lane }) { }

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

        public void DeleteElementFromId(string id)
        {
            foreach (BpmnPoolLane lane in Lanes)
            {
                lane.DeleteElementFromId(id);
            }
        }

        public IEnumerable<string> GetRoles()
        {
            return Lanes.Select(x => x.Role);
        }
    }

    public class BpmnPoolLane
    {
        public string Id;
        public string Role;
        public List<BpmnFlowElement> Elements = new List<BpmnFlowElement>();

        public BpmnPoolLane(string id, string role, IEnumerable<BpmnFlowElement> flowElements)
        {
            Id = id;
            Role = role;
            Elements = flowElements.ToList();
        }

        public BpmnPoolLane(string id) : this(id, "", new List<BpmnFlowElement>()) { }

        public BpmnPoolLane(string id, string role) : this(id, role, new List<BpmnFlowElement>()) { }

        public BpmnPoolLane() : this(Guid.NewGuid().ToString("N"), "", new List<BpmnFlowElement>()) { }

        public BpmnPoolLane(string id, IEnumerable<BpmnFlowElement> flowElements) : this(id, "", flowElements) { }

        public BpmnPoolLane(IEnumerable<BpmnFlowElement> flowElements) : this(Guid.NewGuid().ToString("N"), "", flowElements) { }

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

            List<List<BpmnFlowElement>> res = Elements.Select(x => x.GetElementCollectionFromId(id)).ToList();
            foreach (List<BpmnFlowElement> collection in res)
            {
                if (collection != null)
                {
                    return collection;
                }
            }

            return null;
        }

        public void DeleteElementFromId(string id)
        {
            Utilities.RemoveIdFromBpmnCollection(id, Elements);
        }
    }
}
