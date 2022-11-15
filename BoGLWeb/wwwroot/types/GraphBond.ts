import { GraphElement } from "./elements/GraphElement";

export class GraphBond {
    source: GraphElement;
    target: GraphElement;

    constructor(source: GraphElement, target: GraphElement) {
        this.source = source;
        this.target = target;
    }
}