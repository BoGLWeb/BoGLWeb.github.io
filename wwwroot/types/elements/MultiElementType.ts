import {ElementType} from "./ElementType";

// provides info about a multi-element, which is a combination of system diagram elements
export class MultiElementType extends ElementType {
    // elements in the multi-element
    subElements: number[];
    // edges between sub-elements
    subElementEdges: number[][];
    // x and y offsets for each element in the multi-element
    offsets: number[][];
    constructor(id: number, name: string, category: number, image: string, allowedModifiers: number[], velocityAllowed: boolean, subElements: number[], subElementEdges: number[][], offsets: number[][], maxConnections: number = Number.MAX_SAFE_INTEGER) {
        super(id, name, category, image, allowedModifiers, velocityAllowed, maxConnections, true);
        this.subElements = subElements;
        this.subElementEdges = subElementEdges;
        this.offsets = offsets;
    }
}