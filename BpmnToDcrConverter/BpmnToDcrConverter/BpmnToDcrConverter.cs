using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;
using System.Transactions;

namespace BpmnToDcrConverter
{
    public static class BpmnToDcrConverter
    {
        public static DcrGraph ConvertBpmnToDcr(BpmnGraph bpmnGraph)
        {
            // Get nesting groups
            List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> nestingGroups = GetParallelGatewayNestingsAndSubsets(bpmnGraph);

            MakeBpmnStartEventsToActivities(bpmnGraph);

            HandleExclusiveGateways(bpmnGraph);
            HandleParallelGateways(bpmnGraph);

            RemoveAllEndEvents(bpmnGraph);

            Dictionary<string, DataType> variableToDataTypeDict = GetVariableToDataTypeDict(bpmnGraph);
            Dictionary<string, DataType> activityIdToDataType = GetActivityIdToDataTypeDict(variableToDataTypeDict, bpmnGraph);
            Dictionary<string, string> idToRoleDict = GetIdToRoleDict(bpmnGraph);

            // Create DCR activities
            List<DcrActivity> dcrActivities = MakeDcrActivities(bpmnGraph, activityIdToDataType, idToRoleDict);
            Dictionary<string, DcrActivity> idToDcrActivityDict = dcrActivities.ToDictionary(x => x.Id);

            // Create DCR sub-processes
            List<DcrSubProcess> dcrSubProcesses = MakeDcrSubProcesses(bpmnGraph);
            Dictionary<string, DcrSubProcess> idToDcrSubProcessDict = dcrSubProcesses.ToDictionary(x => x.Id);
            Dictionary<string, DcrSubProcess> idToNestedUnderSubProcessDict = GetSubProcessNestedElementsDict(bpmnGraph, idToDcrSubProcessDict);

            // Create DCR nestings
            Dictionary<string, DcrNesting> idToDcrNestingDict = MakeDcrNestings(nestingGroups, idToDcrActivityDict, idToDcrSubProcessDict);
            List<DcrNesting> dcrNestings = idToDcrNestingDict.Values.ToList();
            List<DcrFlowElement> nestedDcrElements = GetNestedDcrElements(idToDcrNestingDict);
            Dictionary<string, DcrNesting> idToNestedUnder = GetNestedUnderDict(dcrNestings);

            List<DcrFlowElement> allDcrFlowElements = dcrActivities.OfType<DcrFlowElement>().Concat(dcrSubProcesses.OfType<DcrFlowElement>()).Concat(dcrNestings.OfType<DcrFlowElement>()).ToList();
            Dictionary<string, DcrFlowElement> idToDcrFlowElementDict = allDcrFlowElements.ToDictionary(x => x.Id);
            NestSubProcessElements(idToNestedUnderSubProcessDict, allDcrFlowElements);

            AddArrowsToDcrElements(bpmnGraph, idToDcrFlowElementDict, nestingGroups);
            IncludeStartActivitiesAndSetThemToPending(bpmnGraph, idToDcrActivityDict);

            List<DcrFlowElement> topLevelElements = allDcrFlowElements.Where(x => !idToNestedUnderSubProcessDict.ContainsKey(x.Id) && !idToNestedUnder.ContainsKey(x.Id)).ToList();
            DcrGraph dcrGraph = new DcrGraph(topLevelElements);

            return dcrGraph;
        }

        private static List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> GetParallelGatewayNestingsAndSubsets(BpmnGraph bpmnGraph)
        {
            List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> nestingGroups = GetAllParallelGatewayNestingGroups(bpmnGraph);

            // Remove groups that does not point to anything
            nestingGroups = nestingGroups.Where(x => x.Item2.Count > 0).OrderBy(x => x.Item1.Count).ToList();

            // Remove potentially duplicate groups
            List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> noDuplicateNestingGroups = new List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)>();
            foreach (var group in nestingGroups)
            {
                bool found = false;
                foreach (var otherGroup in noDuplicateNestingGroups)
                {
                    if (IsListEqualOtherList(group.Item1, otherGroup.Item2))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    noDuplicateNestingGroups.Add(group);
                }
            }

            // Find subsets
            for (int i = 0; i < noDuplicateNestingGroups.Count; i++)
            {
                for (int j = i + 1; j < noDuplicateNestingGroups.Count; j++)
                {
                    if (IsListSubsetOfOtherList(noDuplicateNestingGroups[j].Item1, noDuplicateNestingGroups[i].Item1))
                    {
                        BpmnTemporaryNesting bpmnTemporaryNesting = new BpmnTemporaryNesting(noDuplicateNestingGroups[i].Item3, noDuplicateNestingGroups[i].Item1);

                        List<BpmnFlowElement> newList = noDuplicateNestingGroups[j].Item1.Except(FullyUnwrapTemporaryNestings(noDuplicateNestingGroups[i].Item1)).ToList();
                        newList.Add(bpmnTemporaryNesting);
                        noDuplicateNestingGroups[j] = (newList, noDuplicateNestingGroups[j].Item2, noDuplicateNestingGroups[j].Item3);

                        break;
                    }
                }
            }

            return noDuplicateNestingGroups;
        }

        private static Dictionary<string, DcrNesting> GetNestedUnderDict(List<DcrNesting> nestings)
        {
            Dictionary<string, DcrNesting> idToNestedUnder = new Dictionary<string, DcrNesting>();
            foreach (DcrNesting nesting in nestings)
            {
                foreach (DcrFlowElement element in nesting.Elements)
                {
                    if (!idToNestedUnder.ContainsKey(element.Id))
                    {
                        idToNestedUnder[element.Id] = nesting;
                    }
                }
            }

            return idToNestedUnder;
        }

        private static List<DcrFlowElement> GetNestedDcrElements(Dictionary<string, DcrNesting> idToDcrNestingDict)
        {
            List<DcrFlowElement> elements = idToDcrNestingDict.Values.Select(x => (DcrFlowElement)x).ToList();
            while (elements.Any(x => x is DcrNesting))
            {
                elements = elements.OfType<DcrNesting>().SelectMany(x => x.Elements).Concat(elements.Where(x => !(x is DcrNesting))).ToList();
            }

            return elements.Distinct().ToList();
        }

        private static Dictionary<string, DcrNesting> MakeDcrNestings(List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> nestingGroups, Dictionary<string, DcrActivity> idToDcrActivityDict, Dictionary<string, DcrSubProcess> idToDcrSubProcessDict)
        {
            Dictionary<string, DcrNesting> idToDcrNestingDict = new Dictionary<string, DcrNesting>();

            foreach (var group in nestingGroups)
            {
                List<DcrFlowElement> nestedElements = group.Item1.Select(x =>
                {
                    if (idToDcrActivityDict.ContainsKey(x.Id))
                    {
                        return (DcrFlowElement)idToDcrActivityDict[x.Id];
                    }

                    if (idToDcrSubProcessDict.ContainsKey(x.Id))
                    {
                        return (DcrFlowElement)idToDcrSubProcessDict[x.Id];
                    }

                    if (idToDcrNestingDict.ContainsKey(x.Id))
                    {
                        return (DcrFlowElement)idToDcrNestingDict[x.Id];
                    }

                    throw new Exception("");
                }).ToList();

                idToDcrNestingDict[group.Item3] = new DcrNesting(group.Item3, nestedElements);
            }

            return idToDcrNestingDict;
        }

        private static bool IsListSubsetOfOtherList(List<BpmnFlowElement> super, List<BpmnFlowElement> sub)
        {
            super = FullyUnwrapTemporaryNestings(super);
            sub = FullyUnwrapTemporaryNestings(sub);

            if (super.Count < sub.Count)
            {
                return false;
            }

            List<string> superIds = super.Select(x => x.Id).ToList();
            List<string> subIds = sub.Select(x => x.Id).ToList();
            List<string> removedList = superIds.Except(subIds).ToList();
            int expectedSubsetLength = superIds.Count - subIds.Count;

            if (removedList.Count < super.Count && removedList.Count > expectedSubsetLength)
            {
                throw new Exception("List was only a partial subset, this should not be possible.");
            }

            return removedList.Count == expectedSubsetLength;
        }

        private static List<BpmnFlowElement> FullyUnwrapTemporaryNestings(List<BpmnFlowElement> elements)
        {
            while (elements.Where(x => x is BpmnTemporaryNesting).Count() > 0)
            {
                elements = elements.OfType<BpmnTemporaryNesting>().SelectMany(x => x.FlowElements).Concat(elements.Where(x => !(x is BpmnTemporaryNesting))).ToList();
            }

            return elements;
        }

        private static bool IsListEqualOtherList(List<BpmnFlowElement> listA, List<BpmnFlowElement> listB)
        {
            List<string> listAIds = listA.Select(x => x.Id).ToList();
            List<string> listBIds = listB.Select(x => x.Id).ToList();

            return !listAIds.Except(listBIds).Any();
        }

        private static List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> GetAllParallelGatewayNestingGroups(BpmnGraph bpmnGraph)
        {
            List<BpmnParallelGateway> splittingParallelGateways = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnParallelGateway>().Where(x => x.OutgoingArrows.Count > 1).ToList();

            List<(List<BpmnFlowElement>, BpmnParallelGateway)> nestingGroups = new List<(List<BpmnFlowElement>, BpmnParallelGateway)>();
            foreach (BpmnParallelGateway gateway in splittingParallelGateways)
            {
                bpmnGraph.GetAllFlowElementsFlat().ForEach(x => x.Visited = false);

                nestingGroups.Add(GetParallelGatewayNestingGroup(gateway));
            }

            return nestingGroups.Select(x => (x.Item1, GetReachableActivities(bpmnGraph, x.Item2), Guid.NewGuid().ToString("N"))).ToList();
        }

        private static (List<BpmnFlowElement>, BpmnParallelGateway) GetParallelGatewayNestingGroup(BpmnParallelGateway gateway)
        {
            List<BpmnFlowElement> groupElements = new List<BpmnFlowElement>();
            BpmnParallelGateway endGateway = null;

            foreach (BpmnFlowArrow arrow in gateway.OutgoingArrows)
            {
                var (foundGroupElements, foundEndGateway) = RecursiveGetParallelNestingGroup(arrow.Element, 1);

                endGateway = foundEndGateway;
                groupElements.AddRange(foundGroupElements);
            }

            return (groupElements, endGateway);
        }

        private static (List<BpmnFlowElement>, BpmnParallelGateway) RecursiveGetParallelNestingGroup(BpmnFlowElement element, int nestingDepth)
        {
            if (element.Visited)
            {
                return (new List<BpmnFlowElement>(), null);
            }

            if (element is BpmnParallelGateway)
            {
                BpmnParallelGateway gateway = (BpmnParallelGateway)element;

                if (IsJoiningParallelGateway(gateway))
                {
                    nestingDepth--;
                }

                if (nestingDepth == 0)
                {
                    return (new List<BpmnFlowElement>(), gateway);
                }

                if (IsSplittingParallelGateway(gateway))
                {
                    nestingDepth++;
                }
            }

            element.Visited = true;

            List<BpmnFlowElement> groupingElements = new List<BpmnFlowElement>();
            BpmnParallelGateway finalGateway = null;

            if (element is BpmnActivity || element is BpmnSubProcess)
            {
                groupingElements.Add(element);
            }

            foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
            {
                var (arrowGroupingEvents, arrowFinalGateway) = RecursiveGetParallelNestingGroup(arrow.Element, nestingDepth);

                if (arrowFinalGateway != null)
                {
                    finalGateway = arrowFinalGateway;
                }

                groupingElements.AddRange(arrowGroupingEvents);
            }

            return (groupingElements, finalGateway);
        }

        private static bool IsSplittingParallelGateway(BpmnParallelGateway gateway)
        {
            return gateway.OutgoingArrows.Count > 1;
        }

        private static bool IsJoiningParallelGateway(BpmnParallelGateway gateway)
        {
            return gateway.IncomingArrows.Count > 1;
        }

        private static List<BpmnFlowElement> GetReachableActivities(BpmnGraph bpmnGraph, BpmnFlowElement element)
        {
            bpmnGraph.GetAllFlowElementsFlat().ForEach(x => x.Visited = false);
            return GetReachableActivitiesRecursive(element);
        }

        private static List<BpmnFlowElement> GetReachableActivitiesRecursive(BpmnFlowElement element)
        {
            if (element is BpmnActivity || element is BpmnSubProcess)
            {
                return new List<BpmnFlowElement> { element };
            }

            element.Visited = true;

            List<BpmnFlowElement> reachableElements = new List<BpmnFlowElement>();
            foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
            {
                reachableElements.AddRange(GetReachableActivitiesRecursive(arrow.Element));
            }

            return reachableElements;
        }

        private static void NestSubProcessElements(Dictionary<string, DcrSubProcess> idToNestedUnderSubProcessDict, List<DcrFlowElement> allDcrFlowElements)
        {
            foreach (DcrFlowElement flowElement in allDcrFlowElements)
            {
                if (idToNestedUnderSubProcessDict.ContainsKey(flowElement.Id))
                {
                    DcrSubProcess subProcess = idToNestedUnderSubProcessDict[flowElement.Id];
                    subProcess.Elements.Add(flowElement);
                }
            }
        }

        private static void RemoveAllEndEvents(BpmnGraph bpmnGraph)
        {
            List<BpmnEndEvent> endEvents = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnEndEvent>().ToList();
            foreach (BpmnEndEvent endEvent in endEvents)
            {
                bpmnGraph.DeleteElementFromId(endEvent.Id);
            }
        }

        private static void AddArrowsToDcrElements(BpmnGraph bpmnGraph, Dictionary<string, DcrFlowElement> idToDcrFlowElementDict, List<(List<BpmnFlowElement>, List<BpmnFlowElement>, string)> nestingGroups)
        {
            foreach (BpmnFlowElement bpmnElement in bpmnGraph.GetAllFlowElementsFlat())
            {
                DcrFlowElement dcrElement = idToDcrFlowElementDict[bpmnElement.Id];

                if (dcrElement is DcrActivity)
                {
                    Utilities.AddDcrArrow(dcrElement, dcrElement, DcrFlowArrowType.Exclude, "");
                }

                foreach (BpmnFlowArrow arrow in bpmnElement.OutgoingArrows)
                {
                    DcrFlowElement toDcrElement = idToDcrFlowElementDict[arrow.Element.Id];

                    if (toDcrElement is DcrActivity)
                    {
                        Utilities.AddDcrArrow(dcrElement, toDcrElement, DcrFlowArrowType.Response, arrow.Condition);
                        Utilities.AddDcrArrow(dcrElement, toDcrElement, DcrFlowArrowType.Include, arrow.Condition);
                    }

                    if (toDcrElement is DcrSubProcess)
                    {
                        DcrSubProcess dcrSubProcess = (DcrSubProcess)toDcrElement;
                        DcrFlowElement startActivity = idToDcrFlowElementDict[dcrSubProcess.StartActivityId];

                        Utilities.AddDcrArrow(dcrElement, dcrSubProcess, DcrFlowArrowType.Response, arrow.Condition);

                        Utilities.AddDcrArrow(dcrElement, startActivity, DcrFlowArrowType.Response, arrow.Condition);
                        Utilities.AddDcrArrow(dcrElement, startActivity, DcrFlowArrowType.Include, arrow.Condition);
                    }
                }
            }

            List<(DcrNesting, List<BpmnFlowElement>)> dcrNestingsAndTargets = nestingGroups.Select(x => ((DcrNesting)idToDcrFlowElementDict[x.Item3], x.Item2)).ToList();
            foreach (var nestingAndTarget in dcrNestingsAndTargets)
            {
                List<DcrFlowElement> targets = nestingAndTarget.Item2.Select(x => idToDcrFlowElementDict[x.Id]).ToList();

                foreach (DcrFlowElement target in targets)
                {
                    Utilities.AddDcrArrow(nestingAndTarget.Item1, target, DcrFlowArrowType.Milestone, "");
                }
            }
        }

        private static void IncludeStartActivitiesAndSetThemToPending(BpmnGraph bpmnGraph, Dictionary<string, DcrActivity> idToDcrActivityDict)
        {
            Dictionary<BpmnFlowElement, List<BpmnFlowElement>> pointingToThisElementDict = bpmnGraph.GetAllFlowElementsFlat().ToDictionary(x => x, x => new List<BpmnFlowElement>());
            foreach (BpmnFlowElement element in bpmnGraph.GetAllFlowElementsFlat())
            {
                foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
                {
                    pointingToThisElementDict[arrow.Element].Add(element);
                }
            }

            Dictionary<BpmnFlowElement, BpmnFlowElement> nestedUnderDict = new Dictionary<BpmnFlowElement, BpmnFlowElement>();
            foreach (BpmnSubProcess subProcess in bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnSubProcess>())
            {
                foreach (BpmnFlowElement nestedElement in subProcess.FlowElements)
                {
                    nestedUnderDict[nestedElement] = subProcess;
                }
            }

            List<BpmnFlowElement> startingActivities = pointingToThisElementDict.Where(x => !x.Value.Any()).Select(x => x.Key).ToList();
            foreach (BpmnFlowElement element in startingActivities)
            {
                DcrActivity activity = idToDcrActivityDict[element.Id];
                activity.Included = true;
                activity.Pending = true;

                if (nestedUnderDict.ContainsKey(element))
                {
                    activity.Included = false;
                }
            }
        }

        private static Dictionary<string, DcrSubProcess> GetSubProcessNestedElementsDict(BpmnGraph bpmnGraph, Dictionary<string, DcrSubProcess> idToDcrSubProcessDict)
        {
            Dictionary<string, DcrSubProcess> idToNestedUnderSubProcessDict = new Dictionary<string, DcrSubProcess>();
            List<BpmnSubProcess> bpmnSubProcesses = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnSubProcess>().ToList();
            foreach (BpmnSubProcess bpmnSubProcess in bpmnSubProcesses)
            {
                DcrSubProcess dcrSubProcess = idToDcrSubProcessDict[bpmnSubProcess.Id];

                List<string> ids = bpmnSubProcess.FlowElements.ConvertAll(x => x.Id);
                foreach (string id in ids)
                {
                    idToNestedUnderSubProcessDict[id] = dcrSubProcess;
                }
            }

            return idToNestedUnderSubProcessDict;
        }

        private static List<DcrSubProcess> MakeDcrSubProcesses(BpmnGraph bpmnGraph)
        {
            List<BpmnSubProcess> bpmnSubProcesses = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnSubProcess>().ToList();
            return bpmnSubProcesses.ConvertAll(x =>
            {
                return new DcrSubProcess(
                    x.Id,
                    "",
                    new List<DcrActivity>(),
                    true,
                    false,
                    false,
                    x.StartEventId
                );
            });
        }

        private static List<DcrActivity> MakeDcrActivities(BpmnGraph bpmnGraph, Dictionary<string, DataType> activityIdToDataType, Dictionary<string, string> idToRoleDict)
        {
            List<BpmnActivity> bpmnActivities = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnActivity>().ToList();
            List<DcrActivity> dcrActivities = bpmnActivities.ConvertAll(x =>
            {
                return new DcrActivity(
                    x.Id,
                    x.Name,
                    idToRoleDict[x.Id],
                    false,
                    false,
                    false,
                    activityIdToDataType[x.Id]
                );
            });
            return dcrActivities;
        }

        private static Dictionary<string, DataType> GetActivityIdToDataTypeDict(Dictionary<string, DataType> variableToDataTypeDict, BpmnGraph bpmnGraph)
        {
            List<BpmnActivity> bpmnActivities = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnActivity>().ToList();
            return bpmnActivities.Select(x => x.Id).ToDictionary(x => x, x =>
            {
                if (variableToDataTypeDict.ContainsKey(x))
                {
                    return variableToDataTypeDict[x];
                }

                return DataType.Unknown;
            });
        }

        private static Dictionary<string, string> GetIdToRoleDict(BpmnGraph bpmnGraph)
        {
            Dictionary<string, string> idToRoleDict = new Dictionary<string, string>();
            foreach (BpmnPool pool in bpmnGraph.GetPools())
            {
                foreach (BpmnPoolLane lane in pool.Lanes)
                {
                    string role = lane.Role;

                    List<string> laneElementIds = lane.GetFlowElementsFlat().ConvertAll(x => x.Id);
                    foreach (string id in laneElementIds)
                    {
                        idToRoleDict[id] = role;
                    }
                }
            }

            return idToRoleDict;
        }

        private static void HandleParallelGateways(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<BpmnParallelGateway> bpmnAnds = allBpmnElements.OfType<BpmnParallelGateway>().ToList();

            foreach (BpmnParallelGateway gateway in bpmnAnds)
            {
                // Handle combining gates first
                // - Remove conditions on all arrows comin from parallel gateways
                // - Skip all gatways by combining arrow conditions
                // - Parallel gateways are needed for determining milestone arrows, so maybe determine this first?

                // Check if parallel split
                // Find all elements in each path and the gate where they join
                // Somehow remember that these elements need to be nested and that the nesting should point to the elements pointed to by the parallel join
                // Remove parallel gateways

                MakeArrowsSkipBpmnElement(gateway);
            }

            RemoveAllParallelGateways(bpmnGraph);
        }

        private static void HandleExclusiveGateways(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<BpmnExclusiveGateway> bpmnXors = allBpmnElements.OfType<BpmnExclusiveGateway>().ToList();
            GiveConditionsToDefaultArrow(bpmnXors);
            SkipAllExclusiveGateways(bpmnXors);
            RemoveAllExclusiveGateways(bpmnGraph);
        }

        private static void RemoveAllParallelGateways(BpmnGraph bpmnGraph)
        {
            List<BpmnParallelGateway> gateways = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnParallelGateway>().ToList();
            foreach (BpmnParallelGateway gateway in gateways)
            {
                bpmnGraph.DeleteElementFromId(gateway.Id);
            }
        }

        private static void RemoveAllExclusiveGateways(BpmnGraph bpmnGraph)
        {
            List<BpmnExclusiveGateway> gateways = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnExclusiveGateway>().ToList();
            foreach (BpmnExclusiveGateway gateway in gateways)
            {
                bpmnGraph.DeleteElementFromId(gateway.Id);
            }
        }

        private static void SkipAllExclusiveGateways(List<BpmnExclusiveGateway> exclusiveGateways)
        {
            foreach (BpmnExclusiveGateway exclusiveGateway in exclusiveGateways)
            {
                MakeArrowsSkipBpmnElement(exclusiveGateway);
            }
        }

        private static void MakeArrowsSkipBpmnElement(BpmnFlowElement element)
        {
            foreach (BpmnFlowArrow incomingArrow in element.IncomingArrows)
            {
                foreach (BpmnFlowArrow outgoingArrow in element.OutgoingArrows)
                {
                    string condition = "";

                    if (incomingArrow.Condition != "" && outgoingArrow.Condition != "")
                    {
                        condition = $"({incomingArrow.Condition}) && ({outgoingArrow.Condition})";
                    }
                    else if (incomingArrow.Condition != "")
                    {
                        condition = incomingArrow.Condition;
                    }
                    else if (outgoingArrow.Condition != "")
                    {
                        condition = outgoingArrow.Condition;
                    }

                    Utilities.AddBpmnArrow(Guid.NewGuid().ToString("N"), incomingArrow.Element, outgoingArrow.Element, BpmnFlowArrowType.Sequence, condition);
                }
            }

            // Remove incoming arrows
            List<BpmnFlowArrow> incomingArrows = element.IncomingArrows.ToList();
            foreach (BpmnFlowArrow incomingArrow in incomingArrows)
            {
                BpmnFlowArrow arrow = incomingArrow.Element.OutgoingArrows.Where(x => x.Element == element).FirstOrDefault();
                Utilities.RemoveBpmnArrow(incomingArrow.Element, arrow);
            }

            // Remove outgoing arrows
            List<BpmnFlowArrow> outgoingArrows = element.OutgoingArrows.ToList();
            foreach (BpmnFlowArrow outgoingArrow in outgoingArrows)
            {
                Utilities.RemoveBpmnArrow(element, outgoingArrow);
            }
        }

        private static void GiveConditionsToDefaultArrow(List<BpmnExclusiveGateway> bpmnXors)
        {
            foreach (BpmnExclusiveGateway bpmnXor in bpmnXors)
            {
                if (bpmnXor.DefaultPath == "")
                {
                    continue;
                }

                BpmnFlowArrow defaultArrow = bpmnXor.OutgoingArrows.Where(x => x.Id == bpmnXor.DefaultPath).FirstOrDefault();
                List<BpmnFlowArrow> notDefaultArrows = bpmnXor.OutgoingArrows.Where(x => x.Id != bpmnXor.DefaultPath).ToList();

                List<string> conditions = notDefaultArrows.ConvertAll(x => x.Condition);
                List<string> conditionsPrep = conditions.ConvertAll(x => $"(!({x}))");
                string newCondition = string.Join(" && ", conditionsPrep);

                defaultArrow.Condition = newCondition;
            }
        }

        private static void MakeBpmnStartEventsToActivities(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<BpmnStartEvent> startEvents = allBpmnElements.OfType<BpmnStartEvent>().ToList();
            foreach (BpmnStartEvent startEvent in startEvents)
            {
                BpmnActivity activity = new BpmnActivity(startEvent.Id, "Start");

                BpmnFlowArrow arrow = startEvent.OutgoingArrows.FirstOrDefault();
                Utilities.AddBpmnArrow(arrow.Id, activity, arrow.Element, BpmnFlowArrowType.Sequence, arrow.Condition);
                Utilities.RemoveBpmnArrow(startEvent, arrow);

                List<BpmnFlowElement> collection = bpmnGraph.GetElementCollectionFromId(startEvent.Id);
                collection.Add(activity);
                collection.Remove(startEvent);
            }
        }

        private static List<RelationalOperation> GetRelationalExpressionsFromExpression(Expression expression)
        {
            if (expression is RelationalOperation)
            {
                return new List<RelationalOperation> { (RelationalOperation)expression };
            }

            if (expression is BinaryLogicalOperation)
            {
                BinaryLogicalOperation operation = (BinaryLogicalOperation)expression;

                return GetRelationalExpressionsFromExpression(operation.Left).Concat(GetRelationalExpressionsFromExpression(operation.Right)).ToList();
            }

            if (expression is UnaryLogicalOperation)
            {
                UnaryLogicalOperation operation = (UnaryLogicalOperation)expression;

                return GetRelationalExpressionsFromExpression(operation.Expression);
            }

            throw new Exception("Unhandled case.");
        }

        private static Dictionary<string, DataType> GetVariableToDataTypeDict(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<string> allArrowConditions = allBpmnElements.SelectMany(x => x.OutgoingArrows).Select(x => x.Condition).Where(x => x != "").ToList();
            List<Expression> allArrowExpressions = allArrowConditions.Select(x => LogicParser.ConditionParser.Parse(x)).ToList();
            List<RelationalOperation> allRelationalExpressions = allArrowExpressions.SelectMany(x => GetRelationalExpressionsFromExpression(x)).ToList();
            List<string> allVariables = allRelationalExpressions.SelectMany(x => x.GetVariableNames()).Distinct().ToList();

            CheckForConstantlyEvaluablePartsOfExpressions(allArrowExpressions);

            Dictionary<string, DataType> variableDataTypeDict = allVariables.ToDictionary(x => x, x => DataType.Unknown);
            GiveTypesToVariablesComparedToConstants(allRelationalExpressions, variableDataTypeDict);
            GiveTypesToVariablesComparedToVariables(allRelationalExpressions, variableDataTypeDict);
            CheckUnknownOrInconsistentTypes(allVariables, allRelationalExpressions, variableDataTypeDict);

            return variableDataTypeDict;
        }

        private static void CheckForConstantlyEvaluablePartsOfExpressions(List<Expression> allArrowExpressions)
        {
            foreach (Expression exp in allArrowExpressions)
            {
                List<Expression> constantlyEvaluableSubExpressions = exp.GetAllConstantlyEvaluableSubExpressions();

                foreach (Expression subExpression in constantlyEvaluableSubExpressions)
                {
                    string wholeStr = exp.GetString();
                    string subStr = subExpression.GetString();
                    string res = subExpression.Evaluate().ToString().ToLower();

                    if (wholeStr == subStr)
                    {
                        Console.WriteLine($"Warning: {subStr} will always evaluate to {res}.");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: {subStr} will always evaluate to {res} in {wholeStr}.");
                    }
                }
            }
        }

        private static void CheckUnknownOrInconsistentTypes(List<string> allVariables, List<RelationalOperation> relations, Dictionary<string, DataType> variableDataTypeDict)
        {
            foreach (string variable in allVariables)
            {
                if (variableDataTypeDict[variable] == DataType.Unknown)
                {
                    Console.WriteLine($"Could not determine the data type of variable {variable}.");
                }
            }

            foreach (RelationalOperation relation in relations)
            {
                string leftName = relation.Left.GetString();
                string rightName = relation.Right.GetString();

                DataType left = relation.Left.GetDataType(variableDataTypeDict);
                DataType right = relation.Right.GetDataType(variableDataTypeDict);

                if (left != right)
                {
                    throw new Exception($"Type mismatch between \"{leftName}\" and \"{rightName}\". They have types \"{left}\" and \"{right}\" respectively.");
                }
            }
        }

        private static void GiveTypesToVariablesComparedToVariables(List<RelationalOperation> relations, Dictionary<string, DataType> variableDataTypeDict)
        {
            List<RelationalOperation> relationsWithUnknown = relations.Where(x => x.ContainsKnownAndUnknownVariables(variableDataTypeDict)).ToList();
            while (relationsWithUnknown.Any())
            {
                foreach (RelationalOperation relation in relationsWithUnknown)
                {
                    Variable leftVariable = (Variable)relation.Left;
                    Variable rightVariable = (Variable)relation.Right;

                    if (variableDataTypeDict[leftVariable.Name] == DataType.Unknown)
                    {
                        variableDataTypeDict[leftVariable.Name] = variableDataTypeDict[rightVariable.Name];
                    }
                    else
                    {
                        variableDataTypeDict[rightVariable.Name] = variableDataTypeDict[leftVariable.Name];
                    }
                }

                relationsWithUnknown = relations.Where(x => x.ContainsKnownAndUnknownVariables(variableDataTypeDict)).ToList();
            }
        }

        private static void GiveTypesToVariablesComparedToConstants(List<RelationalOperation> relations, Dictionary<string, DataType> variableDataTypeDict)
        {
            List<RelationalOperation> relationsWithConstants = relations.Where(x => x.ContainsConstant()).ToList();
            foreach (RelationalOperation relation in relationsWithConstants)
            {
                if (relation.PurelyConstant())
                {
                    continue;
                }

                Variable var;
                Constant constant;

                if (relation.Left.IsConstant())
                {
                    constant = (Constant)relation.Left;
                    var = (Variable)relation.Right;
                }
                else
                {
                    constant = (Constant)relation.Right;
                    var = (Variable)relation.Left;
                }

                variableDataTypeDict[var.Name] = constant.GetDataType(variableDataTypeDict);
            }
        }
    }
}
