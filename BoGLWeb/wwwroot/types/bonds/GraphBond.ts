import { GraphElement } from "../elements/GraphElement";

// provides the UI with information about a connection in a system diagram or bond in a bond graph
export class GraphBond {
    source: GraphElement;
    target: GraphElement;
    velocity: number; // set to 0 for bond graph bonds

    constructor(source: GraphElement, target: GraphElement, velocity = 0) {
        this.source = source;
        this.target = target;
        this.velocity = velocity;
    }

    copy(source: GraphElement, target: GraphElement) {
        return new GraphBond(source, target, this.velocity);
    }
}