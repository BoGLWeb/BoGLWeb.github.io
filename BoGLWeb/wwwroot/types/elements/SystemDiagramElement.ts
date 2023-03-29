import { GraphElement } from "./GraphElement";

export class SystemDiagramElement extends GraphElement {
    type: number;
    modifiers: number[];
    velocity: number;
    
    constructor(id: number, type: number, x: number, y: number, velocity: number, modifiers: number[]) {
        super(id, x, y);
        this.modifiers = modifiers;
        this.velocity = velocity;
        this.type = type;
    }

    copy(id, offset = 0) {
        return new SystemDiagramElement(id, this.type, this.x + offset, this.y + offset, this.velocity, [...this.modifiers]);
    }
}