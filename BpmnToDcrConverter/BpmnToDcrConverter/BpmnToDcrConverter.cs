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
            List<Tuple<DcrFlowElement, DcrFlowElement, string>> arrowsToAdd = new List<Tuple<DcrFlowElement, DcrFlowElement, string>>();

            MakeBpmnStartEventsToActivities(bpmnGraph);
            Dictionary<string, DataType> variableToDataTypeDict = GetVariableDataTypesDict(bpmnGraph);
            HandleExclusiveGateways(bpmnGraph);
            HandleParallelGateways(bpmnGraph);

            // Convert to DCR activities and add arrows
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<BpmnActivity> bpmnActivities = allBpmnElements.OfType<BpmnActivity>().ToList();
            List<BpmnSubProcess> bpmnSubProcesses = allBpmnElements.OfType<BpmnSubProcess>().ToList();

            // Add roles
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

            Dictionary<string, DataType> activityIdToDataType = bpmnActivities.Select(x => x.Id).ToDictionary(x => x, x =>
            {
                if (variableToDataTypeDict.ContainsKey(x))
                {
                    return variableToDataTypeDict[x];
                }

                return DataType.Unknown;
            });

            List<DcrActivity> dcrActivities = bpmnActivities.ConvertAll(x => new DcrActivity(x.Id, x.Name, idToRoleDict[x.Id], false, false, false, activityIdToDataType[x.Id]));
            Dictionary<string, DcrActivity> idToDcrActivityDict = dcrActivities.ToDictionary(x => x.Id);

            foreach (BpmnFlowElement element in bpmnActivities)
            {
                DcrActivity dcrActivity = idToDcrActivityDict[element.Id];

                Utilities.AddDcrArrow(dcrActivity, dcrActivity, DcrFlowArrowType.Exclude, "");

                foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
                {
                    if (!idToDcrActivityDict.ContainsKey(arrow.Element.Id))
                    {
                        continue;
                    }

                    DcrActivity toDcrActivity = idToDcrActivityDict[arrow.Element.Id];

                    arrowsToAdd.Add(new Tuple<DcrFlowElement, DcrFlowElement, string>(dcrActivity, toDcrActivity, arrow.Condition));
                }
            }

            // Subprocesses
            List<DcrSubProcess> dcrSubProcesses = bpmnSubProcesses.ConvertAll(x => new DcrSubProcess(x.Id, "", new List<DcrActivity>(), false, false, false));

            Dictionary<string, DcrSubProcess> idToSubprocessDict = new Dictionary<string, DcrSubProcess>();
            foreach (BpmnSubProcess subProcess in bpmnSubProcesses)
            {
                DcrSubProcess dcrSubProcess = dcrSubProcesses.Where(x => x.Id == subProcess.Id).FirstOrDefault();

                List<string> ids = subProcess.flowElements.ConvertAll(x => x.Id);
                foreach (string id in ids)
                {
                    idToSubprocessDict[id] = dcrSubProcess;
                }
            }

            List<DcrFlowElement> allDcrFlowElements = dcrActivities.Select(x => (DcrFlowElement)x).Concat(dcrSubProcesses.Select(x => (DcrFlowElement)x)).ToList();
            foreach (DcrFlowElement flowElement in allDcrFlowElements)
            {
                if (idToSubprocessDict.ContainsKey(flowElement.Id))
                {
                    DcrSubProcess subProcess = idToSubprocessDict[flowElement.Id];
                    subProcess.Elements.Add(flowElement);
                }
            }

            foreach (BpmnFlowElement bpmnElement in allBpmnElements)
            {
                if (!(bpmnElement is BpmnActivity))
                {
                    continue;
                }

                foreach (BpmnFlowArrow arrow in bpmnElement.OutgoingArrows)
                {
                    if (arrow.Element is BpmnSubProcess)
                    {
                        DcrFlowElement dcrElement = allDcrFlowElements.Where(x => x.Id == bpmnElement.Id).FirstOrDefault();
                        DcrFlowElement dcrSubProcess = allDcrFlowElements.Where(x => x.Id == arrow.Element.Id).FirstOrDefault();

                        arrowsToAdd.Add(new Tuple<DcrFlowElement, DcrFlowElement, string>(dcrElement, dcrSubProcess, arrow.Condition));
                    }
                }
            }

            foreach (var arrowToAdd in arrowsToAdd)
            {
                Utilities.AddDcrArrow(arrowToAdd.Item1, arrowToAdd.Item2, DcrFlowArrowType.Response, arrowToAdd.Item3);
                Utilities.AddDcrArrow(arrowToAdd.Item1, arrowToAdd.Item2, DcrFlowArrowType.Include, arrowToAdd.Item3);
            }

            // Include start activities and set them to pending
            Dictionary<BpmnFlowElement, List<BpmnFlowElement>> pointingToDict = bpmnGraph.GetAllFlowElementsFlat().ToDictionary(x => x, x => new List<BpmnFlowElement>());
            foreach (BpmnFlowElement element in bpmnGraph.GetAllFlowElementsFlat())
            {
                foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
                {
                    pointingToDict[arrow.Element].Add(element);
                }
            }

            List<BpmnFlowElement> startingActivities = pointingToDict.Where(x => !x.Value.Any()).Select(x => x.Key).Where(x => x is BpmnActivity).ToList();
            foreach (BpmnFlowElement element in startingActivities)
            {
                DcrActivity activity = idToDcrActivityDict[element.Id];
                activity.Included = true;
                activity.Pending = true;
            }

            List<DcrFlowElement> firstLevelElements = allDcrFlowElements.Where(x => !idToSubprocessDict.ContainsKey(x.Id)).ToList();
            DcrGraph dcrGraph = new DcrGraph(firstLevelElements);

            return dcrGraph;
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
                        condition = $"{incomingArrow.Condition} && {outgoingArrow.Condition}";
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
                    List<string> otherConditionsPrep = otherConditions.ConvertAll(x => $"!({x})");
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
            if (expression is Term)
            {
                return new List<RelationalOperation>();
            }

            if (expression is RelationalOperation)
            {
                return new List<RelationalOperation> { (RelationalOperation)expression };
            }

            if (expression is LogicalOperation)
            {
                LogicalOperation operation = (LogicalOperation)expression;

                return GetRelationalExpressionsFromExpression(operation.Left).Concat(GetRelationalExpressionsFromExpression(operation.Right)).ToList();
            }

            throw new Exception("Unhandled case.");
        }

        private static Dictionary<string, DataType> GetVariableDataTypesDict(BpmnGraph bpmnGraph)
        {
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            List<string> allArrowConditions = allBpmnElements.SelectMany(x => x.OutgoingArrows).Select(x => x.Condition).Where(x => x != "").ToList();
            List<Expression> allArrowExpressions = allArrowConditions.Select(x => LogicParser.LogicalExpressionParser.Parse(x)).ToList();
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
