class GraphState {
    selectedElement: BondGraphElement = null;
    selectedBond: BondGraphBond = null;
    mouseDownNode: BondGraphElement = null;
    mouseDownLink: BondGraphBond = null;
    justDragged: boolean = false;
    justScaleTransGraph: boolean = false;
    lastKeyDown: number = -1;
    shiftNodeDrag: boolean = false;
    graphMouseDown: boolean = false;
}