class BondGraphBond {
    source: BondGraphElement;
    target: BondGraphElement;
    hoverable: boolean = false;

    constructor(source: BondGraphElement, target: BondGraphElement, hoverable?: boolean) {
        this.source = source;
        this.target = target;
        this.hoverable = hoverable;
    }
}