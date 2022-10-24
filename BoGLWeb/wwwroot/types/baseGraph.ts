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
    nodes: BondGraphElement[];
    edges: BondGraphBond[];
    state: GraphState = new GraphState();
    svg: SVGSelection;
    svgG: SVGSelection;
    dragLine: SVGSelection;
    bonds: BGBondSelection;
    elements: BGElementSelection;
    draggingElement: string = null;

    constructor(svg: SVGSelection, nodes: BondGraphElement[], edges: BondGraphBond[]) {
        this.nodes = nodes || [];
        this.edges = edges || [];

        svg.attr("id", "svg");

        // define arrow markers for graph links
        var defs = svg.append("svg:defs");
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
        var svgG = this.svgG;

        // displayed when dragging between nodes
        this.dragLine = svgG.append("svg:path");
        this.dragLine.attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");

        // svg nodes and edges
        this.bonds = svgG.append("g").selectAll("g");
        this.elements = svgG.append("g").selectAll("g");

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
        var graph = this;
        return d3.behavior.drag()
            .origin(function (d: any) {
                return { x: d.x, y: d.y };
            })
            .on("drag", function (args) {
                graph.state.justDragged = true;
                graph.dragmove.call(graph, args);
            });
    }

    // listen for dragging
    get dragSvg() {
        var graph = this;
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

    setIdCt(idct) {
        this.idct = idct;
    };


    dragmove(d) {
        if (this.state.shiftNodeDrag) {
            this.dragLine.attr("d", "M" + d.x + "," + d.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
        } else {
            d.x += (<DragEvent>d3.event).dx;
            d.y += (<DragEvent>d3.event).dy;
            this.updateGraph();
        }
    };

    // remove edges associated with a node
    spliceLinksForNode(node) {
        let toSplice = this.edges.filter(function (l) {
            return (l.source === node || l.target === node);
        });
        toSplice.map(function (l) {
            this.edges.splice(this.edges.indexOf(l), 1);
        });
    };

    replaceSelectEdge(d3Path, edgeData) {
        d3Path.classed(this.selectedClass, true);
        if (this.state.selectedEdge) {
            this.removeSelectFromEdge();
        }
        this.state.selectedEdge = edgeData;
    };

    replaceSelectNode(d3Node, nodeData) {
        d3Node.classed(this.selectedClass, true);
        if (this.state.selectedNode) {
            this.removeSelectFromNode();
        }
        this.state.selectedNode = nodeData;
    };

    removeSelectFromNode() {
        let graph = this;
        this.elements.filter(function (cd) { return cd.id === graph.state.selectedNode.id; }).classed(this.selectedClass, false);
        this.state.selectedNode = null;
    };

    removeSelectFromEdge() {
        var graph = this;
        graph.bonds.filter(function (cd) { return cd === graph.state.selectedEdge; }).classed(graph.selectedClass, false);
        this.state.selectedEdge = null;
    };

    pathMouseDown(d3path, d) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownLink = d;

        if (this.state.selectedNode) {
            this.removeSelectFromNode();
        }

        var prevEdge = this.state.selectedEdge;
        if (!prevEdge || prevEdge !== d) {
            this.replaceSelectEdge(d3path, d);
        } else {
            this.removeSelectFromEdge();
        }
    };

    // mousedown on node
    nodeMouseDown(d) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownNode = d;
        if ((<KeyboardEvent>d3.event).shiftKey) {
            this.state.shiftNodeDrag = (<KeyboardEvent>d3.event).shiftKey;
            // reposition dragged directed edge
            this.dragLine.classed("hidden", false)
                .attr("d", "M" + d.x + "," + d.y + "L" + d.x + "," + d.y);
            return;
        }
    };

    // mouseup on nodes
    nodeMouseUp(d3node, d) {
        var graph = this;
        let state = graph.state;

        // reset the states
        state.shiftNodeDrag = false;
        d3node.classed(this.bondClass, false);

        var mouseDownNode = state.mouseDownNode;

        if (!mouseDownNode) return;

        this.dragLine.classed("hidden", true);

        if (mouseDownNode !== d) {
            // we"re in a different node: create new edge for mousedown edge and add to graph
            var newEdge = { source: mouseDownNode, target: d };
            var filtRes = this.bonds.filter(function (d) {
                if (d.source === newEdge.target && d.target === newEdge.source) {
                    graph.edges.splice(graph.edges.indexOf(d), 1);
                }
                return d.source === newEdge.source && d.target === newEdge.target;
            });
            if (!filtRes[0].length) {
                this.edges.push(newEdge);
                this.updateGraph();
            }
        } else {
            // we"re in the same node
            if (state.justDragged) {
                // dragged, not clicked
                state.justDragged = false;
            } else {
                if (state.selectedEdge) {
                    this.removeSelectFromEdge();
                }
                var prevNode = state.selectedNode;

                if (!prevNode || prevNode.id !== d.id) {
                    this.replaceSelectNode(d3node, d);
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
            var xycoords = d3.mouse(this.svgG.node());
            this.nodes.push(new BondGraphElement(this.idct++, this.draggingElement, xycoords[0], xycoords[1]));
            this.updateGraph();
        }
        if (state.justScaleTransGraph) {
            // dragged not clicked
            state.justScaleTransGraph = false;
        } else if (state.shiftNodeDrag) {
            // dragged from node
            state.shiftNodeDrag = false;
            this.dragLine.classed("hidden", true);
        }
        state.graphMouseDown = false;
    };

    // keydown on main svg
    svgKeyDown() {
        let state = this.state;
        // make sure repeated key presses don"t register for each keydown
        if (state.lastKeyDown !== -1) return;

        state.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;
        var selectedNode = state.selectedNode,
            selectedEdge = state.selectedEdge;

        switch ((<KeyboardEvent>d3.event).keyCode) {
            case this.BACKSPACE_KEY:
            case this.DELETE_KEY:
                (<Event>d3.event).preventDefault();
                if (selectedNode) {
                    this.nodes.splice(this.nodes.indexOf(selectedNode), 1);
                    this.spliceLinksForNode(selectedNode);
                    state.selectedNode = null;
                    this.updateGraph();
                } else if (selectedEdge) {
                    this.edges.splice(this.edges.indexOf(selectedEdge), 1);
                    state.selectedEdge = null;
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
        var graph = this;

        this.bonds = this.bonds.data(this.edges, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });
        var paths = this.bonds;
        // update existing bonds
        paths.classed(this.selectedClass, function (d) {
            return d === graph.state.selectedEdge;
        })
            .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            });

        // add new bonds
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

        // update existing nodes
        this.elements = this.elements.data<BondGraphElement>(this.nodes, function (d) { return d.id.toString(); });
        this.elements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });

        // add new elements
        var newElements = this.elements.enter().append("g");

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
        image.attr("href", function (d: any, i) { return d.img; })
            .attr("x", "-25px")
            .attr("y", "-25px")
            .attr("preserveAspectRatio", "xMidYMid meet")
            .attr("height", "50px")
            .attr("width", "50px");

        // remove old nodes
        this.elements.exit().remove();
    };

    zoomed() {
        this.state.justScaleTransGraph = true;
        d3.select("." + this.graphClass)
            .attr("transform", "translate(" + (<ZoomEvent>d3.event).translate + ") scale(" + (<ZoomEvent>d3.event).scale + ")");
    };
}

export { BaseGraph }