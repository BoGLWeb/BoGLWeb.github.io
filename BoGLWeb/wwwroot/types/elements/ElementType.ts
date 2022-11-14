export class ElementType {
    id: number;
    name: string;
    category: number;
    allowedModifiers: number[];

    constructor(id: number, name: string, category: number, allowedModifiers: number[]) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.allowedModifiers = allowedModifiers;
    }
}