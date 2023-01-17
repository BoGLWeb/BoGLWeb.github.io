export class GraphBondID {
    source: number;
    target: number;

    constructor(source: number, target: number) {
        this.source = source;
        this.target = target;
    }

    checkEquality(source: number, target: number) {
        return source == this.source && target == this.target;
    }
}