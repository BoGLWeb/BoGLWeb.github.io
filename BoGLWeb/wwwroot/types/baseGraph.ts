import { BGBondSelection, BGElementSelection, SVGSelection } from "../type_libraries/d3-selection";
import { DragEvent, ZoomEvent } from "../type_libraries/d3";

class BaseGraph {
    // constants
    readonly selectedClass: string = "selected";
    readonly bondClass: string = "bond";
    readonly graphClass: "graph";
    readonly BACKSPACE_KEY: 8;
    readonly DELETE_KEY: 46;
    readonly ENTER_KEY: 13;

    idct: number = 0;
    elements: BondGraphElement[];
    bonds: BondGraphBond[];
    state: GraphState = new GraphState();
    svg: SVGSelection;
    svgG: SVGSelection;
    dragBond: SVGSelection;
    bondSelection: BGBondSelection;
    elementSelection: BGElementSelection;
    draggingElement: string = null;

    constructor(svg: SVGSelection, nodes: BondGraphElement[], edges: BondGraphBond[]) {
        this.elements = nodes || [];
        this.bonds = edges || [];

        svg.attr("id", "svg");

        // define arrow markers for graph links
        let defs = svg.append("svg:defs");
        defs.append("svg:marker")
            .attr("id", "end-arrow")
            .attr("viewBox", "0 -5 10 10")
            .attr("refX", "32")
            .attr("markerWidth", 3.5)
            .attr("markerHeight", 3.5)
            .attr("orient", "auto")
            .append("svg:path")
            .attr("d", "M0,-5L10,0L0,5");

        // define arrow markers for leading arrow
        defs.append("svg:marker")
            .attr("id", "mark-end-arrow")
            .attr("viewBox", "0 -5 10 10")
            .attr("refX", 7)
            .attr("markerWidth", 3.5)
            .attr("markerHeight", 3.5)
            .attr("orient", "auto")
            .append("svg:path")
            .attr("d", "M0,-5L10,0L0,5");

        this.svg = svg;
        this.svgG = svg.append("g")
            .classed(this.graphClass, true);
        let svgG = this.svgG;

        // displayed when dragging between elements
        this.dragBond = svgG.append("svg:path");
        this.dragBond.attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");

        // svg elements and bonds
        this.bondSelection = svgG.append("g").selectAll("g");
        this.elementSelection = svgG.append("g").selectAll("g");

        let graph = this;

        // listen for key events
        d3.select(window).on("keydown", function () {
            this.svgKeyDown.call(this);
        })
        .on("keyup", function () {
            this.svgKeyUp.call(this);
        });
        svg.on("mousedown", function (d) { graph.svgMouseDown.call(this, d); });
        svg.on("mouseup", function (d) { graph.svgMouseUp.call(this, d); });

        svg.call(this.dragSvg).on("dblclick.zoom", null);
    }

    get drag() {
        let graph = this;
        return d3.behavior.drag()
            .origin(function (d) {
                return { x: d.x, y: d.y };
            })
            .on("drag", function (args) {
                graph.state.justDragged = true;
                graph.dragmove.call(graph, args);
            });
    }

    // listen for dragging
    get dragSvg() {
        let graph = this;
        return d3.behavior.zoom()
            .on("zoom", function () {
                graph.zoomed.call(graph);
                return true;
            })
            .on("zoomstart", function () {
                if (!((<KeyboardEvent>(<ZoomEvent>d3.event).sourceEvent).shiftKey)) d3.select("body").style("cursor", "move");
            })
            .on("zoomend", function () {
                d3.select("body").style("cursor", "auto");
            });
    }

    setIdCt(id: number) {
        this.idct = id;
    };

    dragmove(el: BondGraphElement) {
        if (this.state.shiftNodeDrag) {
            this.dragBond.attr("el", "M" + el.x + "," + el.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
        } else {
            el.x += (<DragEvent>d3.event).dx;
            el.y += (<DragEvent>d3.event).dy;
            this.updateGraph();
        }
    };

    // remove bonds associated with a node
    spliceLinksForNode(el: BondGraphElement) {
        let toSplice = this.bonds.filter(function (l) {
            return (l.source === el || l.target === el);
        });
        toSplice.map(function (l) {
            this.edges.splice(this.edges.indexOf(l), 1);
        });
    };

    replaceSelectEdge(d3Bond: SVGSelection, bond: BondGraphBond) {
        d3Bond.classed(this.selectedClass, true);
        if (this.state.selectedBond) {
            this.removeSelectFromEdge();
        }
        this.state.selectedBond = bond;
    };

    replaceSelectNode(d3Elem: SVGSelection, el: BondGraphElement) {
        d3Elem.classed(this.selectedClass, true);
        if (this.state.selectedElement) {
            this.removeSelectFromNode();
        }
        this.state.selectedElement = el;
    };

    removeSelectFromNode() {
        let graph = this;
        this.elementSelection.filter(function (cd) { return cd.id === graph.state.selectedElement.id; }).classed(this.selectedClass, false);
        this.state.selectedElement = null;
    };

    removeSelectFromEdge() {
        let graph = this;
        graph.bondSelection.filter(function (cd) { return cd === graph.state.selectedBond; }).classed(graph.selectedClass, false);
        this.state.selectedBond = null;
    };

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
    };

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
    };

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
            let newEdge = { source: mouseDownNode, target: el };
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

    };

    // mousedown on main svg
    svgMouseDown() {
        this.state.graphMouseDown = true;
    };

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
    };

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
    };

    svgKeyUp() {
        this.state.lastKeyDown = -1;
    };

    // call to propagate changes to graph
    updateGraph() {
        let graph = this;

        this.bondSelection = this.bondSelection.data(this.bonds, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });
        let paths = this.bondSelection;
        // update existing bondSelection
        paths.classed(this.selectedClass, function (d) {
            return d === graph.state.selectedBond;
        })
            .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            });

        // add new bondSelection
        paths.enter()
            .append("path")
            .classed("link", true)
            .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            })
            .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d3.select(this), d);
            })
            .on("mouseup", function (d) {
                graph.state.mouseDownLink = null;
            });

        // remove old links
        paths.exit().remove();

        // update existing elements
        this.elementSelection = this.elementSelection.data<BondGraphElement>(this.elements, function (d) { return d.id.toString(); });
        this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });

        // add new elementSelection
        let newElements = this.elementSelection.enter().append("g");

        newElements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; })
            .on("mouseover", function () {
                if (graph.state.shiftNodeDrag) {
                    d3.select(this).classed(graph.bondClass, true);
                }
            })
            .on("mouseout", function () {
                d3.select(this).classed(graph.bondClass, false);
            })
            .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
            .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d3.select(this), d);
            })
            .call(this.drag);

        let group = newElements.append("g");
        group.attr("style", "fill:inherit")
            .attr("index", function (d, i) { return d.id.toString(); });

        let box = group.append("rect");
        box.attr("width", "60px")
            .attr("height", "60px")
            .attr("x", "-30px")
            .attr("y", "-30px")
            .attr("style", "fill:inherit");

        let image = group.append("image");
        image.attr("href", function (d) { return d.img; })
            .attr("x", "-25px")
            .attr("y", "-25px")
            .attr("preserveAspectRatio", "xMidYMid meet")
            .attr("height", "50px")
            .attr("width", "50px");

        // remove old elements
        this.elementSelection.exit().remove();
    };

    zoomed() {
        this.state.justScaleTransGraph = true;
        d3.select("." + this.graphClass)
            .attr("transform", "translate(" + (<ZoomEvent>d3.event).translate + ") scale(" + (<ZoomEvent>d3.event).scale + ")");
    };
}

export { BaseGraph }