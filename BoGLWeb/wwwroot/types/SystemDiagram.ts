import { SVGSelection } from "../type_libraries/d3-selection";
import { DragEvent } from "../type_libraries/d3";
import { BaseGraph } from "./BaseGraph";

export class SystemDiagram extends BaseGraph {
    constructor(svg: SVGSelection, nodes: BondGraphElement[], edges: BondGraphBond[]) {
        super(svg, nodes, edges);
        // displayed when dragging between elements
        this.dragBond = this.svgG.append("svg:path");
        this.dragBond.attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");
    }

    // remove bonds associated with a node
    spliceLinksForNode(el: BondGraphElement) {
        let toSplice = this.bonds.filter(function (l) {
            return (l.source === el || l.target === el);
        });
        toSplice.map(function (l) {
            this.edges.splice(this.edges.indexOf(l), 1);
        });
    }

    replaceSelectEdge(d3Bond: SVGSelection, bond: BondGraphBond) {
        d3Bond.classed(this.selectedClass, true);
        if (this.state.selectedBond) {
            this.removeSelectFromEdge();
        }
        this.state.selectedBond = bond;
    }

    replaceSelectNode(d3Elem: SVGSelection, el: BondGraphElement) {
        d3Elem.classed(this.selectedClass, true);
        if (this.state.selectedElement) {
            this.removeSelectFromNode();
        }
        this.state.selectedElement = el;
    }

    removeSelectFromNode() {
        let graph = this;
        this.elementSelection.filter(function (cd) { return cd.id === graph.state.selectedElement.id; }).classed(this.selectedClass, false);
        this.state.selectedElement = null;
    }

    removeSelectFromEdge() {
        let graph = this;
        graph.bondSelection.filter(function (cd) { return cd === graph.state.selectedBond; }).classed(graph.selectedClass, false);
        this.state.selectedBond = null;
    }

    pathMouseDown(d3Bond: SVGSelection, bond: BondGraphBond) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownLink = bond;

        if (this.state.selectedElement) {
            this.removeSelectFromNode();
        }

        let prevEdge = this.state.selectedBond;
        if (!prevEdge || prevEdge !== bond) {
            this.replaceSelectEdge(d3Bond, bond);
        } else {
            this.removeSelectFromEdge();
        }
    }

    // mousedown on element
    nodeMouseDown(el: BondGraphElement) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownNode = el;
        if ((<KeyboardEvent>d3.event).shiftKey) {
            this.state.shiftNodeDrag = (<KeyboardEvent>d3.event).shiftKey;
            // reposition dragged directed edge
            this.dragBond.classed("hidden", false)
                .attr("el", "M" + el.x + "," + el.y + "L" + el.x + "," + el.y);
            return;
        }
    }

    // mouseup on elements
    nodeMouseUp(d3Elem: SVGSelection, el: BondGraphElement) {
        let graph = this;
        let state = graph.state;

        // reset the states
        state.shiftNodeDrag = false;
        d3Elem.classed(this.bondClass, false);

        let mouseDownNode = state.mouseDownNode;

        if (!mouseDownNode) return;

        this.dragBond.classed("hidden", true);

        if (mouseDownNode !== el) {
            // we"re in a different node: create new edge for mousedown edge and add to graph
            let newEdge = new BondGraphBond(mouseDownNode, el);
            let filtRes = this.bondSelection.filter(function (d) {
                if (d.source === newEdge.target && d.target === newEdge.source) {
                    graph.bonds.splice(graph.bonds.indexOf(d), 1);
                }
                return d.source === newEdge.source && d.target === newEdge.target;
            });
            if (!filtRes[0].length) {
                this.bonds.push(newEdge);
                this.updateGraph();
            }
        } else {
            // we"re in the same node
            if (state.justDragged) {
                // dragged, not clicked
                state.justDragged = false;
            } else {
                if (state.selectedBond) {
                    this.removeSelectFromEdge();
                }
                let prevNode = state.selectedElement;

                if (!prevNode || prevNode.id !== el.id) {
                    this.replaceSelectNode(d3Elem, el);
                } else {
                    this.removeSelectFromNode();
                }
            }
        }
        state.mouseDownNode = null;
        return;
    }

    // mousedown on main svg
    svgMouseDown() {
        this.state.graphMouseDown = true;
    }

    // mouseup on main svg
    svgMouseUp() {
        let state = this.state;
        if (this.draggingElement) {
            document.body.style.cursor = "auto";
            let xycoords = d3.mouse(this.svgG.node());
            this.elements.push(new BondGraphElement(this.idct++, this.draggingElement, xycoords[0], xycoords[1]));
            this.updateGraph();
        }
        if (state.justScaleTransGraph) {
            // dragged not clicked
            state.justScaleTransGraph = false;
        } else if (state.shiftNodeDrag) {
            // dragged from node
            state.shiftNodeDrag = false;
            this.dragBond.classed("hidden", true);
        }
        state.graphMouseDown = false;
    }

    // keydown on main svg
    svgKeyDown() {
        let state = this.state;
        // make sure repeated key presses don"t register for each keydown
        if (state.lastKeyDown !== -1) return;

        state.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;
        let selectedNode = state.selectedElement,
            selectedEdge = state.selectedBond;

        switch ((<KeyboardEvent>d3.event).keyCode) {
            case this.BACKSPACE_KEY:
            case this.DELETE_KEY:
                (<Event>d3.event).preventDefault();
                if (selectedNode) {
                    this.elements.splice(this.elements.indexOf(selectedNode), 1);
                    this.spliceLinksForNode(selectedNode);
                    state.selectedElement = null;
                    this.updateGraph();
                } else if (selectedEdge) {
                    this.bonds.splice(this.bonds.indexOf(selectedEdge), 1);
                    state.selectedBond = null;
                    this.updateGraph();
                }
                break;
        }
    }

    dragmove(el: BondGraphElement) {
        if (this.state.shiftNodeDrag) {
            this.dragBond.attr("el", "M" + el.x + "," + el.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
        } else {
            el.x += (<DragEvent>d3.event).dx;
            el.y += (<DragEvent>d3.event).dy;
            this.updateGraph();
        }
    }

    svgKeyUp() {
        this.state.lastKeyDown = -1;
    }
}