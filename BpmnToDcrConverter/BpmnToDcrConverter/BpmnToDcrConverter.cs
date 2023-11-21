using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Dcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;

namespace BpmnToDcrConverter
{
    public static class BpmnToDcrConverter
    {
        public static DcrGraph ConvertBpmnToDcr(BpmnGraph bpmnGraph)
        {
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

            // Subprocesses
            List<DcrSubProcess> dcrSubProcesses = MakeDcrSubProcesses(bpmnGraph);
            Dictionary<string, DcrSubProcess> idToDcrSubProcessDict = dcrSubProcesses.ToDictionary(x => x.Id);
            Dictionary<string, DcrSubProcess> idToNestedUnderSubProcessDict = GetSubProcessNestedElementsDict(bpmnGraph, idToDcrSubProcessDict);

            List<DcrFlowElement> allDcrFlowElements = dcrActivities.OfType<DcrFlowElement>().Concat(dcrSubProcesses.OfType<DcrFlowElement>()).ToList();
            Dictionary<string, DcrFlowElement> idToDcrFlowElementDict = allDcrFlowElements.ToDictionary(x => x.Id);
            NestSubProcessElements(idToNestedUnderSubProcessDict, allDcrFlowElements);

            AddArrowsToDcrElements(bpmnGraph, idToDcrFlowElementDict);
            IncludeStartActivitiesAndSetThemToPending(bpmnGraph, idToDcrActivityDict);

            List<DcrFlowElement> firstLevelElements = allDcrFlowElements.Where(x => !idToNestedUnderSubProcessDict.ContainsKey(x.Id)).ToList();
            DcrGraph dcrGraph = new DcrGraph(firstLevelElements);

            return dcrGraph;
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

        private static void AddArrowsToDcrElements(BpmnGraph bpmnGraph, Dictionary<string, DcrFlowElement> idToDcrFlowElementDict)
        {
            foreach (BpmnFlowElement bpmnElement in bpmnGraph.GetAllFlowElementsFlat())
            {
                DcrFlowElement dcrElement = idToDcrFlowElementDict[bpmnElement.Id];

                Utilities.AddDcrArrow(dcrElement, dcrElement, DcrFlowArrowType.Exclude, "");

                foreach (BpmnFlowArrow arrow in bpmnElement.OutgoingArrows)
                {
                    DcrFlowElement toDcrElement = idToDcrFlowElementDict[arrow.Element.Id];
                    Utilities.AddDcrArrow(dcrElement, toDcrElement, DcrFlowArrowType.Response, arrow.Condition);
                    Utilities.AddDcrArrow(dcrElement, toDcrElement, DcrFlowArrowType.Include, arrow.Condition);
                }
            }
        }

        private static void IncludeStartActivitiesAndSetThemToPending(BpmnGraph bpmnGraph, Dictionary<string, DcrActivity> idToDcrActivityDict)
        {
            Dictionary<BpmnFlowElement, List<BpmnFlowElement>> pointingToDict = bpmnGraph.GetAllFlowElementsFlat().ToDictionary(x => x, x => new List<BpmnFlowElement>());
            foreach (BpmnFlowElement element in bpmnGraph.GetAllFlowElementsFlat())
            {
                foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
                {
                    pointingToDict[arrow.Element].Add(element);
                }
            }

            List<BpmnFlowElement> startingActivities = pointingToDict.Where(x => !x.Value.Any()).Select(x => x.Key).ToList();
            foreach (BpmnFlowElement element in startingActivities)
            {
                DcrActivity activity = idToDcrActivityDict[element.Id];
                activity.Included = true;
                activity.Pending = true;
            }
        }

        private static Dictionary<string, DcrSubProcess> GetSubProcessNestedElementsDict(BpmnGraph bpmnGraph, Dictionary<string, DcrSubProcess> idToDcrSubProcessDict)
        {
            Dictionary<string, DcrSubProcess> idToNestedUnderSubProcessDict = new Dictionary<string, DcrSubProcess>();
            List<BpmnSubProcess> bpmnSubProcesses = bpmnGraph.GetAllFlowElementsFlat().OfType<BpmnSubProcess>().ToList();
            foreach (BpmnSubProcess bpmnSubProcess in bpmnSubProcesses)
            {
                DcrSubProcess dcrSubProcess = idToDcrSubProcessDict[bpmnSubProcess.Id];

                List<string> ids = bpmnSubProcess.flowElements.ConvertAll(x => x.Id);
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
                    false,
                    false,
                    false
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
                MakeArrowsSkipBpmnElement(gateway);
            }

            RemoveAllParallelGateways(bpmnGraph);
        }

        private static void HandleExclusiveGateways(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<BpmnExclusiveGateway> bpmnXors = allBpmnElements.OfType<BpmnExclusiveGateway>().ToList();
            GiveConditionsToArrowsWithoutOne(bpmnXors);
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

                    Utilities.AddBpmnArrow(incomingArrow.Element, outgoingArrow.Element, BpmnFlowArrowType.Sequence, condition);
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

        private static void GiveConditionsToArrowsWithoutOne(List<BpmnExclusiveGateway> bpmnXors)
        {
            foreach (BpmnExclusiveGateway bpmnXor in bpmnXors)
            {
                BpmnFlowArrow arrowWithoutCondition = bpmnXor.OutgoingArrows.Where(x => x.Condition == "").FirstOrDefault();
                List<BpmnFlowArrow> otherArrows = bpmnXor.OutgoingArrows.Where(x => x != arrowWithoutCondition).ToList();

                if (arrowWithoutCondition != null)
                {
                    List<string> otherConditions = otherArrows.ConvertAll(x => x.Condition);
                    List<string> otherConditionsPrep = otherConditions.ConvertAll(x => $"(!({x}))");
                    string newCondition = string.Join(" && ", otherConditionsPrep);

                    arrowWithoutCondition.Condition = newCondition;
                }
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
                Utilities.AddBpmnArrow(activity, arrow.Element, BpmnFlowArrowType.Sequence, arrow.Condition);
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
