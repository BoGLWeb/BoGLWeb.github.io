class GraphState {
    selectedNode: BondGraphElement = null;
    selectedEdge: BondGraphBond = null;
    mouseDownNode: BondGraphElement = null;
    mouseDownLink: BondGraphBond = null;
    justDragged: Boolean = false;
    justScaleTransGraph: Boolean = false;
    lastKeyDown: Number = -1;
    shiftNodeDrag: Boolean = false;
    graphMouseDown: Boolean = false;
}