import { GraphElement } from "../elements/GraphElement";

export class GraphBond {
    source: GraphElement;
    target: GraphElement;
    velocity: number;

    constructor(source: GraphElement, target: GraphElement, velocity: number = 0) {
        this.source = source;
        this.target = target;
        this.velocity = velocity;
    }
}