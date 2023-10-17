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
            List<BpmnFlowElement> allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();

            List<BpmnActivity> bpmnActivities = allBpmnElements.Where(x => x is BpmnActivity)
                                                               .Select(x => (BpmnActivity)x)
                                                               .ToList();

            List<BpmnExclusiveGateway> bpmnXors = allBpmnElements.Where(x => x is BpmnExclusiveGateway)
                                                                 .Select(x => (BpmnExclusiveGateway)x)
                                                                 .ToList();

            // Get variables and their types
            List<string> allArrowConditions = allBpmnElements.SelectMany(x => x.OutgoingArrows).Select(x => x.Condition).Where(x => x != "").ToList();
            List<Expression> allArrowExpressions = allArrowConditions.Select(x => LogicParser.LogicalExpressionParser.Parse(x)).ToList();
            List<RelationalOperation> allRelationalExpressions = allArrowExpressions.SelectMany(x => GetRelationalExpressionsFromExpression(x)).ToList();
            List<string> allVariables = allRelationalExpressions.SelectMany(x => x.GetVariableNames()).ToList();

            foreach (Expression exp in allArrowExpressions)
            {
                List<Expression> purelyConstantSubExpressions = exp.GetAllConstantlyEvaluableSubExpressions();

                foreach (Expression subExpression in purelyConstantSubExpressions)
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

            Dictionary<string, DataType> variableToDataTypeDict = GetVariableDataTypesDict(allVariables, allRelationalExpressions);


            // Give conditions to arrows from XOR without one (inverted of all the other conditions)
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

            // Dechain XORs
            foreach (BpmnExclusiveGateway xor in bpmnXors)
            {
                DeChainExclusiveGateway(xor);
            }

            // Convert start events to activities
            List<BpmnStartEvent> startEvents = allBpmnElements.Where(x => x is BpmnStartEvent).Select(x => (BpmnStartEvent)x).ToList();
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

            allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();

            // Turn XOR arrows into direct arrows
            List<BpmnFlowElement> bpmnElementsPointingToXors = allBpmnElements.Where(x => x.OutgoingArrows.Any(y => y.Element is BpmnExclusiveGateway)).ToList();
            foreach (BpmnFlowElement element in bpmnElementsPointingToXors)
            {
                foreach (BpmnFlowArrow arrow in element.OutgoingArrows.ToList())
                {
                    if (!(arrow.Element is BpmnExclusiveGateway))
                    {
                        continue;
                    }

                    BpmnExclusiveGateway xor = (BpmnExclusiveGateway)arrow.Element;
                    foreach (BpmnFlowArrow nestedArrow in xor.OutgoingArrows.ToList())
                    {
                        Utilities.AddBpmnArrow(element, nestedArrow.Element, BpmnFlowArrowType.Sequence, nestedArrow.Condition);
                    }

                    Utilities.RemoveBpmnArrow(element, arrow);
                }
            }

            // Turn AND arrows into direct arrows
            List<BpmnFlowElement> bpmnElementsPointingToAnds = allBpmnElements.Where(x => x.OutgoingArrows.Any(y => y.Element is BpmnParallelGateway)).ToList();
            foreach (BpmnFlowElement element in bpmnElementsPointingToAnds)
            {
                foreach (BpmnFlowArrow arrow in element.OutgoingArrows)
                {
                    if (!(arrow.Element is BpmnParallelGateway))
                    {
                        continue;
                    }

                    BpmnParallelGateway and = (BpmnParallelGateway)arrow.Element;
                    foreach (BpmnFlowArrow nestedArrow in and.OutgoingArrows)
                    {
                        Utilities.AddBpmnArrow(element, nestedArrow.Element, BpmnFlowArrowType.Sequence, "");
                    }

                    Utilities.RemoveBpmnArrow(element, arrow);
                }
            }

            // Convert to DCR activities and add arrows
            allBpmnElements = bpmnGraph.GetAllFlowElementsFlat();
            bpmnActivities = allBpmnElements.Where(x => x is BpmnActivity)
                                            .Select(x => (BpmnActivity)x)
                                            .ToList();
            List<BpmnSubProcess> bpmnSubProcesses = allBpmnElements.Where(x => x is BpmnSubProcess)
                                                               .Select(x => (BpmnSubProcess)x)
                                                               .ToList();

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

                    Utilities.AddDcrArrow(dcrActivity, toDcrActivity, DcrFlowArrowType.Response, arrow.Condition);
                    Utilities.AddDcrArrow(dcrActivity, toDcrActivity, DcrFlowArrowType.Include, arrow.Condition);
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

                        Utilities.AddDcrArrow(dcrElement, dcrSubProcess, DcrFlowArrowType.Response, arrow.Condition);
                        Utilities.AddDcrArrow(dcrElement, dcrSubProcess, DcrFlowArrowType.Include, arrow.Condition);
                    }
                }
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

        private static void DeChainExclusiveGateway(BpmnExclusiveGateway bpmnExclusiveGateway)
        {
            foreach (BpmnFlowArrow arrow in bpmnExclusiveGateway.OutgoingArrows.ToList())
            {
                if (!(arrow.Element is BpmnExclusiveGateway))
                {
                    continue;
                }

                BpmnExclusiveGateway xor = (BpmnExclusiveGateway)arrow.Element;
                DeChainExclusiveGateway(xor);

                foreach (BpmnFlowArrow nestedArrow in xor.OutgoingArrows.ToList())
                {
                    string newCondition = $"({arrow.Condition}) && ({nestedArrow.Condition})";
                    Utilities.AddBpmnArrow(bpmnExclusiveGateway, nestedArrow.Element, BpmnFlowArrowType.Sequence, newCondition);
                }

                Utilities.RemoveBpmnArrow(bpmnExclusiveGateway, arrow);
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

        private static Dictionary<string, DataType> GetVariableDataTypesDict(List<string> allVariables, List<RelationalOperation> relations)
        {
            Dictionary<string, DataType> variableDataTypeDict = allVariables.Distinct().ToDictionary(x => x, x => DataType.Unknown);

            GiveTypesToVariablesComparedToConstants(relations, variableDataTypeDict);
            GiveTypesToVariablesComparedToVariables(relations, variableDataTypeDict);

            CheckUnknownOrInconsistentTypes(allVariables, relations, variableDataTypeDict);

            return variableDataTypeDict;
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
