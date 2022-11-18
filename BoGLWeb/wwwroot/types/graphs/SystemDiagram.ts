import { GraphBond } from "../bonds/GraphBond";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";

export class SystemDiagram {
    elements: SystemDiagramElement[];
    edges: GraphBond[];

    constructor(elements: SystemDiagramElement[], edges: GraphBond[]) {
        this.elements = elements;
        this.edges = edges;
    }
}