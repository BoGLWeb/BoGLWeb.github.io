import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";
import { DragEvent, ZoomEvent } from "../../type_libraries/d3";
import { BaseGraph } from "../graphs/BaseGraph";
import { SystemDiagramDisplay } from "./SystemDiagramDisplay";

export class BaseGraphDisplay {
    // constants
    readonly selectedClass: string = "selected";
    readonly bondClass: string = "bond";
    readonly BACKSPACE_KEY: number = 8;
    readonly DELETE_KEY: number = 46;
    readonly ENTER_KEY: number = 13;
    readonly A_KEY: number = 65;
    readonly C_KEY: number = 67;
    readonly X_KEY: number = 88;
    readonly V_KEY: number = 86;
    readonly CTRL_KEY: number = 17;

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
    selectedElements: GraphElement[] = [];
    selectedBonds: GraphBond[] = [];

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

        // listen for key events
        let graph = this;
        svg.on("mousedown", function (d) { graph.svgMouseDown.call(graph, d); });
        svg.on("mouseup", function (d) { graph.svgMouseUp.call(graph, d); });
        svg.on("mousemove", function (d) { graph.svgMouseMove.call(graph, d); });
    }

    svgMouseDown() { }
    svgMouseMove() { }
    pathExtraRendering(path: BGBondSelection) { }
    renderElements(newElements: GraphElementSelection) { }

    svgMouseUp() {
        if (!this.justScaleTransGraph) {
            this.setSelection([], []);
        } else {
            this.justScaleTransGraph = false;
        }
    }

    // functions needed in system diagram are called from this class but not defined by default
    svgKeyDown() {
        this.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;
    }

    checkCtrlCombo(a: number) {
        return (d3.event.keyCode == a && this.lastKeyDown == this.CTRL_KEY) || (d3.event.keyCode == this.CTRL_KEY && this.lastKeyDown == a);
    }

    svgKeyUp() {
        if (this.checkCtrlCombo(this.A_KEY)) {
            this.setSelection(this.elements, this.bonds);
            this.updateGraph();
        }
    }

    // test this on return
    pathMouseDown(bond: GraphBond) {
        d3.event.stopPropagation();

        if (d3.event.ctrlKey || d3.event.metaKey) {
            if (this.selectionContains(bond)) {
                this.removeFromSelection(bond);
            } else {
                this.addToSelection(bond);
            }
        } else {
            if (!this.selectionContains(bond)) {
                this.setSelection([], []);
                this.addToSelection(bond);
            }
        }

        this.updateGraph();
    }

    nodeMouseUp(el: GraphElement) {
        d3.event.stopPropagation();

        this.mouseDownNode = null;
        if (!this.justDragged) {
            if (d3.event.ctrlKey || d3.event.metaKey) {
                if (this.selectionContains(el)) {
                    this.removeFromSelection(el);
                } else {
                    this.addToSelection(el);
                }
            } else {
                if (!this.selectionContains(el)) {
                    this.setSelection([], []);
                    this.addToSelection(el);
                }
            }
            this.updateGraph();
        }

        this.justDragged = false;
    }

    updateTopMenu() {
        DotNet.invokeMethodAsync("BoGLWeb", "SetIsSelecting", this.selectedElements.length > 0 || this.selectedBonds.length > 0);
    }

    addToSelection(e: GraphElement | GraphBond) {
        if (e instanceof GraphElement) {
            this.selectedElements.push(e);
        } else {
            this.selectedBonds.push(e);
        }
        this.updateTopMenu();
    }

    selectionContains(e: GraphElement | GraphBond) {
        if (e instanceof GraphElement) {
            return this.selectedElements.includes(e);
        } else {
            return this.selectedBonds.includes(e);
        }
    }

    removeFromSelection(e: GraphElement | GraphBond) {
        if (e instanceof GraphElement) {
            this.selectedElements = this.selectedElements.filter(d => d != e);
        } else {
            this.selectedBonds = this.selectedBonds.filter(d => d != e);
        }
        this.updateTopMenu();
    }

    setSelection(elList: GraphElement[], bondList: GraphBond[]) {
        this.selectedElements = elList;
        this.selectedBonds = bondList;
        this.updateTopMenu();
    }

    // mousedown on element
    nodeMouseDown(el: GraphElement) {
        (<Event>d3.event).stopPropagation();
        this.mouseDownNode = el;
        this.justDragged = false;
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

    checkOverlap(rect1, rect2) {
        return rect1.top <= rect2.bottom && rect1.bottom >= rect2.top && rect1.left <= rect2.right && rect1.right >= rect2.left;
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

                    d3.select("#selectionRect").attr("width", Math.abs(width))
                        .attr("height", Math.abs(height))
                        .attr("x", width >= 0 ? graph.dragStartX : mouse[0])
                        .attr("y", height >= 0 ? graph.dragStartY : mouse[1]);
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
                let selectionBounds = d3.select("#selectionRect").node().getBoundingClientRect();
                if (Math.round(selectionBounds.width) > 0 && Math.round(selectionBounds.height) > 0) {
                    let newSelection = [];
                    if (this instanceof SystemDiagramDisplay) {
                        for (const el of graph.elementSelection.selectAll(".outline")) {
                            if (graph.checkOverlap(selectionBounds, el[0].getBoundingClientRect())) {
                                newSelection.push(el[0].__data__);
                            }
                        }
                    } else {
                        for (const el of graph.elementSelection[0]) {
                            if (graph.checkOverlap(selectionBounds, el.getBoundingClientRect())) {
                                newSelection.push(el.__data__);
                            }
                        }
                    }
                    for (const bond of graph.bondSelection[0]) {
                        if (bond && graph.checkOverlap(selectionBounds, bond.getBoundingClientRect())) {
                            newSelection.push(bond.__data__);
                        }
                    }
                    if (d3.event.sourceEvent?.ctrlKey || d3.event.sourceEvent?.metaKey) {
                        for (const e of newSelection) {
                            if (graph.selectionContains(e)) {
                                graph.removeFromSelection(e);
                            } else {
                                graph.addToSelection(e);
                            }
                        }
                    } else {
                        graph.setSelection([], []);
                        for (const e of newSelection) {
                            graph.addToSelection(e);
                        }
                    }
                }
                document.getElementById("selectionRect").remove();
                d3.select("body").style("cursor", "auto");
                if (graph instanceof SystemDiagramDisplay) {
                    graph.updateModifierMenu();
                    graph.updateVelocityMenu();
                }
                graph.updateGraph();
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
                graph.pathMouseDown.call(graph, d);
            });

        // update existing bondSelection
        paths.classed(this.selectedClass, function (d) {
            return graph.selectedBonds.includes(d);
        }).attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); });

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

        let graph = this;
        newElements.classed(this.selectedClass, function (d) {
            return graph.selectedElements.includes(d);
        });

        this.renderElements(newElements);

        // remove old elements
        this.elementSelection.exit().remove();
    }

    dragmove(el: GraphElement) {
        if (this.mouseDownNode) {
            if (!this.selectedElements.includes(el)) {
                this.setSelection([el], []);
            }

            for (const el of this.selectedElements) {
                el.x += (<DragEvent>d3.event).dx;
                el.y += (<DragEvent>d3.event).dy;
            }

            this.updateGraph();
        }
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