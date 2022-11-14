import { GraphElement } from "./GraphElement";

export class SystemDiagramElement extends GraphElement {
    img: string;

    constructor(id: number, img: string, x: number, y: number) {
        super(id, x, y);
        this.img = img;
    }
}