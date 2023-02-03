export class GraphElement {
    id: number;
    x: number;
    y: number;

    constructor(id: number, x: number, y: number) {
        this.id = id;
        this.x = x;
        this.y = y;
    }

    copy(id) {
        return new GraphElement(id, this.x, this.y);
    }
}