import { GraphElement } from "../elements/GraphElement";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";
import { GraphBond } from "../bonds/GraphBond";

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
    elemId: number = 0;
}