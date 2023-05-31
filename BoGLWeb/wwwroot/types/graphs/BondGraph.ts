import { BondGraphBond } from "../bonds/BondGraphBond";
import { BondGraphElement } from "../elements/BondGraphElement";
import { BaseGraph } from "./BaseGraph";

// stores a list of nodes and edges subclassed for bond graphs
export class BondGraph extends BaseGraph {
    nodes: BondGraphElement[];
    edges: BondGraphBond[];

    constructor(nodes: BondGraphElement[], edges: BondGraphBond[]) {
        super(nodes, edges);
        this.nodes = nodes;
        this.edges = edges;
    }
}