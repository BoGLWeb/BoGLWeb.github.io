import { GraphElement } from "./GraphElement";

// provides info about a system diagram element
export class SystemDiagramElement extends GraphElement {
    type: number; // element type
    modifiers: number[]; // modifiers that have been applied to the element
    velocity: number; // velocity of the element
    
    constructor(id: number, type: number, x: number, y: number, velocity: number, modifiers: number[]) {
        super(id, x, y);
        this.modifiers = modifiers;
        this.velocity = velocity;
        this.type = type;
    }

    // copies the element with an optional offset, used when copying a selection to ensure that the selections don't overlap
    copy(id, offset = 0) {
        return new SystemDiagramElement(id, this.type, this.x + offset, this.y + offset, this.velocity, [...this.modifiers]);
    }
}