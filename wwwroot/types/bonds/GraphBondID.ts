// allows graph bonds to be compared and stores their velocity, used for undo/redo
export class GraphBondID {
    source: number;
    target: number;
    velID: number;

    constructor(source: number, target: number, velID = 0) {
        this.source = source;
        this.target = target;
        this.velID = velID;
    }

    checkEquality(source: number, target: number) {
        return source == this.source && target == this.target;
    }
}