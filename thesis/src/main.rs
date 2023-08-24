use std::collections::HashMap;
use std::rc::Rc;

#[derive(Debug)]
enum BpmnFlowType {
    Sequence,
    Message
}

#[derive(Debug, PartialEq, Eq, Hash, Clone)]
enum BpmnGatewayType {
    AND,
    OR
}

#[derive(Debug, PartialEq, Eq, Hash, Clone)]
enum BpmnFlowElement {
    Activity(BpmnActivity),
    Gateway(BpmnGatewayType)
}

#[derive(Debug, PartialEq, Eq, Hash, Clone)]
struct BpmnActivity {
    name: String
}

impl BpmnActivity {
    pub fn new(name: String) -> BpmnActivity {
        BpmnActivity {
            name
        }
    }
}

type BpmnFlow = (BpmnFlowType, BpmnFlowElement);
type BpmnGraphContent = HashMap<BpmnFlowElement, Vec<BpmnFlow>>;

#[derive(Debug)]
struct BpmnGraph {
    graph_content: BpmnGraphContent
}

impl BpmnGraph {
    pub fn new(graph_content: BpmnGraphContent) -> BpmnGraph {
        BpmnGraph {
            graph_content
        }
    }
}







fn main() {
    let mut graph_content: BpmnGraphContent = HashMap::new();

    let activity1 = BpmnFlowElement::Activity(
        BpmnActivity::new("Name1".to_string())
    );

    let activity2 = BpmnFlowElement::Activity(
        BpmnActivity::new("Name2".to_string())
    );

    let flow1: BpmnFlow = (
        BpmnFlowType::Sequence,
        activity2.clone()
    );

    graph_content.insert(activity1.clone(), vec![flow1]);
    graph_content.insert(activity2.clone(), vec![]);

    for key in graph_content.keys() {
        println!("{:?}", key);
    }

    println!("{:?}", BpmnGraph::new(graph_content))
}
