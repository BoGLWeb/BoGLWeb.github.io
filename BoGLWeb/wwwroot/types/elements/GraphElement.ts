// provides basic information about a graph element
export class GraphElement {
    id: number;
    label: string
    // the graph element x and y positions
    x: number;
    y: number;

    constructor(id: number, x: number, y: number, label?: string) {
        this.id = id;
        this.x = x;
        this.y = y;
    }
    copy(id) {
        return new GraphElement(id, this.x, this.y);
    }
}