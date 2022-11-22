import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";

export class BaseGraph {
    nodes: GraphElement[];
    edges: GraphBond[];

    constructor(nodes: GraphElement[], edges: GraphBond[]) {
        this.nodes = nodes;
        this.edges = edges;
    }
}