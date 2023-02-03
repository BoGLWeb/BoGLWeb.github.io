import { GraphBond } from "../bonds/GraphBond";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";
import { BaseGraph } from "./BaseGraph";

export class SystemDiagram extends BaseGraph {
    nodes: SystemDiagramElement[];
    edges: GraphBond[];

    constructor(nodes: SystemDiagramElement[], edges: GraphBond[]) {
        super(nodes, edges);
        this.nodes = nodes;
    }
}