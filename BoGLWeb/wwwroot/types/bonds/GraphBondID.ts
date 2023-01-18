export class GraphBondID {
    source: number;
    target: number;
    id: number;

    constructor(source: number, target: number, id = 0) {
        this.source = source;
        this.target = target;
        this.id = id;
    }

    checkEquality(source: number, target: number) {
        return source == this.source && target == this.target;
    }
}