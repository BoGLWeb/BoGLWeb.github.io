define("types/baseGraph", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraph = void 0;
    class BaseGraph {
        constructor(svg, nodes, edges) {
            this.selectedClass = "selected";
            this.bondClass = "bond";
            this.idct = 0;
            this.state = new GraphState();
            this.draggingElement = null;
            this.nodes = nodes || [];
            this.edges = edges || [];
            svg.attr("id", "svg");
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
            this.dragLine = svgG.append("svg:path");
            this.dragLine.attr("class", "link dragline hidden")
                .attr("d", "M0,0L0,0");
            this.bonds = svgG.append("g").selectAll("g");
            this.elements = svgG.append("g").selectAll("g");
            let graph = this;
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
                .origin(function (d) {
                return { x: d.x, y: d.y };
            })
                .on("drag", function (args) {
                graph.state.justDragged = true;
                graph.dragmove.call(graph, args);
            });
        }
        get dragSvg() {
            var graph = this;
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
        setIdCt(idct) {
            this.idct = idct;
        }
        ;
        dragmove(d) {
            if (this.state.shiftNodeDrag) {
                this.dragLine.attr("d", "M" + d.x + "," + d.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            }
            else {
                d.x += d3.event.dx;
                d.y += d3.event.dy;
                this.updateGraph();
            }
        }
        ;
        spliceLinksForNode(node) {
            let toSplice = this.edges.filter(function (l) {
                return (l.source === node || l.target === node);
            });
            toSplice.map(function (l) {
                this.edges.splice(this.edges.indexOf(l), 1);
            });
        }
        ;
        replaceSelectEdge(d3Path, edgeData) {
            d3Path.classed(this.selectedClass, true);
            if (this.state.selectedEdge) {
                this.removeSelectFromEdge();
            }
            this.state.selectedEdge = edgeData;
        }
        ;
        replaceSelectNode(d3Node, nodeData) {
            d3Node.classed(this.selectedClass, true);
            if (this.state.selectedNode) {
                this.removeSelectFromNode();
            }
            this.state.selectedNode = nodeData;
        }
        ;
        removeSelectFromNode() {
            let graph = this;
            this.elements.filter(function (cd) { return cd.id === graph.state.selectedNode.id; }).classed(this.selectedClass, false);
            this.state.selectedNode = null;
        }
        ;
        removeSelectFromEdge() {
            var graph = this;
            graph.bonds.filter(function (cd) { return cd === graph.state.selectedEdge; }).classed(graph.selectedClass, false);
            this.state.selectedEdge = null;
        }
        ;
        pathMouseDown(d3path, d) {
            d3.event.stopPropagation();
            this.state.mouseDownLink = d;
            if (this.state.selectedNode) {
                this.removeSelectFromNode();
            }
            var prevEdge = this.state.selectedEdge;
            if (!prevEdge || prevEdge !== d) {
                this.replaceSelectEdge(d3path, d);
            }
            else {
                this.removeSelectFromEdge();
            }
        }
        ;
        nodeMouseDown(d) {
            d3.event.stopPropagation();
            this.state.mouseDownNode = d;
            if (d3.event.shiftKey) {
                this.state.shiftNodeDrag = d3.event.shiftKey;
                this.dragLine.classed("hidden", false)
                    .attr("d", "M" + d.x + "," + d.y + "L" + d.x + "," + d.y);
                return;
            }
        }
        ;
        nodeMouseUp(d3node, d) {
            var graph = this;
            let state = graph.state;
            state.shiftNodeDrag = false;
            d3node.classed(this.bondClass, false);
            var mouseDownNode = state.mouseDownNode;
            if (!mouseDownNode)
                return;
            this.dragLine.classed("hidden", true);
            if (mouseDownNode !== d) {
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
            }
            else {
                if (state.justDragged) {
                    state.justDragged = false;
                }
                else {
                    if (state.selectedEdge) {
                        this.removeSelectFromEdge();
                    }
                    var prevNode = state.selectedNode;
                    if (!prevNode || prevNode.id !== d.id) {
                        this.replaceSelectNode(d3node, d);
                    }
                    else {
                        this.removeSelectFromNode();
                    }
                }
            }
            state.mouseDownNode = null;
            return;
        }
        ;
        svgMouseDown() {
            this.state.graphMouseDown = true;
        }
        ;
        svgMouseUp() {
            let state = this.state;
            if (this.draggingElement) {
                document.body.style.cursor = "auto";
                var xycoords = d3.mouse(this.svgG.node());
                this.nodes.push(new BondGraphElement(this.idct++, this.draggingElement, xycoords[0], xycoords[1]));
                this.updateGraph();
            }
            if (state.justScaleTransGraph) {
                state.justScaleTransGraph = false;
            }
            else if (state.shiftNodeDrag) {
                state.shiftNodeDrag = false;
                this.dragLine.classed("hidden", true);
            }
            state.graphMouseDown = false;
        }
        ;
        svgKeyDown() {
            let state = this.state;
            if (state.lastKeyDown !== -1)
                return;
            state.lastKeyDown = d3.event.keyCode;
            var selectedNode = state.selectedNode, selectedEdge = state.selectedEdge;
            switch (d3.event.keyCode) {
                case this.BACKSPACE_KEY:
                case this.DELETE_KEY:
                    d3.event.preventDefault();
                    if (selectedNode) {
                        this.nodes.splice(this.nodes.indexOf(selectedNode), 1);
                        this.spliceLinksForNode(selectedNode);
                        state.selectedNode = null;
                        this.updateGraph();
                    }
                    else if (selectedEdge) {
                        this.edges.splice(this.edges.indexOf(selectedEdge), 1);
                        state.selectedEdge = null;
                        this.updateGraph();
                    }
                    break;
            }
        }
        ;
        svgKeyUp() {
            this.state.lastKeyDown = -1;
        }
        ;
        updateGraph() {
            var graph = this;
            this.bonds = this.bonds.data(this.edges, function (d) {
                return String(d.source.id) + "+" + String(d.target.id);
            });
            var paths = this.bonds;
            paths.classed(this.selectedClass, function (d) {
                return d === graph.state.selectedEdge;
            })
                .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            });
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
            paths.exit().remove();
            this.elements = this.elements.data(this.nodes, function (d) { return d.id.toString(); });
            this.elements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
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
            image.attr("href", function (d, i) { return d.img; })
                .attr("x", "-25px")
                .attr("y", "-25px")
                .attr("preserveAspectRatio", "xMidYMid meet")
                .attr("height", "50px")
                .attr("width", "50px");
            this.elements.exit().remove();
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
define("main", ["require", "exports", "types/baseGraph"], function (require, exports, baseGraph_1) {
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
        var svg = d3.select("#graph").append("svg");
        var graph = new baseGraph_1.BaseGraph(svg, [], []);
        graph.draggingElement = null;
        document.addEventListener("mouseup", function () {
            document.body.style.cursor = "auto";
            graph.draggingElement = null;
        });
        populateMenu(graph);
        graph.setIdCt(2);
        graph.updateGraph();
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
class BondGraphBond {
    constructor(source, target) {
        this.source = source;
        this.target = target;
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
        this.selectedNode = null;
        this.selectedEdge = null;
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