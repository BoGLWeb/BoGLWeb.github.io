import { BondGraphBond } from "../bonds/BondGraphBond";
import { BondGraphElement } from "../elements/BondGraphElement";

export class BondGraph {
    nodes: BondGraphElement[];
    edges: BondGraphBond[];

    constructor(nodes: BondGraphElement[], edges: BondGraphBond[]) {
        this.nodes = nodes;
        this.edges = edges;
    }
}