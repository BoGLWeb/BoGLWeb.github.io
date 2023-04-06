import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { GraphElement } from "../elements/GraphElement";
import { DragEvent, ZoomEvent } from "../../type_libraries/d3";
import { BaseGraph } from "../graphs/BaseGraph";
import { SystemDiagramDisplay } from "./SystemDiagramDisplay";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";

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
    readonly Z_KEY: number = 90;
    readonly Y_KEY: number = 89;
    readonly CTRL_KEY: number = 17;
    readonly ARROW_LEFT: number = 37;
    readonly ARROW_UP: number = 38;
    readonly ARROW_RIGHT: number = 39;
    readonly ARROW_DOWN: number = 40;
    readonly PAN_SPEED: number = 2.0;

    // These are related to slider zoom and dragging, some may no longer be needed once zoom is fixed
    dragAllowed: boolean = false;
    prevScale: number = 1;
    initXPos: number = null;
    initYPos: number = null;
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
    elementsBeforeDrag: GraphElement[] = null;
    dragXOffset: number = 0;
    dragYOffset: number = 0;
    startedSelectionDrag: boolean = false;
    initWidth: number = 0;
    initHeight: number = 0;

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
        let bondElements = svgG.append("g");
        bondElements.attr("id", "bondGroup");
        this.bondSelection = bondElements.selectAll("g");
        this.elementSelection = svgG.append("g").selectAll("g");

        svg.call(this.dragSvg()).on("dblclick.zoom", null);

        // listen for key events
        let graph = this;
        svg.on("mouseup", function (d) { graph.svgMouseUp.call(graph, d); });
        svg.on("mousemove", function (d) { graph.svgMouseMove.call(graph, d); });
    }

    svgMouseMove() { }
    pathExtraRendering(paths: BGBondSelection, pathGroup: BGBondSelection) { }
    renderElements(newElements: GraphElementSelection) { }

    getSelection() {
        return ([] as (GraphElement | GraphBond)[]).concat(this.selectedElements).concat(this.selectedBonds);
    }

    updateMenus() {
        this.updateTopMenu();
    }

    svgMouseUp() {
        if (!this.justScaleTransGraph) {
            let prevSelection = this.getSelection();
            this.setSelection([], []);
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), [], [], ...this.listToIDObjects(prevSelection));
        } else {
            this.justScaleTransGraph = false;
        }
    }

    // functions needed in system diagram are called from this class but not defined by default
    svgKeyDown() {
        this.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;

        switch ((<KeyboardEvent>d3.event).keyCode) {
            case this.ARROW_LEFT:
                this.changeScale(this.svgX - this.PAN_SPEED, this.svgY, this.prevScale);
                break;
            case this.ARROW_UP:
                this.changeScale(this.svgX, this.svgY - this.PAN_SPEED, this.prevScale);
                break;
            case this.ARROW_RIGHT:
                this.changeScale(this.svgX + this.PAN_SPEED, this.svgY, this.prevScale);
                break;
            case this.ARROW_DOWN:
                this.changeScale(this.svgX, this.svgY + this.PAN_SPEED, this.prevScale);
                break;
        }
    }

    checkCtrlCombo(a: number) {
        return d3.event && ((d3.event.keyCode == a && this.lastKeyDown == this.CTRL_KEY) || (d3.event.keyCode == this.CTRL_KEY && this.lastKeyDown == a));
    }

    selectAll() {
        let removeFromSelection = [].concat(this.elements.filter(e => !this.selectedElements.includes(e as SystemDiagramElement))).concat(this.bonds.filter(e => !this.selectedBonds.includes(e)));
        this.setSelection(this.elements, this.bonds);
        this.updateGraph();
        DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(removeFromSelection), [], []);
        this.updateMenus();
    }

    svgKeyUp() {
        if (this.checkCtrlCombo(this.A_KEY)) {
            this.selectAll();
        }
    }

    pathMouseDown(bond: GraphBond) {
        d3.event.stopPropagation();
        let addEdges = [];
        let removeEl = [];
        let removeEdges = [];

        if (d3.event.ctrlKey || d3.event.metaKey) {
            if (this.selectionContains(bond)) {
                this.removeFromSelection(bond);
                removeEdges = [bond];
            } else {
                this.addToSelection(bond);
                addEdges = [bond];
            }
        } else {
            if (!this.selectionContains(bond)) {
                addEdges = [bond];
                removeEl = this.selectedElements;
                removeEdges = this.selectedBonds;
                this.setSelection([], [bond]);
            }
        }

        this.updateGraph();
        DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects([].concat(addEdges)), ...this.listToIDObjects(removeEl.concat(removeEdges)));
        this.updateMenus();
    }

    handleAreaSelectionEnd() {
        if (!d3.select("#selectionRect").node()) return false;
        let selectionBounds = d3.select("#selectionRect").node().getBoundingClientRect();
        if (Math.round(selectionBounds.width) > 0 && Math.round(selectionBounds.height) > 0) {
            let newSelection = [];
            if (this instanceof SystemDiagramDisplay) {
                for (const el of this.elementSelection.selectAll(".outline")) {
                    if (this.checkOverlap(selectionBounds, el[0].getBoundingClientRect())) {
                        newSelection.push(el[0].__data__);
                    }
                }
            } else {
                for (const el of this.elementSelection[0]) {
                    if (this.checkOverlap(selectionBounds, el.getBoundingClientRect())) {
                        newSelection.push(el.__data__);
                    }
                }
            }
            for (const bond of (this.bondSelection.selectAll("path") as any)) {
                if (bond && this.checkOverlap(selectionBounds, bond.getBoundingClientRect())) {
                    newSelection.push(bond.__data__);
                }
            }

            let removeList = [];
            let addList = [];
            if (d3.event.sourceEvent?.ctrlKey || d3.event.sourceEvent?.metaKey) {
                for (const e of newSelection) {
                    if (this.selectionContains(e)) {
                        this.removeFromSelection(e, false);
                        removeList.push(e);
                    } else {
                        this.addToSelection(e, false);
                        addList.push(e);
                    }
                }
            } else {
                this.setSelection(newSelection.filter(e => e instanceof GraphElement), newSelection.filter(e => e instanceof GraphBond));
                addList = newSelection;
            }
            d3.select("body").style("cursor", "auto");
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(addList), ...this.listToIDObjects(removeList));
            this.updateMenus();
            document.getElementById("selectionRect").remove();
            return true;
        }
        document.getElementById("selectionRect").remove();
        return false;
    }

    nodeMouseUp(el: GraphElement) {
        d3.event.stopPropagation();

        this.mouseDownNode = null;
        if (this.handleAreaSelectionEnd()) return;

        if (!this.justDragged) {
            let addEl = [];
            let remove = [];

            if (d3.event.ctrlKey || d3.event.metaKey) {
                if (this.selectionContains(el)) {
                    this.removeFromSelection(el);
                    remove = [el];
                } else {
                    this.addToSelection(el);
                    addEl = [el];
                }
            } else {
                if (!this.selectionContains(el)) {
                    addEl = [el];
                    remove = this.getSelection();
                    this.setSelection([el], []);
                }
            }
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(addEl), ...this.listToIDObjects(remove));
            this.updateMenus();
        }

        this.justDragged = false;
    }

    updateTopMenu() {
        DotNet.invokeMethodAsync("BoGLWeb", "SetIsSelecting", this.selectedElements.length > 0 || this.selectedBonds.length > 0);
    }

    addToSelection(e: GraphElement | GraphBond, undoRedo: boolean = true) {
        if (e instanceof GraphElement) {
            this.selectedElements.push(e);
        } else {
            this.selectedBonds.push(e);
        }
        if (undoRedo) {
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects([e]), [], []);
        }
    }

    selectionContains(e: GraphElement | GraphBond) {
        if (e instanceof GraphElement) {
            return this.selectedElements.includes(e);
        } else {
            return this.selectedBonds.includes(e);
        }
    }

    removeFromSelection(e: GraphElement | GraphBond, undoRedo: boolean = true) {
        if (e instanceof GraphElement) {
            this.selectedElements = this.selectedElements.filter(d => d != e);
        } else {
            this.selectedBonds = this.selectedBonds.filter(d => d != e);
        }
        if (undoRedo) {
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), [], [], ...this.listToIDObjects([e]));
        }
    }

    setSelection(elList: GraphElement[], bondList: GraphBond[]) {
        this.selectedElements = elList;
        this.selectedBonds = bondList;
    }

    // mousedown on element
    nodeMouseDown(el: GraphElement) {
        (<Event>d3.event).stopPropagation();
        this.mouseDownNode = el;
        this.justDragged = false;
    }

    getEdgePosition(sourceEl: GraphElement, targetEl: GraphElement) {
        let x = targetEl.x - sourceEl.x;
        let y = targetEl.y - sourceEl.y;
        let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
        let thetaUR = Math.atan2(30, 30);
        let thetaUL = Math.PI - thetaUR;
        let thetaLL = Math.PI + thetaUR;
        let thetaLR = 2 * Math.PI - thetaUR;
        let coords = [];
        // quads 1, 2, 3, and 4
        if ((theta >= 0 && theta < thetaUR) || (theta >= thetaLR && theta < 2 * Math.PI)) {
            coords = [30, -30 * Math.tan(theta)]
        } else if (theta >= thetaUR && theta < thetaUL) {
            coords = [30 * 1 / Math.tan(theta), -30]
        } else if (theta >= thetaUL && theta < thetaLL) {
            coords = [-30, 30 * Math.tan(theta)]
        } else {
            coords = [-30 * 1 / Math.tan(theta), 30]
        }
        return coords;
    }

    drawPath(d: GraphBond) {
        if (this.startedSelectionDrag && this instanceof SystemDiagramDisplay) {
            return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
        } else {
            let sourceEnd = this.getEdgePosition(d.source, d.target);
            let targetEnd = this.getEdgePosition(d.target, d.source);
            return "M" + (d.source.x + sourceEnd[0]) + "," + (d.source.y + sourceEnd[1]) + "L" + (d.target.x + targetEnd[0]) + "," + (d.target.y + targetEnd[1]);
        }
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

    changeScale(x: number, y: number, scale: number) {
        this.svgX = x;
        this.svgY = y;
        if (this.initXPos == null) {
            this.initXPos = x;
            this.initYPos = y;
        }
        this.prevScale = scale;
        this.svgG.attr("transform", "translate(" + x + ", " + y + ") scale(" + scale + ")");
        this.svg.call(this.dragSvg().scaleExtent([0.25, 1.75]).scale(scale).translate([x, y])).on("dblclick.zoom", null);
        DotNet.invokeMethodAsync("BoGLWeb", "SetScale", scale);
    }

    checkOverlap(rect1, rect2) {
        return rect1.top <= rect2.bottom && rect1.bottom >= rect2.top && rect1.left <= rect2.right && rect1.right >= rect2.left;
    }

    listToIDObjects(eList: (GraphElement | GraphBond)[]): [number[], string[]] {
        let elements: number[] = (eList.filter(e => e instanceof GraphElement) as GraphElement[]).map(e => e.id);
        let bonds: string[] = (eList.filter(e => e instanceof GraphBond) as GraphBond[]).map(e => JSON.stringify({ source: e.source.id, target: e.target.id }));
        return [elements, bonds];
    }

    moveSelectionRect() {
        let mouse = d3.mouse(this.svgG.node());
        this.dragX = this.svgX;
        this.dragY = this.svgY;
        let width = mouse[0] - this.dragStartX;
        let height = mouse[1] - this.dragStartY;

        if (d3.select("#selectionRect").node()) {
            d3.select("#selectionRect").attr("width", Math.abs(width))
                .attr("height", Math.abs(height))
                .attr("x", width >= 0 ? this.dragStartX : mouse[0])
                .attr("y", height >= 0 ? this.dragStartY : mouse[1]);
        }
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
                    graph.moveSelectionRect();
                }
            })
            .on("zoomstart", function () {
                graph.dragAllowed = d3.event.sourceEvent.buttons === 2;
                graph.dragX = graph.dragX ?? graph.svgX;
                graph.dragY = graph.dragY ?? graph.svgY;
                let coordinates = d3.mouse(graph.svgG.node());
                graph.dragStartX = coordinates[0];
                graph.dragStartY = coordinates[1];
                if (document.getElementById("selectionRect")) {
                    document.getElementById("selectionRect").remove();
                }
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
                graph.handleAreaSelectionEnd();
            });
    }

    drawPaths() {
        let graph = this;
        this.bondSelection = this.bondSelection.data(this.bonds, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });

        let paths = this.bondSelection;
        paths.selectAll('path').remove();
        paths.selectAll('text').remove();

        // add new bondSelection
        paths.enter().append("g");
        let pathObjects = paths.append("path")
            .classed("link", true)
            .attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); })
            .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d);
            });

        // update existing bondSelection
        paths.selectAll("path").classed(this.selectedClass, function (d: GraphBond) {
            return graph.selectedBonds.includes(d);
        }).attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); });

        this.pathExtraRendering(pathObjects, paths);

        // remove old links
        paths.exit().remove();
    }

    fullRenderElements(dragmove: boolean = false) {
        if (dragmove) {
            this.elementSelection.filter(e => this.selectedElements.includes(e)).attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            return;
        }

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
                let selection = this.getSelection();
                // not updating menus until end of drag because it causes significant lag
                this.setSelection([el], []);
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects([el]), ...this.listToIDObjects(selection));
            }

            this.startedSelectionDrag = true;

            let dx = (<DragEvent>d3.event).dx;
            let dy = (<DragEvent>d3.event).dy;
            this.dragXOffset += dx;
            this.dragYOffset += dy;
            for (const el of this.selectedElements) {
                el.x += dx;
                el.y += dy;
            }

            // Just update element selection positions without editing anything else since dragmove gets called so much
            this.updateGraph(true);
        } else {
            if (this.startedSelectionDrag) {
                DotNet.invokeMethodAsync("BoGLWeb", "URMoveSelection", parseInt(window.tabNum), this.selectedElements.map(e => e.id), this.dragXOffset, this.dragYOffset);
                this.dragXOffset = 0;
                this.dragYOffset = 0;
                this.startedSelectionDrag = false;
                this.updateGraph();
                this.updateMenus();
            }
        }
    }

    // call to propagate changes to graph
    updateGraph(dragmove: boolean = false) {
        this.drawPaths();
        this.fullRenderElements(dragmove);
    }

    zoomed() {
        this.justScaleTransGraph = true;
        if (this.prevScale !== (<ZoomEvent>d3.event).scale || d3.event.sourceEvent.buttons == 2) {
            this.changeScale((<ZoomEvent>d3.event).translate[0], (<ZoomEvent>d3.event).translate[1], (<ZoomEvent>d3.event).scale);
        }
    }
}