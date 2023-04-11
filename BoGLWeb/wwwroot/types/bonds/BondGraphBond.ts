import { GraphBond } from "./GraphBond";
import { GraphElement } from "../elements/GraphElement";

export class BondGraphBond extends GraphBond {
    causalStroke: boolean;
    causalStrokeDirection: boolean;
    hasDirection: boolean;
    effortLabel: string;
    flowLabel: string;
    id: number = 0;

    constructor(id: number, source: GraphElement, target: GraphElement, causalStroke: boolean, causalStrokeDirection: boolean, hasDirection: boolean, velocity: number = 0) {
        super(source, target, velocity);
        this.id = id;
        this.causalStroke = causalStroke;
        this.causalStrokeDirection = causalStrokeDirection;
        this.hasDirection = hasDirection;
        this.effortLabel = "e";
        this.flowLabel = "f";
    }
}