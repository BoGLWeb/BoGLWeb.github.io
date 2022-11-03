import { BGBondSelection, BGElementSelection, SVGSelection } from "../type_libraries/d3-selection";
import { DragEvent, ZoomEvent } from "../type_libraries/d3";
import { BaseGraph } from "./BaseGraph";

export class SystemDiagram extends BaseGraph {
    edgeCircle: SVGSelection;
    edgeOrigin: BondGraphElement = null;

    constructor(svg: SVGSelection, nodes: BondGraphElement[], edges: BondGraphBond[]) {
        super(svg, nodes, edges);

        let graph = this;

        // listen for key events
        d3.select(window).on("keydown", function () {
            graph.svgKeyDown.call(graph);
        })
        .on("keyup", function () {
            graph.svgKeyUp.call(graph);
        });
        svg.on("mousedown", function (d) { graph.svgMouseDown.call(graph, d); });
        svg.on("mouseup", function (d) { graph.svgMouseUp.call(graph, d); });
        this.edgeCircle = this.svgG.append("circle");
        this.edgeCircle.attr("r", "5")
            .attr("fill", "green")
            .attr("style", "cursor: pointer; visibility: hidden;");
    }

    moveCircle(e: BondGraphElement) {
        let coordinates = d3.mouse(<Event>d3.event.currentTarget);
        let x = coordinates[0];
        let y = coordinates[1];
        let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
        let fourth = 1 / 4 * Math.PI;
        let s = 30;
        let coords = [];
        // quads 1, 2, 3, and 4
        if ((theta >= 0 && theta < fourth) || (theta >= 7 * fourth && theta < 8 * fourth)) {
            coords = [s, -s * Math.tan(theta)]
        } else if (theta >= fourth && theta < 3 * fourth) {
            coords = [s * 1 / Math.tan(theta), -s]
        } else if (theta >= 3 * fourth && theta < 5 * fourth) {
            coords = [-s, s * Math.tan(theta)]
        } else {
            coords = [-s * 1 / Math.tan(theta), s]
        }
        this.edgeCircle.attr("cx", e.x + coords[0]).attr("cy", e.y + coords[1]);
    }

    setFollowingEdge(sourceNode: BondGraphElement) {
        this.edgeOrigin = sourceNode;
        if (sourceNode == null) {
            // hide edge
            this.dragBond.classed("hidden", true);
        } else {
            this.dragBond.attr("d", "M" + sourceNode.x + "," + sourceNode.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            this.dragBond.classed("hidden", false);
        }
    }

    pathExtraRendering(paths: BGBondSelection) {
        paths.classed("hoverablePath", true);
    }

    addEdgeHover(group: BGElementSelection) {
        let graph = this;

        let edgeHover = group.append("rect");
        edgeHover.attr("width", "80px")
            .attr("height", "80px")
            .attr("x", "-40px")
            .attr("y", "-40px")
            .on("mousemove", function (e) {
                graph.moveCircle.call(graph, e);
            })
            .on("mouseenter", function () {
                graph.edgeCircle.style("visibility", "visible");
            })
            .on("mouseleave", function () {
                graph.edgeCircle.style("visibility", "hidden");
            })
            .on("mouseup", function (d) {
                graph.handleEdgeUp.call(graph, d);
            })
            .on("mousedown", function (d) {
                graph.handleEdgeDown.call(graph, d);
            })
            .call(this.edgeDrag);
    }

    addHover(image: BGElementSelection, hoverBox: BGElementSelection, box: BGElementSelection) {
        let graph = this;

        // determine whether mouse is near edge of element
        image.on("mouseenter", function () {
            graph.edgeCircle.style("visibility", "hidden");
        })
        .on("mouseup", function (d) {
            graph.nodeMouseUp.call(graph, d3.select(this.parentNode.parentNode.parentNode), d);
        })
        .on("mouseleave", function () {
            graph.edgeCircle.style("visibility", "visible");
        });

        // edgeMouseUp
        box.on("mousemove", function (e) {
            graph.moveCircle.call(graph, e);
        })
        .on("mouseenter", function () {
            graph.edgeCircle.style("visibility", "visible");
        })
        .on("mouseup", function (d) {
            graph.handleEdgeUp.call(graph, d);
        })
        .on("mousedown", function (d) {
            graph.handleEdgeDown.call(graph, d);
        })
        .call(this.edgeDrag);
    }

    // remove bonds associated with a node
    spliceLinksForNode(el: BondGraphElement) {
        let graph = this;

        let toSplice = this.bonds.filter(function (l) {
            return (l.source === el || l.target === el);
        });
        toSplice.map(function (l) {
            graph.bonds.splice(graph.bonds.indexOf(l), 1);
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

    removeSelectFromEdge() {
        let graph = this;
        graph.bondSelection.filter(function (cd) { return cd === graph.state.selectedBond; }).classed(graph.selectedClass, false);
        this.state.selectedBond = null;
    }

    removeSelectFromNode() {
        let graph = this;
        this.elementSelection.filter(function (cd) { return cd.id === graph.state.selectedElement.id; }).classed(this.selectedClass, false);
        this.state.selectedElement = null;
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

    handleEdgeDown(el: BondGraphElement) {
        (<Event>d3.event).stopPropagation();
        if (!this.edgeOrigin) {
            this.setFollowingEdge(el);
            (<Event>d3.event).stopPropagation()
        }
    }

    handleEdgeUp(el: BondGraphElement) {
        (<Event>d3.event).stopPropagation();
        if (this.edgeOrigin && this.edgeOrigin != el) {
            this.bonds.push(new BondGraphBond(this.edgeOrigin, el));
            this.setFollowingEdge(null);
            this.edgeOrigin = null;
            this.updateGraph();
        } else {
            this.setFollowingEdge(el);
            (<Event>d3.event).stopPropagation()
        }
    }

    // mousedown on element
    nodeMouseDown(el: BondGraphElement) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownNode = el;
        this.state.justDragged = false;
    }

    nodeMouseUp(d3Elem: SVGSelection, el: BondGraphElement) {
        let state = this.state;

        (<Event>d3.event).stopPropagation();

        state.mouseDownNode = null;
        if (this.edgeOrigin !== el && this.edgeOrigin !== null) {
            this.bonds.push(new BondGraphBond(this.edgeOrigin, el));
            this.setFollowingEdge(null);
            this.edgeOrigin = null;
            this.updateGraph();
        } else {
            // we"re in the same node
            if (!state.justDragged) {
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
        state.justDragged = false;
    }

    // mousedown on main svg
    svgMouseDown() {
        this.state.graphMouseDown = true;
    }

    // mouseup on main svg
    svgMouseUp() {
        let state = this.state;
        this.setFollowingEdge(null);
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

        let graph = this;

        switch ((<KeyboardEvent>d3.event).keyCode) {
            case this.BACKSPACE_KEY:
            case this.DELETE_KEY:
                (<Event>d3.event).preventDefault();
                if (selectedNode) {
                    this.elements.splice(this.elements.indexOf(selectedNode), 1);
                    graph.spliceLinksForNode(selectedNode);
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

    svgKeyUp() {
        this.state.lastKeyDown = -1;
    }

    get edgeDrag() {
        let graph = this;
        return d3.behavior.drag()
            .origin(function (d) {
                return { x: d.x, y: d.y };
            })
            .on("drag", function (d) {
                graph.dragmoveEdge.call(graph, d);
            });
    }

    dragmove(el: BondGraphElement) {
        if (this.state.mouseDownNode) {
            el.x += (<DragEvent>d3.event).dx;
            el.y += (<DragEvent>d3.event).dy;
            this.updateGraph();
        }
    }

    dragmoveEdge(el: BondGraphElement) {
        if (this.edgeOrigin) {
            this.dragBond.attr("d", "M" + el.x + "," + el.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
        }
    }

    zoomed() {
        if (!this.edgeOrigin) {
            this.state.justScaleTransGraph = true;
            this.svgG.attr("transform", "translate(" + (<ZoomEvent>d3.event).translate + ") scale(" + (<ZoomEvent>d3.event).scale + ")");
        }
    };
}