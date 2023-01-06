import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";
import { DragEvent, ZoomEvent } from "../../type_libraries/d3";
import { BaseGraph } from "../graphs/BaseGraph";

export class BaseGraphDisplay {
    // constants
    readonly selectedClass: string = "selected";
    readonly bondClass: string = "bond";
    readonly BACKSPACE_KEY: number = 8;
    readonly DELETE_KEY: number = 46;
    readonly ENTER_KEY: number = 13;

    // These are related to slider zoom and dragging, some may no longer be needed once zoom is fixed
    zoomWithSlider: boolean = false;
    dragAllowed: boolean = false;
    prevScale: number = 1;
    initXPos: number;
    initYPos: number;
    svgX: number = 0;
    svgY: number = 0;
    dragX: number;
    dragY: number;

    elements: GraphElement[];
    bonds: GraphBond[];
    svg: SVGSelection;
    svgG: SVGSelection;
    dragBond: SVGSelection;
    bondSelection: BGBondSelection;
    elementSelection: GraphElementSelection;
    draggingElement: number = null;

    mouseDownNode: GraphElement = null;
    justDragged: boolean = false;
    justScaleTransGraph: boolean = false;
    lastKeyDown: number = -1;
    highestElemId: number = 0;
    dragStartX: number;
    dragStartY: number;

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
        this.mouseDownNode = el;
        this.justDragged = false;
    }

    dragmove(el: GraphElement) {
        if (this.mouseDownNode) {
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
                graph.justDragged = true;
                graph.dragmove.call(graph, args);
            });
    }

    changeScale(x: number, y: number, scale: number, slider: boolean) {
        this.initXPos = !slider ? x : this.initXPos;
        this.initYPos = !slider ? y : this.initYPos;
        this.svgX = x;
        this.svgY = y;
        this.prevScale = scale;
        this.zoomWithSlider = slider;
        this.svgG.attr("transform", "translate(" + x + ", " + y + ") scale(" + scale + ")");
        this.svg.call(this.dragSvg().scaleExtent([0.25, 1.75]).scale(scale).translate([x, y])).on("dblclick.zoom", null);
        DotNet.invokeMethodAsync("BoGLWeb", "SetScale", scale);
    }

    // listen for dragging
    dragSvg() {
        let graph = this;
        return d3.behavior.zoom()
            .on("zoom", function () {
                graph.zoomed.call(graph);
                if (graph.dragAllowed) {
                    graph.dragX = d3.event.translate[0];
                    graph.dragY = d3.event.translate[1];
                } else {
                    let mouse = d3.mouse(graph.svgG.node());
                    graph.dragX = graph.svgX;
                    graph.dragY = graph.svgY;
                    let width = mouse[0] - graph.dragStartX;
                    let height = mouse[1] - graph.dragStartY;
                    let selectionRect = d3.select("#selectionRect");
                    if (width >= 0) {
                        selectionRect.attr("x", graph.dragStartX).attr("width", width);
                    } else {
                        selectionRect.attr("x", mouse[0]).attr("width", Math.abs(width));
                    }

                    if (height >= 0) {
                        selectionRect.attr("y", graph.dragStartY).attr("height", height);
                    } else {
                        selectionRect.attr("y", mouse[1]).attr("height", Math.abs(height));
                    }
                }
            })
            .on("zoomstart", function () {
                graph.dragAllowed = d3.event.sourceEvent.buttons === 2;
                graph.dragX = graph.dragX ?? graph.svgX;
                graph.dragY = graph.dragY ?? graph.svgY;
                let coordinates = d3.mouse(graph.svgG.node());
                graph.dragStartX = coordinates[0];
                graph.dragStartY = coordinates[1];
                graph.svgG.append("rect")
                    .attr("id", "selectionRect")
                    .attr("x", graph.dragStartX)
                    .attr("y", graph.dragStartY)
                    .attr("width", 0)
                    .attr("height", 0)
                    .style("stroke", "black")
                    .style("fill", "blue")
                    .style("opacity", "0.3");
                graph.svg.call(graph.dragSvg().scaleExtent([0.25, 1.75]).scale(graph.prevScale).translate([graph.dragX, graph.dragY])).on("dblclick.zoom", null);
                if (!((<KeyboardEvent>(<ZoomEvent>d3.event).sourceEvent).shiftKey)) d3.select("body").style("cursor", "move");
            })
            .on("zoomend", function () {
                document.getElementById("selectionRect").remove();
                d3.select("body").style("cursor", "auto");
            });
    }

    drawPaths() {
        let graph = this;
        this.bondSelection = this.bondSelection.data(this.bonds, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });

        let paths = this.bondSelection;

        // add new bondSelection
        paths.enter()
            .append("path")
            .classed("link", true)
            .attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); })
            .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d3.select(this), d);
            });

        this.pathExtraRendering(paths);

        // remove old links
        paths.exit().remove();
    }

    fullRenderElements() {
        // update existing elements
        this.elementSelection = this.elementSelection.data<GraphElement>(this.elements, function (d) { return d.id.toString(); });
        this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });

        this.elementSelection.selectAll("*").remove();

        let newElements = this.elementSelection;
        // add new elementSelection
        newElements.enter().append("g");
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
        this.justScaleTransGraph = true;
        if (this.prevScale !== (<ZoomEvent>d3.event).scale || d3.event.sourceEvent.buttons == 2) {
            this.changeScale((<ZoomEvent>d3.event).translate[0], (<ZoomEvent>d3.event).translate[1], (<ZoomEvent>d3.event).scale, false);
        }
    }
}