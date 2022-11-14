define("types/GraphElement", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphElement = void 0;
    class GraphElement {
        constructor(id, x, y) {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }
    exports.GraphElement = GraphElement;
});
define("types/GraphBond", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphBond = void 0;
    class GraphBond {
        constructor(source, target) {
            this.source = source;
            this.target = target;
        }
    }
    exports.GraphBond = GraphBond;
});
define("types/SystemDiagramElement", ["require", "exports", "types/GraphElement"], function (require, exports, GraphElement_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagramElement = void 0;
    class SystemDiagramElement extends GraphElement_1.GraphElement {
        constructor(id, img, x, y) {
            super(id, x, y);
            this.img = img;
        }
    }
    exports.SystemDiagramElement = SystemDiagramElement;
});
define("types/GraphState", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphState = void 0;
    class GraphState {
        constructor() {
            this.selectedElement = null;
            this.selectedBond = null;
            this.mouseDownNode = null;
            this.mouseDownLink = null;
            this.justDragged = false;
            this.justScaleTransGraph = false;
            this.lastKeyDown = -1;
            this.shiftNodeDrag = false;
            this.graphMouseDown = false;
        }
    }
    exports.GraphState = GraphState;
});
define("types/BaseGraph", ["require", "exports", "types/GraphState"], function (require, exports, GraphState_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraph = void 0;
    class BaseGraph {
        constructor(svg, nodes, edges) {
            this.selectedClass = "selected";
            this.bondClass = "bond";
            this.BACKSPACE_KEY = 8;
            this.DELETE_KEY = 46;
            this.ENTER_KEY = 13;
            this.idct = 0;
            this.state = new GraphState_1.GraphState();
            this.draggingElement = null;
            this.elements = nodes || [];
            this.bonds = edges || [];
            this.svg = svg;
            this.svgG = svg.append("g");
            let svgG = this.svgG;
            this.dragBond = this.svgG.append("svg:path");
            this.dragBond.attr("class", "link dragline hidden")
                .attr("d", "M0,0L0,0");
            this.bondSelection = svgG.append("g").selectAll("g");
            this.elementSelection = svgG.append("g").selectAll("g");
            svg.call(this.dragSvg).on("dblclick.zoom", null);
        }
        svgKeyDown() { }
        svgKeyUp() { }
        svgMouseDown() { }
        svgMouseUp() { }
        pathMouseDown(d3Bond, bond) { }
        pathExtraRendering(path) { }
        renderElements(newElements) { }
        nodeMouseDown(el) {
            d3.event.stopPropagation();
            this.state.mouseDownNode = el;
            this.state.justDragged = false;
        }
        dragmove(el) {
            if (this.state.mouseDownNode) {
                el.x += d3.event.dx;
                el.y += d3.event.dy;
                this.updateGraph();
            }
        }
        drawPath(d) {
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
        get dragSvg() {
            let graph = this;
            return d3.behavior.zoom()
                .on("zoom", function () {
                graph.zoomed.call(graph);
                return true;
            })
                .on("zoomstart", function () {
                if (!(d3.event.sourceEvent.shiftKey))
                    d3.select("body").style("cursor", "move");
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
            paths.classed(this.selectedClass, function (d) {
                return d === graph.state.selectedBond;
            }).attr("d", function (d) { return graph.drawPath.call(graph, d); });
            paths.enter()
                .append("path")
                .classed("link", true)
                .attr("d", function (d) { return graph.drawPath.call(graph, d); })
                .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d3.select(this), d);
            })
                .on("mouseup", function () {
                graph.state.mouseDownLink = null;
            });
            this.pathExtraRendering(paths);
            paths.exit().remove();
        }
        fullRenderElements() {
            this.elementSelection = this.elementSelection.data(this.elements, function (d) { return d.id.toString(); });
            this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            let newElements = this.elementSelection.enter().append("g");
            newElements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            this.renderElements(newElements);
            this.elementSelection.exit().remove();
        }
        updateGraph() {
            this.drawPaths();
            this.fullRenderElements();
        }
        zoomed() {
            this.state.justScaleTransGraph = true;
            this.svgG.attr("transform", "translate(" + d3.event.translate + ") scale(" + d3.event.scale + ")");
        }
    }
    exports.BaseGraph = BaseGraph;
});
define("types/BondGraphBond", ["require", "exports", "types/GraphBond"], function (require, exports, GraphBond_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraphBond = void 0;
    class BondGraphBond extends GraphBond_1.GraphBond {
        constructor(source, target, sourceMarker, targetMarker) {
            super(source, target);
            this.sourceMarker = sourceMarker;
            this.targetMarker = targetMarker;
        }
    }
    exports.BondGraphBond = BondGraphBond;
});
define("types/BondGraphElement", ["require", "exports", "types/GraphElement"], function (require, exports, GraphElement_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraphElement = void 0;
    class BondGraphElement extends GraphElement_2.GraphElement {
        constructor(id, label, x, y) {
            super(id, x, y);
            this.labelSize = null;
            this.label = label;
        }
    }
    exports.BondGraphElement = BondGraphElement;
});
define("types/BondGraph", ["require", "exports", "types/BaseGraph"], function (require, exports, BaseGraph_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraph = void 0;
    class BondGraph extends BaseGraph_1.BaseGraph {
        constructor(svg, nodes, edges) {
            super(svg, nodes, edges);
            this.dragging = false;
            this.testSVG = d3.select("#app").append("svg");
            this.testSVG.style("position", "absolute")
                .style("left", "-10000000px")
                .style("top", "-10000000px");
            this.defs = svg.append("svg:defs");
            this.makeBaseMarker("flat", 1, 5, 10, 10)
                .append("path")
                .attr("d", "M1,10L1,-10");
            this.makeBaseMarker("arrow", 10, 0, 10, 10)
                .append("path")
                .attr("d", "M10,0L2,5");
            let arrowAndFlat = this.makeBaseMarker("flat_and_arrow", 10, 10, 20, 20);
            arrowAndFlat.append("path")
                .attr("d", "M10,10L2,15");
            arrowAndFlat.append("path")
                .attr("d", "M10,5L10,15");
        }
        makeBaseMarker(id, refX, refY, w, h) {
            let marker = this.defs.append("svg:marker");
            marker.attr("id", id)
                .attr("refX", refX)
                .attr("refY", refY)
                .attr("markerWidth", w)
                .attr("markerHeight", h)
                .attr("orient", "auto-start-reverse")
                .style("stroke", "#333");
            return marker;
        }
        updateGraph() {
            this.fullRenderElements();
            this.drawPaths();
        }
        getEdgePosition(source, target) {
            let sourceEl = source;
            let targetEl = target;
            let margin = 10;
            let width = sourceEl.labelSize.width / 2 + margin;
            let height = sourceEl.labelSize.height / 2 + margin;
            let x = targetEl.x - sourceEl.x;
            let y = targetEl.y - sourceEl.y;
            let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
            let thetaUR = Math.atan2(height, width);
            let thetaUL = Math.PI - thetaUR;
            let thetaLL = Math.PI + thetaUR;
            let thetaLR = 2 * Math.PI - thetaUR;
            let coords = [];
            if ((theta >= 0 && theta < thetaUR) || (theta >= thetaLR && theta < 2 * Math.PI)) {
                coords = [width, -width * Math.tan(theta)];
            }
            else if (theta >= thetaUR && theta < thetaUL) {
                coords = [height * 1 / Math.tan(theta), -height];
            }
            else if (theta >= thetaUL && theta < thetaLL) {
                coords = [-width, width * Math.tan(theta)];
            }
            else {
                coords = [-height * 1 / Math.tan(theta), height];
            }
            return coords;
        }
        drawPath(d) {
            let sourceEnd = this.getEdgePosition(d.source, d.target);
            let targetEnd = this.getEdgePosition(d.target, d.source);
            return "M" + (d.source.x + sourceEnd[0]) + "," + (d.source.y + sourceEnd[1]) + "L" + (d.target.x + targetEnd[0]) + "," + (d.target.y + targetEnd[1]);
        }
        renderElements(newElements) {
            let graph = this;
            newElements.classed("boglElem", true)
                .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
                .call(this.drag);
            let text = newElements.append("text");
            text.attr("text-anchor", "middle")
                .text((d) => d.label)
                .each((d) => {
                let testText = this.testSVG.append("text");
                testText.attr("text-anchor", "middle")
                    .text(() => d.label);
                let bb = testText.node().getBBox();
                d.labelSize = { width: bb.width, height: bb.height };
            });
        }
        pathExtraRendering(paths) {
            paths.style('marker-end', (d) => 'url(#' + d.targetMarker + ')')
                .style('marker-start', (d) => 'url(#' + d.sourceMarker + ')')
                .style('stroke-width', 2);
        }
    }
    exports.BondGraph = BondGraph;
});
define("types/SystemDiagram", ["require", "exports", "types/BaseGraph", "types/GraphBond", "types/SystemDiagramElement"], function (require, exports, BaseGraph_2, GraphBond_2, SystemDiagramElement_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagram = void 0;
    class SystemDiagram extends BaseGraph_2.BaseGraph {
        constructor(svg, nodes, edges) {
            super(svg, nodes, edges);
            this.edgeOrigin = null;
            let graph = this;
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
        moveCircle(e) {
            let coordinates = d3.mouse(d3.event.currentTarget);
            let x = coordinates[0];
            let y = coordinates[1];
            let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
            let fourth = 1 / 4 * Math.PI;
            let s = 30;
            let coords = [];
            if ((theta >= 0 && theta < fourth) || (theta >= 7 * fourth && theta < 8 * fourth)) {
                coords = [s, -s * Math.tan(theta)];
            }
            else if (theta >= fourth && theta < 3 * fourth) {
                coords = [s * 1 / Math.tan(theta), -s];
            }
            else if (theta >= 3 * fourth && theta < 5 * fourth) {
                coords = [-s, s * Math.tan(theta)];
            }
            else {
                coords = [-s * 1 / Math.tan(theta), s];
            }
            this.edgeCircle.attr("cx", e.x + coords[0]).attr("cy", e.y + coords[1]);
        }
        setFollowingEdge(sourceNode) {
            this.edgeOrigin = sourceNode;
            if (sourceNode == null) {
                this.dragBond.classed("hidden", true);
            }
            else {
                this.dragBond.attr("d", "M" + sourceNode.x + "," + sourceNode.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
                this.dragBond.classed("hidden", false);
            }
        }
        renderElements(newElements) {
            let graph = this;
            newElements.classed("boglElem", true)
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
                .call(this.drag);
            let group = newElements.append("g");
            group.attr("style", "fill:inherit;")
                .attr("index", function (d, i) { return d.id.toString(); });
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
            let hoverBox = group.append("g");
            let box = hoverBox.append("rect");
            box.classed("outline", true)
                .attr("width", "60px")
                .attr("height", "60px")
                .attr("x", "-30px")
                .attr("y", "-30px");
            let image = hoverBox.append("image");
            image.attr("href", function (d) { return d.img; })
                .classed("hoverImg", true)
                .attr("x", "-25px")
                .attr("y", "-25px")
                .attr("preserveAspectRatio", "xMidYMid meet")
                .attr("height", "50px")
                .attr("width", "50px");
            image.on("mouseenter", function () {
                graph.edgeCircle.style("visibility", "hidden");
            })
                .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d3.select(this.parentNode.parentNode.parentNode), d);
            })
                .on("mouseleave", function () {
                graph.edgeCircle.style("visibility", "visible");
            });
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
        pathExtraRendering(paths) {
            paths.classed("hoverablePath", true);
        }
        spliceLinksForNode(el) {
            let graph = this;
            let toSplice = this.bonds.filter(function (l) {
                return (l.source === el || l.target === el);
            });
            toSplice.map(function (l) {
                graph.bonds.splice(graph.bonds.indexOf(l), 1);
            });
        }
        replaceSelectEdge(d3Bond, bond) {
            d3Bond.classed(this.selectedClass, true);
            if (this.state.selectedBond) {
                this.removeSelectFromEdge();
            }
            this.state.selectedBond = bond;
        }
        replaceSelectNode(d3Elem, el) {
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
        pathMouseDown(d3Bond, bond) {
            d3.event.stopPropagation();
            this.state.mouseDownLink = bond;
            if (this.state.selectedElement) {
                this.removeSelectFromNode();
            }
            let prevEdge = this.state.selectedBond;
            if (!prevEdge || prevEdge !== bond) {
                this.replaceSelectEdge(d3Bond, bond);
            }
            else {
                this.removeSelectFromEdge();
            }
        }
        handleEdgeDown(el) {
            d3.event.stopPropagation();
            if (!this.edgeOrigin) {
                this.setFollowingEdge(el);
                d3.event.stopPropagation();
            }
        }
        handleEdgeUp(el) {
            d3.event.stopPropagation();
            if (this.edgeOrigin && this.edgeOrigin != el) {
                this.bonds.push(new GraphBond_2.GraphBond(this.edgeOrigin, el));
                this.setFollowingEdge(null);
                this.edgeOrigin = null;
                this.updateGraph();
            }
            else {
                this.setFollowingEdge(el);
                d3.event.stopPropagation();
            }
        }
        nodeMouseDown(el) {
            d3.event.stopPropagation();
            this.state.mouseDownNode = el;
            this.state.justDragged = false;
        }
        nodeMouseUp(d3Elem, el) {
            let state = this.state;
            d3.event.stopPropagation();
            state.mouseDownNode = null;
            if (this.edgeOrigin !== el && this.edgeOrigin !== null) {
                this.bonds.push(new GraphBond_2.GraphBond(this.edgeOrigin, el));
                this.setFollowingEdge(null);
                this.edgeOrigin = null;
                this.updateGraph();
            }
            else {
                if (!state.justDragged) {
                    if (state.selectedBond) {
                        this.removeSelectFromEdge();
                    }
                    let prevNode = state.selectedElement;
                    if (!prevNode || prevNode.id !== el.id) {
                        this.replaceSelectNode(d3Elem, el);
                    }
                    else {
                        this.removeSelectFromNode();
                    }
                }
            }
            state.justDragged = false;
        }
        svgMouseDown() {
            this.state.graphMouseDown = true;
        }
        svgMouseUp() {
            let state = this.state;
            this.setFollowingEdge(null);
            if (this.draggingElement) {
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());
                this.elements.push(new SystemDiagramElement_1.SystemDiagramElement(this.idct++, this.draggingElement, xycoords[0], xycoords[1]));
                this.updateGraph();
            }
            if (state.justScaleTransGraph) {
                state.justScaleTransGraph = false;
            }
            else if (state.shiftNodeDrag) {
                state.shiftNodeDrag = false;
                this.dragBond.classed("hidden", true);
            }
            state.graphMouseDown = false;
        }
        svgKeyDown() {
            let state = this.state;
            if (state.lastKeyDown !== -1)
                return;
            state.lastKeyDown = d3.event.keyCode;
            let selectedNode = state.selectedElement, selectedEdge = state.selectedBond;
            let graph = this;
            switch (d3.event.keyCode) {
                case this.BACKSPACE_KEY:
                case this.DELETE_KEY:
                    d3.event.preventDefault();
                    if (selectedNode) {
                        this.elements.splice(this.elements.indexOf(selectedNode), 1);
                        graph.spliceLinksForNode(selectedNode);
                        state.selectedElement = null;
                        this.updateGraph();
                    }
                    else if (selectedEdge) {
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
        dragmoveEdge(el) {
            if (this.edgeOrigin) {
                this.dragBond.attr("d", "M" + el.x + "," + el.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            }
        }
        zoomed() {
            if (!this.edgeOrigin) {
                this.state.justScaleTransGraph = true;
                this.svgG.attr("transform", "translate(" + d3.event.translate + ") scale(" + d3.event.scale + ")");
            }
        }
        ;
    }
    exports.SystemDiagram = SystemDiagram;
});
define("main", ["require", "exports", "types/BondGraph", "types/BondGraphBond", "types/BondGraphElement", "types/SystemDiagram"], function (require, exports, BondGraph_1, BondGraphBond_1, BondGraphElement_1, SystemDiagram_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    function makeElementSource(graph, section, link) {
        const group = document.createElement('div');
        group.classList.add("groupDiv");
        group.addEventListener("mousedown", function () {
            document.body.style.cursor = "grabbing";
            graph.draggingElement = link;
        });
        section.appendChild(group);
        var box = document.createElement('div');
        box.classList.add("box");
        group.appendChild(box);
        var image = document.createElement('img');
        image.src = link;
        image.draggable = false;
        image.classList.add("elemImage");
        box.appendChild(image);
    }
    function makeSection(graph, sectionName, images) {
        let sectionElem = document.getElementById(sectionName);
        images.forEach(image => makeElementSource(graph, sectionElem, "images/" + sectionName + "/" + image + ".svg"));
    }
    function populateMenu(graph) {
        makeSection(graph, "mechTrans", ["mass", "spring", "damper", "ground", "force_input", "gravity", "velocity_input"]);
        makeSection(graph, "mechRot", ["flywheel", "spring", "damper", "torque_input", "velocity_input"]);
        makeSection(graph, "transElem", ["lever", "pulley", "belt", "shaft", "gear", "gear_pair", "rack", "rack_pinion"]);
        makeSection(graph, "electrical", ["inductor", "capacitor", "resistor", "transformer", "junction_palette", "ground", "current_input", "voltage_input"]);
        makeSection(graph, "actuators", ["pm_motor", "vc_transducer"]);
    }
    function loadPage() {
        var systemDiagramSVG = d3.select("#systemDiagram").append("svg");
        systemDiagramSVG.classed("graphSVG", true);
        var systemDiagram = new SystemDiagram_1.SystemDiagram(systemDiagramSVG, [], []);
        systemDiagram.draggingElement = null;
        document.addEventListener("mouseup", function () {
            document.body.style.cursor = "auto";
            systemDiagram.draggingElement = null;
        });
        populateMenu(systemDiagram);
        systemDiagram.updateGraph();
        var bondGraphSVG = d3.select("#bondGraph").append("svg");
        bondGraphSVG.classed("graphSVG", true);
        let n1 = new BondGraphElement_1.BondGraphElement(0, "1", 50, 50);
        let n2 = new BondGraphElement_1.BondGraphElement(1, "R:b", 50, -50);
        let n3 = new BondGraphElement_1.BondGraphElement(2, "I:m", 150, 50);
        let n4 = new BondGraphElement_1.BondGraphElement(3, "C:1/k", 50, 150);
        let n5 = new BondGraphElement_1.BondGraphElement(4, "Se:F(t)", -50, 50);
        var bondGraph = new BondGraph_1.BondGraph(bondGraphSVG, [n1, n2, n3, n4, n5], [new BondGraphBond_1.BondGraphBond(n1, n2, "flat", "arrow"), new BondGraphBond_1.BondGraphBond(n1, n3, "", "flat_and_arrow"),
            new BondGraphBond_1.BondGraphBond(n1, n4, "flat", "arrow"), new BondGraphBond_1.BondGraphBond(n1, n5, "flat_and_arrow", "")]);
        bondGraph.updateGraph();
    }
    function pollDOM() {
        const el = document.getElementById('graphMenu');
        if (el != null) {
            loadPage();
        }
        else {
            setTimeout(pollDOM, 20);
        }
    }
    pollDOM();
});
//# sourceMappingURL=build.js.map