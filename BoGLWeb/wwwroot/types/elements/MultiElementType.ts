import {ElementType} from "./ElementType";

export class MultiElementType extends ElementType{
    subElements: number[];
    subElementEdges: number[][];
    offsets: number[][];
    constructor(id: number, name: string, category: number, image: string, allowedModifiers: number[], velocityAllowed: boolean, subElements: number[], subElementEdges: number[][], offsets: number[][], maxConnections: number = Number.MAX_SAFE_INTEGER) {
        super(id, name, category, image, allowedModifiers, velocityAllowed, maxConnections, true);
        this.subElements = subElements;
        this.subElementEdges = subElementEdges;
        this.offsets = offsets;
    }
}