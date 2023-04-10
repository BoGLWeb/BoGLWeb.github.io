import { GraphElement } from "../elements/GraphElement";

export class GraphBond {
    source: GraphElement;
    target: GraphElement;
    velocity: number;
    effortLabel: string;
    flowLabel: string;
    id: number = 0;

    constructor(source: GraphElement, target: GraphElement, velocity = 0) {
        this.source = source;
        this.target = target;
        this.velocity = velocity;
        this.effortLabel = "e_" + this.id;
        this.flowLabel = "f_" + this.id;
    }

    copy(source: GraphElement, target: GraphElement) {
        return new GraphBond(source, target, this.velocity);
    }
}