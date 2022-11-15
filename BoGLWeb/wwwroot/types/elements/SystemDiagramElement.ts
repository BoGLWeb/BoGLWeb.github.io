import { GraphElement } from "./GraphElement";

export class SystemDiagramElement extends GraphElement {
    type: number;

    constructor(id: number, type: number, x: number, y: number) {
        super(id, x, y);
        this.type = type;
    }
}