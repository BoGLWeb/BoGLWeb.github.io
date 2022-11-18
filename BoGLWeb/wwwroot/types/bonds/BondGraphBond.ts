import { GraphBond } from "./GraphBond";
import { GraphElement } from "../elements/GraphElement";

export class BondGraphBond extends GraphBond {
    sourceMarker: string;
    targetMarker: string;

    constructor(source: GraphElement, target: GraphElement, sourceMarker: string, targetMarker: string, velocity: number = 0) {
        super(source, target, velocity);
        this.sourceMarker = sourceMarker;
        this.targetMarker = targetMarker;
    }
}