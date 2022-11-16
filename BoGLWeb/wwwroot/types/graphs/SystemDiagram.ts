import { GraphBond } from "../bonds/GraphBond";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";

export class SystemDiagram {
    nodes: SystemDiagramElement[];
    edges: GraphBond[];

    constructor(nodes: SystemDiagramElement[], edges: GraphBond[]) {
        this.nodes = nodes;
        this.edges = edges;
    }
}