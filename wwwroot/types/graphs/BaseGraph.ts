import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";

// stores a list of nodes and edges that define a graph
export class BaseGraph {
    nodes: GraphElement[];
    edges: GraphBond[];

    constructor(nodes: GraphElement[], edges: GraphBond[]) {
        this.nodes = nodes;
        this.edges = edges;
    }
}