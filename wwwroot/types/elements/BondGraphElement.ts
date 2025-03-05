import { GraphElement } from "./GraphElement";

// provides info about a bond graph element
export class BondGraphElement extends GraphElement {
    label: string; // label for bond graph element
    labelSize: { width: number, height: number } = null; // size of bond graph label
    backendId: number; // ID for this element in the backend

    constructor(id: number, backendId: number, label: string, x: number, y: number) {
        super(id, x, y);
        this.label = label;
        this.backendId = backendId;
    }
}