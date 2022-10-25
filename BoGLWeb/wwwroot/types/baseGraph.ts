import { BGBondSelection, BGElementSelection, SVGSelection } from "../type_libraries/d3-selection";
import { DragEvent, ZoomEvent } from "../type_libraries/d3";

class BaseGraph {
    // constants
    readonly selectedClass: string = "selected";
    readonly bondClass: string = "bond";
    readonly graphClass: string = "graph";
    readonly BACKSPACE_KEY: number = 8;
    readonly DELETE_KEY: number = 46;
    readonly ENTER_KEY: number = 13;

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

        this.svg = svg;
        this.svgG = svg.append("g").classed(this.graphClass, true);
        let svgG = this.svgG;

        // displayed when dragging between elements, here because it needs to be added first
        this.dragBond = this.svgG.append("svg:path");
        this.dragBond.attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");

        // svg elements and bonds
        this.bondSelection = svgG.append("g").selectAll("g");
        this.elementSelection = svgG.append("g").selectAll("g");

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

        svg.call(this.dragSvg).on("dblclick.zoom", null);
    }

    // functions needed in system diagram are called from this class but not defined by default
    svgKeyDown() { }
    svgKeyUp() { }
    svgMouseDown() { }
    svgMouseUp() { }
    pathMouseDown(d3Bond: SVGSelection, bond: BondGraphBond) { }
    nodeMouseUp(d3Elem: SVGSelection, el: BondGraphElement) { }

    // mousedown on element
    nodeMouseDown(el: BondGraphElement) {
        (<Event>d3.event).stopPropagation();
    };

    dragmove(el: BondGraphElement) {
        el.x += (<DragEvent>d3.event).dx;
        el.y += (<DragEvent>d3.event).dy;
        this.updateGraph();
    };

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