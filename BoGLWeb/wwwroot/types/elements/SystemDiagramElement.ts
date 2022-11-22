import { GraphElement } from "./GraphElement";

export class SystemDiagramElement extends GraphElement {
    type: number;
    velocity: number;

    constructor(id: number, type: number, x: number, y: number, velocity: number) {
        super(id, x, y);
        this.velocity = velocity;
        this.type = type;
    }
}