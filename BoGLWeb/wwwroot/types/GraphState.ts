import { GraphBond } from "./GraphBond";
import { GraphElement } from "./GraphElement";
import { SystemDiagramElement } from "./SystemDiagramElement";

export class GraphState {
    selectedElement: SystemDiagramElement = null;
    selectedBond: GraphBond = null;
    mouseDownNode: GraphElement = null;
    mouseDownLink: GraphBond = null;
    justDragged: boolean = false;
    justScaleTransGraph: boolean = false;
    lastKeyDown: number = -1;
    shiftNodeDrag: boolean = false;
    graphMouseDown: boolean = false;
}