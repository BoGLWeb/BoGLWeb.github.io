import { GraphBond } from "./GraphBond";
import { GraphElement } from "../elements/GraphElement";

export class BondGraphBond extends GraphBond {
    causalStroke: boolean;
    causalStrokeDirection: boolean;

    constructor(source: GraphElement, target: GraphElement, causalStroke: boolean, causalStrokeDirection: boolean, velocity: number = 0) {
        super(source, target, velocity);
        this.causalStroke = causalStroke;
        this.causalStrokeDirection = causalStrokeDirection;
    }
}