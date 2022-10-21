define("types/baseGraph", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraph = void 0;
    class BaseGraph {
        constructor(svg, nodes, edges) {
            this.state = {
                selectedNode: null,
                selectedEdge: null,
                mouseDownNode: null,
                mouseDownLink: null,
                justDragged: false,
                justScaleTransGraph: false,
                lastKeyDown: -1,
                shiftNodeDrag: false,
                selectedText: null,
                graphMouseDown: false
            };
            this.consts = {
                selectedClass: "selected",
                connectClass: "connect-node",
                circleGClass: "conceptG",
                graphClass: "graph",
                activeEditId: "active-editing",
                BACKSPACE_KEY: 8,
                DELETE_KEY: 46,
                ENTER_KEY: 13,
                nodeRadius: 50
            };
            this.draggingElement = null;
            var thisGraph = this;
            thisGraph.idct = 0;
            thisGraph.nodes = nodes || [];
            thisGraph.edges = edges || [];
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
            thisGraph.svg = svg;
            thisGraph.svgG = svg.append("g")
                .classed(thisGraph.consts.graphClass, true);
            var svgG = thisGraph.svgG;
            thisGraph.dragLine = svgG.append("svg:path");
            thisGraph.dragLine.attr("class", "link dragline hidden")
                .attr("d", "M0,0L0,0");
            thisGraph.paths = svgG.append("g").selectAll("g");
            thisGraph.nodeObjects = svgG.append("g").selectAll("g");
            d3.select(window).on("keydown", function () {
                thisGraph.svgKeyDown.call(thisGraph);
            })
                .on("keyup", function () {
                thisGraph.svgKeyUp.call(thisGraph);
            });
            svg.on("mousedown", function (d) { thisGraph.svgMouseDown.call(thisGraph, d); });
            svg.on("mouseup", function (d) { thisGraph.svgMouseUp.call(thisGraph, d); });
            svg.call(thisGraph.dragSvg).on("dblclick.zoom", null);
            window.onresize = function () { thisGraph.updateWindow(svg); };
        }
        get drag() {
            var thisGraph = this;
            return d3.behavior.drag()
                .origin(function (d) {
                return { x: d.x, y: d.y };
            })
                .on("drag", function (args) {
                thisGraph.state.justDragged = true;
                thisGraph.dragmove.call(thisGraph, args);
            })
                .on("dragend", function () {
            });
        }
        get dragSvg() {
            var thisGraph = this;
            return d3.behavior.zoom()
                .on("zoom", function () {
                thisGraph.zoomed.call(thisGraph);
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
            var thisGraph = this;
            if (thisGraph.state.shiftNodeDrag) {
                thisGraph.dragLine.attr("d", "M" + d.x + "," + d.y + "L" + d3.mouse(thisGraph.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            }
            else {
                d.x += d3.event.dx;
                d.y += d3.event.dy;
                thisGraph.updateGraph();
            }
        }
        ;
        spliceLinksForNode(node) {
            var thisGraph = this, toSplice = thisGraph.edges.filter(function (l) {
                return (l.source === node || l.target === node);
            });
            toSplice.map(function (l) {
                thisGraph.edges.splice(thisGraph.edges.indexOf(l), 1);
            });
        }
        ;
        replaceSelectEdge(d3Path, edgeData) {
            var thisGraph = this;
            d3Path.classed(thisGraph.consts.selectedClass, true);
            if (thisGraph.state.selectedEdge) {
                thisGraph.removeSelectFromEdge();
            }
            thisGraph.state.selectedEdge = edgeData;
        }
        ;
        replaceSelectNode(d3Node, nodeData) {
            var thisGraph = this;
            d3Node.classed(this.consts.selectedClass, true);
            if (thisGraph.state.selectedNode) {
                thisGraph.removeSelectFromNode();
            }
            thisGraph.state.selectedNode = nodeData;
        }
        ;
        removeSelectFromNode() {
            var thisGraph = this;
            thisGraph.nodeObjects.filter(function (cd) {
                return cd.id === thisGraph.state.selectedNode.id;
            }).classed(thisGraph.consts.selectedClass, false);
            thisGraph.state.selectedNode = null;
        }
        ;
        removeSelectFromEdge() {
            var thisGraph = this;
            thisGraph.paths.filter(function (cd) {
                return cd === thisGraph.state.selectedEdge;
            }).classed(thisGraph.consts.selectedClass, false);
            thisGraph.state.selectedEdge = null;
        }
        ;
        pathMouseDown(d3path, d) {
            var thisGraph = this, state = thisGraph.state;
            d3.event.stopPropagation();
            state.mouseDownLink = d;
            if (state.selectedNode) {
                thisGraph.removeSelectFromNode();
            }
            var prevEdge = state.selectedEdge;
            if (!prevEdge || prevEdge !== d) {
                thisGraph.replaceSelectEdge(d3path, d);
            }
            else {
                thisGraph.removeSelectFromEdge();
            }
        }
        ;
        nodeMouseDown(d3node, d) {
            var thisGraph = this, state = thisGraph.state;
            d3.event.stopPropagation();
            state.mouseDownNode = d;
            if (d3.event.shiftKey) {
                state.shiftNodeDrag = d3.event.shiftKey;
                thisGraph.dragLine.classed("hidden", false)
                    .attr("d", "M" + d.x + "," + d.y + "L" + d.x + "," + d.y);
                return;
            }
        }
        ;
        nodeMouseUp(d3node, d) {
            var thisGraph = this, state = thisGraph.state, consts = thisGraph.consts;
            state.shiftNodeDrag = false;
            d3node.classed(consts.connectClass, false);
            var mouseDownNode = state.mouseDownNode;
            if (!mouseDownNode)
                return;
            thisGraph.dragLine.classed("hidden", true);
            if (mouseDownNode !== d) {
                var newEdge = { source: mouseDownNode, target: d };
                var filtRes = thisGraph.paths.filter(function (d) {
                    if (d.source === newEdge.target && d.target === newEdge.source) {
                        thisGraph.edges.splice(thisGraph.edges.indexOf(d), 1);
                    }
                    return d.source === newEdge.source && d.target === newEdge.target;
                });
                if (!filtRes[0].length) {
                    thisGraph.edges.push(newEdge);
                    thisGraph.updateGraph();
                }
            }
            else {
                if (state.justDragged) {
                    state.justDragged = false;
                }
                else {
                    if (state.selectedEdge) {
                        thisGraph.removeSelectFromEdge();
                    }
                    var prevNode = state.selectedNode;
                    if (!prevNode || prevNode.id !== d.id) {
                        thisGraph.replaceSelectNode(d3node, d);
                    }
                    else {
                        thisGraph.removeSelectFromNode();
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
            var thisGraph = this, state = thisGraph.state;
            if (thisGraph.draggingElement) {
                document.body.style.cursor = "auto";
                var xycoords = d3.mouse(thisGraph.svgG.node()), d = { img: thisGraph.draggingElement, id: thisGraph.idct++, x: xycoords[0], y: xycoords[1], initialDrag: true };
                thisGraph.nodes.push(d);
                thisGraph.updateGraph();
            }
            if (state.justScaleTransGraph) {
                state.justScaleTransGraph = false;
            }
            else if (state.shiftNodeDrag) {
                state.shiftNodeDrag = false;
                thisGraph.dragLine.classed("hidden", true);
            }
            state.graphMouseDown = false;
        }
        ;
        svgKeyDown() {
            var thisGraph = this, state = thisGraph.state, consts = thisGraph.consts;
            if (state.lastKeyDown !== -1)
                return;
            state.lastKeyDown = d3.event.keyCode;
            var selectedNode = state.selectedNode, selectedEdge = state.selectedEdge;
            switch (d3.event.keyCode) {
                case consts.BACKSPACE_KEY:
                case consts.DELETE_KEY:
                    d3.event.preventDefault();
                    if (selectedNode) {
                        thisGraph.nodes.splice(thisGraph.nodes.indexOf(selectedNode), 1);
                        thisGraph.spliceLinksForNode(selectedNode);
                        state.selectedNode = null;
                        thisGraph.updateGraph();
                    }
                    else if (selectedEdge) {
                        thisGraph.edges.splice(thisGraph.edges.indexOf(selectedEdge), 1);
                        state.selectedEdge = null;
                        thisGraph.updateGraph();
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
            var thisGraph = this, consts = thisGraph.consts, state = thisGraph.state;
            thisGraph.paths = thisGraph.paths.data(thisGraph.edges, function (d) {
                return String(d.source.id) + "+" + String(d.target.id);
            });
            var paths = thisGraph.paths;
            paths.classed(consts.selectedClass, function (d) {
                return d === state.selectedEdge;
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
                thisGraph.pathMouseDown.call(thisGraph, d3.select(this), d);
            })
                .on("mouseup", function (d) {
                state.mouseDownLink = null;
            });
            paths.exit().remove();
            thisGraph.nodeObjects = thisGraph.nodeObjects.data(thisGraph.nodes, function (d) { return d.id; });
            thisGraph.nodeObjects.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            var newGs = thisGraph.nodeObjects.enter()
                .append("g");
            newGs.classed(consts.circleGClass, true)
                .attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; })
                .on("mouseover", function (d) {
                if (state.shiftNodeDrag) {
                    d3.select(this).classed(consts.connectClass, true);
                }
            })
                .on("mouseout", function (d) {
                d3.select(this).classed(consts.connectClass, false);
            })
                .on("mousedown", function (d) {
                thisGraph.nodeMouseDown.call(thisGraph, d3.select(this), d);
            })
                .on("mouseup", function (d) {
                thisGraph.nodeMouseUp.call(thisGraph, d3.select(this), d);
            })
                .call(thisGraph.drag);
            let group = newGs.append("g");
            group.attr("style", "fill:inherit")
                .attr("index", function (d, i) { return d.id; });
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
            thisGraph.nodeObjects.exit().remove();
        }
        ;
        zoomed() {
            this.state.justScaleTransGraph = true;
            d3.select("." + this.consts.graphClass)
                .attr("transform", "translate(" + d3.event.translate + ") scale(" + d3.event.scale + ")");
        }
        ;
        updateWindow(svg) {
            var docEl = document.documentElement, bodyEl = document.getElementsByTagName("body")[0];
            var x = window.innerWidth || docEl.clientWidth || bodyEl.clientWidth;
            var y = window.innerHeight || docEl.clientHeight || bodyEl.clientHeight;
            svg.attr("width", x).attr("height", y);
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
        console.log("Loading");
        var svg = d3.select("#graph").append("svg")
            .attr("width", width)
            .attr("height", height);
        var graph = new baseGraph_1.BaseGraph(svg, [], []);
        graph.draggingElement = null;
        document.addEventListener("mouseup", function () {
            document.body.style.cursor = "auto";
            graph.draggingElement = null;
        });
        var docEl = document.documentElement, bodyEl = document.getElementsByTagName("body")[0];
        var width = window.innerWidth || docEl.clientWidth || bodyEl.clientWidth, height = window.innerHeight || docEl.clientHeight || bodyEl.clientHeight;
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
//# sourceMappingURL=build.js.map