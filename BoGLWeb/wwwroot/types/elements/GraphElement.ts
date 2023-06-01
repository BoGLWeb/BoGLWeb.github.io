// provides basic information about a graph element
export class GraphElement {
    id: number;
    // the graph element x and y positions
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