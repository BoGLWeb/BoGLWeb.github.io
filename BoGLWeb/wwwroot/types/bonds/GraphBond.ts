﻿import { GraphElement } from "../elements/GraphElement";

export class GraphBond {
    source: GraphElement;
    target: GraphElement;
    velocity: number;

    constructor(source: GraphElement, target: GraphElement, velocity = 0) {
        this.source = source;
        this.target = target;
        this.velocity = velocity;
    }

    copy(source: GraphElement, target: GraphElement) {
        return new GraphBond(source, target, this.velocity);
    }
}