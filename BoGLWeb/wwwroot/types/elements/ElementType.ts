export class ElementType {
    id: number;
    name: string;
    category: number;
    allowedModifiers: number[];
    image: string;

    constructor(id: number, name: string, category: number, image: string, allowedModifiers: number[], velocityAllowed: boolean) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.allowedModifiers = allowedModifiers;
        this.image = image;
    }
}