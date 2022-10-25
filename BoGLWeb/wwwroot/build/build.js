define("types/BaseGraph", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraph = void 0;
    class BaseGraph {
        constructor(svg, nodes, edges) {
            this.selectedClass = "selected";
            this.bondClass = "bond";
            this.graphClass = "graph";
            this.BACKSPACE_KEY = 8;
            this.DELETE_KEY = 46;
            this.ENTER_KEY = 13;
            this.idct = 0;
            this.state = new GraphState();
            this.draggingElement = null;
            this.elements = nodes || [];
            this.bonds = edges || [];
            this.svg = svg;
            this.svgG = svg.append("g").classed(this.graphClass, true);
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
        nodeMouseUp(d3Elem, el) { }
        nodeMouseDown(el) {
            d3.event.stopPropagation();
        }
        ;
        dragmove(el) {
            el.x += d3.event.dx;
            el.y += d3.event.dy;
            this.updateGraph();
        }
        ;
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
        updateGraph() {
            let graph = this;
            this.bondSelection = this.bondSelection.data(this.bonds, function (d) {
                return String(d.source.id) + "+" + String(d.target.id);
            });
            let paths = this.bondSelection;
            paths.classed(this.selectedClass, function (d) {
                return d === graph.state.selectedBond;
            })
                .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            });
            paths.enter()
                .append("path")
                .classed("link", true)
                .classed("hoverablePath", function (d) {
                return d.hoverable;
            })
                .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            })
                .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d3.select(this), d);
            })
                .on("mouseup", function () {
                graph.state.mouseDownLink = null;
            });
            paths.exit().remove();
            this.elementSelection = this.elementSelection.data(this.elements, function (d) { return d.id.toString(); });
            this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            let newElements = this.elementSelection.enter().append("g");
            newElements.classed("boglElem", true)
                .attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; })
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
            this.elementSelection.exit().remove();
        }
        ;
        zoomed() {
            this.state.justScaleTransGraph = true;
            d3.select("." + this.graphClass)
                .attr("transform", "translate(" + d3.event.translate + ") scale(" + d3.event.scale + ")");
        }
        ;
    }
    exports.BaseGraph = BaseGraph;
});
define("types/SystemDiagram", ["require", "exports", "types/BaseGraph"], function (require, exports, BaseGraph_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagram = void 0;
    class SystemDiagram extends BaseGraph_1.BaseGraph {
        constructor(svg, nodes, edges) {
            super(svg, nodes, edges);
            let graph = this;
            d3.select(window).on("keydown", function () {
                graph.svgKeyDown.call(graph);
            })
                .on("keyup", function () {
                graph.svgKeyUp.call(graph);
            });
            svg.on("mousedown", function (d) { graph.svgMouseDown.call(graph, d); });
            svg.on("mouseup", function (d) { graph.svgMouseUp.call(graph, d); });
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
        nodeMouseDown(el) {
            d3.event.stopPropagation();
            this.state.mouseDownNode = el;
            if (d3.event.shiftKey) {
                this.state.shiftNodeDrag = d3.event.shiftKey;
                this.dragBond.attr("el", "M" + el.x + "," + el.y + "L" + el.x + "," + el.y);
                this.dragBond.classed("hidden", false);
                return;
            }
        }
        nodeMouseUp(d3Elem, el) {
            let graph = this;
            let state = graph.state;
            state.shiftNodeDrag = false;
            d3Elem.classed(this.bondClass, false);
            let mouseDownNode = state.mouseDownNode;
            if (!mouseDownNode)
                return;
            this.dragBond.classed("hidden", true);
            if (mouseDownNode !== el) {
                let newEdge = new BondGraphBond(mouseDownNode, el, true);
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
            }
            else {
                if (state.justDragged) {
                    state.justDragged = false;
                }
                else {
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
            state.mouseDownNode = null;
            return;
        }
        svgMouseDown() {
            this.state.graphMouseDown = true;
        }
        svgMouseUp() {
            let state = this.state;
            if (this.draggingElement) {
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());
                this.elements.push(new BondGraphElement(this.idct++, this.draggingElement, xycoords[0], xycoords[1]));
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
        dragmove(el) {
            if (this.state.shiftNodeDrag) {
                this.dragBond.attr("d", "M" + el.x + "," + el.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            }
            else {
                el.x += d3.event.dx;
                el.y += d3.event.dy;
                this.updateGraph();
            }
        }
        svgKeyUp() {
            this.state.lastKeyDown = -1;
        }
    }
    exports.SystemDiagram = SystemDiagram;
});
define("main", ["require", "exports", "types/BaseGraph", "types/SystemDiagram"], function (require, exports, BaseGraph_2, SystemDiagram_1) {
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
        let node1 = new BondGraphElement(0, "images/mechTrans/mass.svg", 50, 50);
        let node2 = new BondGraphElement(1, "images/mechTrans/ground.svg", 200, 200);
        var bondGraph = new BaseGraph_2.BaseGraph(bondGraphSVG, [node1, node2], [new BondGraphBond(node1, node2)]);
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
define("types/BondGraph", ["require", "exports", "types/BaseGraph"], function (require, exports, BaseGraph_3) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraph = void 0;
    class BondGraph extends BaseGraph_3.BaseGraph {
        constructor(svg, nodes, edges) {
            super(svg, nodes, edges);
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
            defs.append("svg:marker")
                .attr("id", "mark-end-arrow")
                .attr("viewBox", "0 -5 10 10")
                .attr("refX", 7)
                .attr("markerWidth", 3.5)
                .attr("markerHeight", 3.5)
                .attr("orient", "auto")
                .append("svg:path")
                .attr("d", "M0,-5L10,0L0,5");
        }
    }
    exports.BondGraph = BondGraph;
});
class BondGraphBond {
    constructor(source, target, hoverable) {
        this.hoverable = false;
        this.source = source;
        this.target = target;
        this.hoverable = hoverable;
    }
}
class BondGraphElement {
    constructor(id, img, x, y) {
        this.id = id;
        this.img = img;
        this.x = x;
        this.y = y;
    }
}
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
//# sourceMappingURL=build.js.map