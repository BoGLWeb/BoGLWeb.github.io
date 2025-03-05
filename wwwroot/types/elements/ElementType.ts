// provide info about an element type
export class ElementType {
    id: number;
    name: string; // name of element type
    category: number; // category of element type
    allowedModifiers: number[]; // allowed modifiers for an element type
    image: string; // image for the element type used in the element menu and in the system diagram SVG
    velocityAllowed: boolean; // whether the element can have a velocity
    maxConnections: number; // maximum connections that an element can have
    isMultiElement: boolean; // whether the element is multi-element, a combination of elements with a name an image

    constructor(id: number, name: string, category: number, image: string, allowedModifiers: number[], velocityAllowed: boolean, maxConnections: number = Number.MAX_SAFE_INTEGER, isMultiElement: boolean = false) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.allowedModifiers = allowedModifiers;
        this.image = image;
        this.velocityAllowed = velocityAllowed;
        this.maxConnections = maxConnections;
        this.isMultiElement = isMultiElement;
    }
}