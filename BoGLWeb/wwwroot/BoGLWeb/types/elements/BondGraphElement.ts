import { GraphElement } from "./GraphElement";

export class BondGraphElement extends GraphElement {
    label: string;
    labelSize: { width: number, height: number } = null;

    constructor(id: number, label: string, x: number, y: number) {
        super(id, x, y);
        this.label = label;
    }
}