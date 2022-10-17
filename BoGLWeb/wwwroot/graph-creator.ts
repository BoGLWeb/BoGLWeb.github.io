// @ts-ignore
document.onload = (async function (d3, saveAs, Blob) {
    "use strict";

    var draggingElement = null;

    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        draggingElement = null;
    });

    function makeElementSource(section, link) {
        const group = document.createElement('div');

        // @ts-ignore
        group.classList = "groupDiv";
        group.addEventListener("mousedown", function () {
            document.body.style.cursor = "grabbing";
            draggingElement = link;
        });

        section.appendChild(group);

        var box = document.createElement('div');
        // @ts-ignore
        box.classList = "box";
        group.appendChild(box);

        var image = document.createElement('img');
        image.src = link;
        image.draggable = false;
        // @ts-ignore
        image.classList = "elemImage";
        box.appendChild(image);
    }

    function makeSection(sectionName, images) {
        let sectionElem = document.getElementById(sectionName);
        images.forEach(image => makeElementSource(sectionElem, "images/" + sectionName + "/" + image + ".svg"));
    }

    function populateMenu() {
        // eventually want to pass these in from C#, which can likely access files easier
        makeSection("mechTrans", ["mass", "spring", "damper", "ground", "force_input", "gravity", "velocity_input"]);
        makeSection("mechRot", ["flywheel", "spring", "damper", "torque_input", "velocity_input"]);
        makeSection("transElem", ["lever", "pulley", "belt", "shaft", "gear", "gear_pair", "rack", "rack_pinion"]);
        makeSection("electrical", ["inductor", "capacitor", "resistor", "transformer", "junction_palette", "ground", "current_input", "voltage_input"]);
        makeSection("actuators", ["pm_motor", "vc_transducer"]);
    }

    // define graphcreator object
    let GraphCreator = function (svg, nodes, edges) {
        var thisGraph = this;
        thisGraph.idct = 0;

        thisGraph.nodes = nodes || [];
        thisGraph.edges = edges || [];

        thisGraph.state = {
            selectedNode: null,
            selectedEdge: null,
            mouseDownNode: null,
            mouseDownLink: null,
            justDragged: false,
            justScaleTransGraph: false,
            lastKeyDown: -1,
            shiftNodeDrag: false,
            selectedText: null
        };

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

        thisGraph.svg = svg;
        thisGraph.svgG = svg.append("g")
            .classed(thisGraph.consts.graphClass, true);
        var svgG = thisGraph.svgG;

        // displayed when dragging between nodes
        thisGraph.dragLine = svgG.append("svg:path")
            .attr("class", "link dragline hidden")
            .attr("d", "M0,0L0,0");

        // svg nodes and edges
        thisGraph.paths = svgG.append("g").selectAll("g");
        thisGraph.circles = svgG.append("g").selectAll("g");

        thisGraph.drag = d3.behavior.drag()
            .origin(function (d) {
                return { x: d.x, y: d.y };
            })
            .on("drag", function (args) {
                thisGraph.state.justDragged = true;
                thisGraph.dragmove.call(thisGraph, args);
            })
            .on("dragend", function () {
                // todo check if edge-mode is selected
            });

        // listen for key events
        d3.select(window).on("keydown", function () {
            thisGraph.svgKeyDown.call(thisGraph);
        })
            .on("keyup", function () {
                thisGraph.svgKeyUp.call(thisGraph);
            });
        svg.on("mousedown", function (d) { thisGraph.svgMouseDown.call(thisGraph, d); });
        svg.on("mouseup", function (d) { thisGraph.svgMouseUp.call(thisGraph, d); });

        // listen for dragging
        var dragSvg = d3.behavior.zoom()
            .on("zoom", function () {
                if (d3.event.sourceEvent.shiftKey) {
                    // TODO  the internal d3 state is still changing
                    return false;
                } else {
                    thisGraph.zoomed.call(thisGraph);
                }
                return true;
            })
            .on("zoomstart", function () {
                var ael = d3.select("#" + thisGraph.consts.activeEditId).node();
                if (ael) {
                    ael.blur();
                }
                if (!d3.event.sourceEvent.shiftKey) d3.select("body").style("cursor", "move");
            })
            .on("zoomend", function () {
                d3.select("body").style("cursor", "auto");
            });

        svg.call(dragSvg).on("dblclick.zoom", null);

        // listen for resize
        window.onresize = function () { thisGraph.updateWindow(svg); };
    };

    GraphCreator.prototype.setIdCt = function (idct) {
        this.idct = idct;
    };

    GraphCreator.prototype.consts = {
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

    /* PROTOTYPE FUNCTIONS */

    GraphCreator.prototype.dragmove = function (d) {
        var thisGraph = this;
        if (thisGraph.state.shiftNodeDrag) {
            thisGraph.dragLine.attr("d", "M" + d.x + "," + d.y + "L" + d3.mouse(thisGraph.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
        } else {
            d.x += d3.event.dx;
            d.y += d3.event.dy;
            thisGraph.updateGraph();
        }
    };

    // remove edges associated with a node
    GraphCreator.prototype.spliceLinksForNode = function (node) {
        var thisGraph = this,
            toSplice = thisGraph.edges.filter(function (l) {
                return (l.source === node || l.target === node);
            });
        toSplice.map(function (l) {
            thisGraph.edges.splice(thisGraph.edges.indexOf(l), 1);
        });
    };

    GraphCreator.prototype.replaceSelectEdge = function (d3Path, edgeData) {
        var thisGraph = this;
        d3Path.classed(thisGraph.consts.selectedClass, true);
        if (thisGraph.state.selectedEdge) {
            thisGraph.removeSelectFromEdge();
        }
        thisGraph.state.selectedEdge = edgeData;
    };

    GraphCreator.prototype.replaceSelectNode = function (d3Node, nodeData) {
        var thisGraph = this;
        d3Node.classed(this.consts.selectedClass, true);
        if (thisGraph.state.selectedNode) {
            thisGraph.removeSelectFromNode();
        }
        thisGraph.state.selectedNode = nodeData;
    };

    GraphCreator.prototype.removeSelectFromNode = function () {
        var thisGraph = this;
        thisGraph.circles.filter(function (cd) {
            return cd.id === thisGraph.state.selectedNode.id;
        }).classed(thisGraph.consts.selectedClass, false);
        thisGraph.state.selectedNode = null;
    };

    GraphCreator.prototype.removeSelectFromEdge = function () {
        var thisGraph = this;
        thisGraph.paths.filter(function (cd) {
            return cd === thisGraph.state.selectedEdge;
        }).classed(thisGraph.consts.selectedClass, false);
        thisGraph.state.selectedEdge = null;
    };

    GraphCreator.prototype.pathMouseDown = function (d3path, d) {
        var thisGraph = this,
            state = thisGraph.state;
        d3.event.stopPropagation();
        state.mouseDownLink = d;

        if (state.selectedNode) {
            thisGraph.removeSelectFromNode();
        }

        var prevEdge = state.selectedEdge;
        if (!prevEdge || prevEdge !== d) {
            thisGraph.replaceSelectEdge(d3path, d);
        } else {
            thisGraph.removeSelectFromEdge();
        }
    };

    // mousedown on node
    GraphCreator.prototype.nodeMouseDown = function (d3node, d) {
        var thisGraph = this,
            state = thisGraph.state;
        d3.event.stopPropagation();
        state.mouseDownNode = d;
        if (d3.event.shiftKey) {
            state.shiftNodeDrag = d3.event.shiftKey;
            // reposition dragged directed edge
            thisGraph.dragLine.classed("hidden", false)
                .attr("d", "M" + d.x + "," + d.y + "L" + d.x + "," + d.y);
            return;
        }
    };

    // mouseup on nodes
    GraphCreator.prototype.nodeMouseUp = function (d3node, d) {
        var thisGraph = this,
            state = thisGraph.state,
            consts = thisGraph.consts;
        // reset the states
        state.shiftNodeDrag = false;
        d3node.classed(consts.connectClass, false);

        var mouseDownNode = state.mouseDownNode;

        if (!mouseDownNode) return;

        thisGraph.dragLine.classed("hidden", true);

        if (mouseDownNode !== d) {
            // we"re in a different node: create new edge for mousedown edge and add to graph
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
        } else {
            // we"re in the same node
            if (state.justDragged) {
                // dragged, not clicked
                state.justDragged = false;
            } else {
                if (state.selectedEdge) {
                    thisGraph.removeSelectFromEdge();
                }
                var prevNode = state.selectedNode;

                if (!prevNode || prevNode.id !== d.id) {
                    thisGraph.replaceSelectNode(d3node, d);
                } else {
                    thisGraph.removeSelectFromNode();
                }
            }
        }
        state.mouseDownNode = null;
        return;

    }; // end of circles mouseup

    // mousedown on main svg
    GraphCreator.prototype.svgMouseDown = function () {
        this.state.graphMouseDown = true;
    };

    // mouseup on main svg
    GraphCreator.prototype.svgMouseUp = function () {
        var thisGraph = this,
            state = thisGraph.state;
        if (draggingElement) {
            document.body.style.cursor = "auto";
            var xycoords = d3.mouse(thisGraph.svgG.node()),
                d = { img: draggingElement, id: thisGraph.idct++, x: xycoords[0], y: xycoords[1], initialDrag: true };
            thisGraph.nodes.push(d);
            thisGraph.updateGraph();
        }
        if (state.justScaleTransGraph) {
            // dragged not clicked
            state.justScaleTransGraph = false;
        } else if (state.shiftNodeDrag) {
            // dragged from node
            state.shiftNodeDrag = false;
            thisGraph.dragLine.classed("hidden", true);
        }
        state.graphMouseDown = false;
    };

    // keydown on main svg
    GraphCreator.prototype.svgKeyDown = function () {
        var thisGraph = this,
            state = thisGraph.state,
            consts = thisGraph.consts;
        // make sure repeated key presses don"t register for each keydown
        if (state.lastKeyDown !== -1) return;

        state.lastKeyDown = d3.event.keyCode;
        var selectedNode = state.selectedNode,
            selectedEdge = state.selectedEdge;

        switch (d3.event.keyCode) {
            case consts.BACKSPACE_KEY:
            case consts.DELETE_KEY:
                d3.event.preventDefault();
                if (selectedNode) {
                    thisGraph.nodes.splice(thisGraph.nodes.indexOf(selectedNode), 1);
                    thisGraph.spliceLinksForNode(selectedNode);
                    state.selectedNode = null;
                    thisGraph.updateGraph();
                } else if (selectedEdge) {
                    thisGraph.edges.splice(thisGraph.edges.indexOf(selectedEdge), 1);
                    state.selectedEdge = null;
                    thisGraph.updateGraph();
                }
                break;
        }
    };

    GraphCreator.prototype.svgKeyUp = function () {
        this.state.lastKeyDown = -1;
    };

    // call to propagate changes to graph
    GraphCreator.prototype.updateGraph = function () {

        var thisGraph = this,
            consts = thisGraph.consts,
            state = thisGraph.state;

        thisGraph.paths = thisGraph.paths.data(thisGraph.edges, function (d) {
            return String(d.source.id) + "+" + String(d.target.id);
        });
        var paths = thisGraph.paths;
        // update existing paths
        paths.classed(consts.selectedClass, function (d) {
                return d === state.selectedEdge;
            })
            .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            });

        // add new paths
        paths.enter()
            .append("path")
            .classed("link", true)
            .attr("d", function (d) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            })
            .on("mousedown", function (d) {
                thisGraph.pathMouseDown.call(thisGraph, d3.select(this), d);
            }
            )
            .on("mouseup", function (d) {
                state.mouseDownLink = null;
            });

        // remove old links
        paths.exit().remove();

        // update existing nodes
        thisGraph.circles = thisGraph.circles.data(thisGraph.nodes, function (d) { return d.id; });
        thisGraph.circles.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });

        // add new nodes
        var newGs = thisGraph.circles.enter()
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

        // remove old nodes
        thisGraph.circles.exit().remove();
    };

    GraphCreator.prototype.zoomed = function () {
        this.state.justScaleTransGraph = true;
        d3.select("." + this.consts.graphClass)
            .attr("transform", "translate(" + d3.event.translate + ") scale(" + d3.event.scale + ")");
    };

    GraphCreator.prototype.updateWindow = function (svg) {
        var docEl = document.documentElement,
            bodyEl = document.getElementsByTagName("body")[0];
        var x = window.innerWidth || docEl.clientWidth || bodyEl.clientWidth;
        var y = window.innerHeight || docEl.clientHeight || bodyEl.clientHeight;
        svg.attr("width", x).attr("height", y);
    };

    var docEl = document.documentElement,
        bodyEl = document.getElementsByTagName("body")[0];

    var width = window.innerWidth || docEl.clientWidth || bodyEl.clientWidth,
        height = window.innerHeight || docEl.clientHeight || bodyEl.clientHeight;

    // initial node data
    var nodes = [];
    var edges = [];

    var svg = d3.select("#graph").append("svg")
        .attr("width", width)
        .attr("height", height);
    var menu = document.getElementById("graphMenu");
    var graph = new GraphCreator(svg, nodes, edges);
    populateMenu();
    graph.setIdCt(2);
    graph.updateGraph();
// @ts-ignore
})(window.d3, window.saveAs, window.Blob);