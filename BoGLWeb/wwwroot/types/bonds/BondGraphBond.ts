import { GraphBond } from "./GraphBond";
import { GraphElement } from "../elements/GraphElement";

// provides information to the UI about a bond in a bond graph
export class BondGraphBond extends GraphBond {
    causalStroke: boolean; // whether the bond has a causal stroke
    causalStrokeDirection: boolean; // true if the stroke is at the origin, false if at the target, igored if no causal stroke
    hasDirection: boolean; // true if bond should show arrow (source to target), false if bond is bidirectional
    effortLabel: string; // effort label that follows the bond
    flowLabel: string; // flow label that follows the bond
    // Max angles the bond can be at from horozontal while attaching the center of the label to a point off the line
    // instead of attaching the end of the label to this point. This allows the label to look centered on the bond 
    // without allowing it to overap the bond. This is calculated for effort and flow labels on initializing a bond
    effortLabelAngle: number;
    flowLabelAngle: number;
    id: number = 0;

    constructor(id: number, source: GraphElement, target: GraphElement, causalStroke: boolean, causalStrokeDirection: boolean, hasDirection: boolean, effortLabel: string, flowLabel: string, velocity: number = 0) {
        super(source, target, velocity);
        this.id = id;
        this.causalStroke = causalStroke;
        this.causalStrokeDirection = causalStrokeDirection;
        this.hasDirection = hasDirection;
        this.effortLabel = effortLabel;
        this.flowLabel = flowLabel;
    }
}