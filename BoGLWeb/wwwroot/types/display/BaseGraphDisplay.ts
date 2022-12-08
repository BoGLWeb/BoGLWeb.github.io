import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";
import { GraphState } from "./GraphState";
import { DragEvent, ZoomEvent } from "../../type_libraries/d3";
import { BaseGraph } from "../graphs/BaseGraph";

export class BaseGraphDisplay {
    // constants
    readonly selectedClass: string = "selected";
    readonly bondClass: string = "bond";
    readonly BACKSPACE_KEY: number = 8;
    readonly DELETE_KEY: number = 46;
    readonly ENTER_KEY: number = 13;
    initXPos: number;
    initYPos: number;
    scale: number;
    idct: number = 0;
    elements: GraphElement[];
    bonds: GraphBond[];
    state: GraphState = new GraphState();
    svg: SVGSelection;
    svgG: SVGSelection;
    dragBond: SVGSelection;
    bondSelection: BGBondSelection;
    elementSelection: GraphElementSelection;
    draggingElement: number = null;

    constructor(svg: SVGSelection, baseGraph: BaseGraph) {
        this.elements = baseGraph.nodes || [];
        this.bonds = baseGraph.edges || [];

        svg.selectAll('*').remove();

        this.svg = svg;
        this.svgG = svg.append("g");
        let svgG = this.svgG;

        // displayed when dragging between elements, here because it needs to be added first
        this.dragBond = this.svgG.append("svg:path");
        this.dragBond.attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");

        // svg elements and bonds
        this.bondSelection = svgG.append("g").selectAll("g");
        this.elementSelection = svgG.append("g").selectAll("g");

        svg.call(this.dragSvg()).on("dblclick.zoom", null);
    }

    // functions needed in system diagram are called from this class but not defined by default
    svgKeyDown() { }
    svgKeyUp() { }
    svgMouseDown() { }
    svgMouseUp() { }
    pathMouseDown(d3Bond: SVGSelection, bond: GraphBond) { }
    pathExtraRendering(path: BGBondSelection) { }
    renderElements(newElements: GraphElementSelection) { }

    // mousedown on element
    nodeMouseDown(el: GraphElement) {
        (<Event>d3.event).stopPropagation();
        this.state.mouseDownNode = el;
        this.state.justDragged = false;
    }

    dragmove(el: GraphElement) {
        if (this.state.mouseDownNode) {
            el.x += (<DragEvent>d3.event).dx;
            el.y += (<DragEvent>d3.event).dy;
            this.updateGraph();
        }
    }

    drawPath(d: GraphBond) {
        return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
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

    changeScale(x: number, y: number, scale: number, holdValue: boolean) {
        this.initXPos = !holdValue ? x : this.initXPos;
        this.initYPos = !holdValue ? y : this.initYPos;
        this.scale = scale ?? this.scale;
        console.log("BAD BAD BAD", x, y, this.initXPos, this.initYPos, this.scale);
        this.svgG.attr("transform", "translate(" + x + ", " + y + ") scale(" + this.scale + ")");
        this.svg.call(this.dragSvg().scaleExtent([0.25, 1.75]).scale(this.scale).translate([x, y])).on("dblclick.zoom", null);
        DotNet.invokeMethodAsync("BoGLWeb", "SetScale", this.scale);
    }

    // listen for dragging
    dragSvg() {
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

    drawPaths() {
        let graph = this;
        this.bondSelection = this.bondSelection.data(this.bonds, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });
        let paths = this.bondSelection;
        // update existing bondSelection
        paths.classed(this.selectedClass, function (d) {
            return d === graph.state.selectedBond;
        }).attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); });

        // add new bondSelection
        paths.enter()
            .append("path")
            .classed("link", true)
            .attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); })
            .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d3.select(this), d);
            })
            .on("mouseup", function () {
                graph.state.mouseDownLink = null;
            });

        this.pathExtraRendering(paths);

        // remove old links
        paths.exit().remove();
    }

    fullRenderElements() {
        // update existing elements
        this.elementSelection = this.elementSelection.data<GraphElement>(this.elements, function (d) { return d.id.toString(); });
        this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });

        // add new elementSelection
        let newElements = this.elementSelection.enter().append("g");
        newElements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; })

        this.renderElements(newElements);

        // remove old elements
        this.elementSelection.exit().remove();
    }

    // call to propagate changes to graph
    updateGraph() {
        this.drawPaths();
        this.fullRenderElements();
    }

    zoomed() {
        this.state.justScaleTransGraph = true;
        this.changeScale((<ZoomEvent>d3.event).translate[0], (<ZoomEvent>d3.event).translate[1], (<ZoomEvent>d3.event).scale, false);
    }
}