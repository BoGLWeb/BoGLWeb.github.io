import { GraphBond } from "./GraphBond";
import { GraphElement } from "./elements/GraphElement";

export class BondGraphBond extends GraphBond {
    sourceMarker: string;
    targetMarker: string;

    constructor(source: GraphElement, target: GraphElement, sourceMarker: string, targetMarker: string) {
        super(source, target);
        this.sourceMarker = sourceMarker;
        this.targetMarker = targetMarker;
    }
}