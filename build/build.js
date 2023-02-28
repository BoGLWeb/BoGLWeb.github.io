var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
define("types/elements/GraphElement", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphElement = void 0;
    class GraphElement {
        constructor(id, x, y) {
            this.id = id;
            this.x = x;
            this.y = y;
        }
        copy(id) {
            return new GraphElement(id, this.x, this.y);
        }
    }
    exports.GraphElement = GraphElement;
});
define("types/bonds/GraphBond", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphBond = void 0;
    class GraphBond {
        constructor(source, target, velocity = 0) {
            this.source = source;
            this.target = target;
            this.velocity = velocity;
        }
        copy(source, target) {
            return new GraphBond(source, target, this.velocity);
        }
    }
    exports.GraphBond = GraphBond;
});
define("types/bonds/BondGraphBond", ["require", "exports", "types/bonds/GraphBond"], function (require, exports, GraphBond_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraphBond = void 0;
    class BondGraphBond extends GraphBond_1.GraphBond {
        constructor(source, target, causalStroke, causalStrokeDirection, hasDirection, velocity = 0) {
            super(source, target, velocity);
            this.causalStroke = causalStroke;
            this.causalStrokeDirection = causalStrokeDirection;
            this.hasDirection = hasDirection;
        }
    }
    exports.BondGraphBond = BondGraphBond;
});
define("types/bonds/GraphBondID", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.GraphBondID = void 0;
    class GraphBondID {
        constructor(source, target, velID = 0) {
            this.source = source;
            this.target = target;
            this.velID = velID;
        }
        checkEquality(source, target) {
            return source == this.source && target == this.target;
        }
    }
    exports.GraphBondID = GraphBondID;
});
define("types/elements/BondGraphElement", ["require", "exports", "types/elements/GraphElement"], function (require, exports, GraphElement_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraphElement = void 0;
    class BondGraphElement extends GraphElement_1.GraphElement {
        constructor(id, label, x, y) {
            super(id, x, y);
            this.labelSize = null;
            this.label = label;
        }
    }
    exports.BondGraphElement = BondGraphElement;
});
define("types/graphs/BaseGraph", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraph = void 0;
    class BaseGraph {
        constructor(nodes, edges) {
            this.nodes = nodes;
            this.edges = edges;
        }
    }
    exports.BaseGraph = BaseGraph;
});
define("types/graphs/BondGraph", ["require", "exports", "types/graphs/BaseGraph"], function (require, exports, BaseGraph_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraph = void 0;
    class BondGraph extends BaseGraph_1.BaseGraph {
        constructor(nodes, edges) {
            super(nodes, edges);
            this.nodes = nodes;
            this.edges = edges;
        }
    }
    exports.BondGraph = BondGraph;
});
define("types/elements/Category", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.Category = void 0;
    class Category {
        constructor(id, name, folderName) {
            this.id = id;
            this.folderName = folderName;
            this.name = name;
        }
    }
    exports.Category = Category;
});
define("types/elements/ElementType", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.ElementType = void 0;
    class ElementType {
        constructor(id, name, category, image, allowedModifiers, velocityAllowed, maxConnections = Number.MAX_SAFE_INTEGER, isMultiElement = false) {
            this.id = id;
            this.name = name;
            this.category = category;
            this.allowedModifiers = allowedModifiers;
            this.image = image;
            this.velocityAllowed = velocityAllowed;
            this.maxConnections = maxConnections;
            this.isMultiElement = isMultiElement;
        }
    }
    exports.ElementType = ElementType;
});
define("types/elements/Modifier", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.Modifier = void 0;
    class Modifier {
        constructor(id, name) {
            this.id = id;
            this.name = name;
        }
    }
    exports.Modifier = Modifier;
});
define("types/elements/SystemDiagramElement", ["require", "exports", "types/elements/GraphElement"], function (require, exports, GraphElement_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagramElement = void 0;
    class SystemDiagramElement extends GraphElement_2.GraphElement {
        constructor(id, type, x, y, velocity, modifiers) {
            super(id, x, y);
            this.modifiers = modifiers;
            this.velocity = velocity;
            this.type = type;
        }
        copy(id, offset = 0) {
            return new SystemDiagramElement(id, this.type, this.x + offset, this.y + offset, this.velocity, this.modifiers);
        }
    }
    exports.SystemDiagramElement = SystemDiagramElement;
});
define("types/elements/MultiElementType", ["require", "exports", "types/elements/ElementType"], function (require, exports, ElementType_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.MultiElementType = void 0;
    class MultiElementType extends ElementType_1.ElementType {
        constructor(id, name, category, image, allowedModifiers, velocityAllowed, subElements, subElementEdges, offsets, maxConnections = Number.MAX_SAFE_INTEGER) {
            super(id, name, category, image, allowedModifiers, velocityAllowed, maxConnections, true);
            this.subElements = subElements;
            this.subElementEdges = subElementEdges;
            this.offsets = offsets;
        }
    }
    exports.MultiElementType = MultiElementType;
});
define("types/elements/ElementNamespace", ["require", "exports", "types/elements/Category", "types/elements/ElementType", "types/elements/Modifier", "types/elements/MultiElementType"], function (require, exports, Category_1, ElementType_2, Modifier_1, MultiElementType_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.ElementNamespace = void 0;
    var ElementNamespace;
    (function (ElementNamespace) {
        ElementNamespace.categories = [
            new Category_1.Category(0, "Basic Mechanical Translation", "mechTrans"),
            new Category_1.Category(1, "Basic Mechanical Rotation", "mechRot"),
            new Category_1.Category(2, "Transmission Elements", "transElem"),
            new Category_1.Category(3, "Electrical", "electrical"),
            new Category_1.Category(4, "Actuators", "actuators")
        ];
        ElementNamespace.modifiers = [
            new Modifier_1.Modifier(0, "Mass"),
            new Modifier_1.Modifier(1, "Inertia"),
            new Modifier_1.Modifier(2, "Stiffness"),
            new Modifier_1.Modifier(3, "Friction"),
            new Modifier_1.Modifier(4, "Damping"),
            new Modifier_1.Modifier(5, "Parallel"),
            new Modifier_1.Modifier(6, "Tooth Wear")
        ];
        ElementNamespace.elementTypes = [
            new ElementType_2.ElementType(0, "Mass", 0, "mass", [3], true),
            new ElementType_2.ElementType(1, "Spring", 0, "spring", [5], true, 2),
            new ElementType_2.ElementType(2, "Damper", 0, "damper", [5], true, 2),
            new ElementType_2.ElementType(3, "Ground", 0, "mech_ground", [], false),
            new ElementType_2.ElementType(4, "Force Input", 0, "force_input", [], true),
            new ElementType_2.ElementType(5, "Gravity", 0, "gravity", [], false),
            new ElementType_2.ElementType(6, "Velocity Input", 0, "velocity_input", [], true),
            new ElementType_2.ElementType(7, "Flywheel", 1, "flywheel", [], true),
            new ElementType_2.ElementType(8, "Rotational Spring", 1, "spring", [5], true, 2),
            new ElementType_2.ElementType(9, "Rotational Damper", 1, "damper", [5], true, 2),
            new ElementType_2.ElementType(10, "Torque Input", 1, "torque_input", [], true),
            new ElementType_2.ElementType(11, "Velocity Input", 1, "velocity_input", [], true),
            new ElementType_2.ElementType(12, "Lever", 2, "lever", [3, 1], true),
            new ElementType_2.ElementType(13, "Pulley", 2, "pulley", [3, 1], true),
            new ElementType_2.ElementType(14, "Belt", 2, "belt", [5, 2, 4], true),
            new ElementType_2.ElementType(15, "Shaft", 2, "shaft", [2, 4], true, 2),
            new ElementType_2.ElementType(16, "Gear", 2, "gear", [3, 1, 6], true),
            new MultiElementType_1.MultiElementType(17, "Gear Pair", 2, "gear_pair", [], false, [16, 16], [[0, 1]], [[0, 0], [100, 0]]),
            new ElementType_2.ElementType(18, "Rack", 2, "rack", [3, 1, 6, 0], true),
            new MultiElementType_1.MultiElementType(19, "Rack Pinion", 2, "rack_pinion", [], false, [16, 18], [[0, 1]], [[0, 0], [0, 100]]),
            new ElementType_2.ElementType(20, "Inductor", 3, "inductor", [], false, 2),
            new ElementType_2.ElementType(21, "Capacitor", 3, "capacitor", [], false, 2),
            new ElementType_2.ElementType(22, "Resistor", 3, "resistor", [], false, 2),
            new ElementType_2.ElementType(23, "Transformer", 3, "transformer", [], false, 4),
            new ElementType_2.ElementType(24, "Junction Palette", 3, "junction_palette", [], false, 4),
            new ElementType_2.ElementType(25, "Ground", 3, "elec_ground", [], false, 2),
            new ElementType_2.ElementType(26, "Current Input", 3, "current_input", [], false),
            new ElementType_2.ElementType(27, "Voltage Input", 3, "voltage_input", [], false),
            new ElementType_2.ElementType(28, "PM Motor", 4, "pm_motor", [], false),
            new ElementType_2.ElementType(29, "VC Transducer", 4, "vc_transducer", [], false),
            new ElementType_2.ElementType(30, "Grounded Pulley", 2, "pulley_grounded", [3, 1], true)
        ];
        ElementNamespace.mtCompatibilityGroup = new Set([0, 1, 2, 3, 4, 5, 6, 18, 12, 13, 14]);
        ElementNamespace.mrCompatibilityGroup = new Set([8, 9, 7, 12, 13, 15, 14, 10, 12, 11, 16, 18, 28]);
        ElementNamespace.eCompatibilityGroup = new Set([21, 22, 25, 24, 23, 20, 27, 26, 28]);
        ElementNamespace.oCompatibilityGroup = new Set([29, 28]);
        function isCompatible(e1, e2, graph) {
            if (e1 === null || e2 === null || e1.id === e2.id)
                return false;
            let mtCompatible = ElementNamespace.mtCompatibilityGroup.has(e1.type) && ElementNamespace.mtCompatibilityGroup.has(e2.type);
            let mrCompatible = ElementNamespace.mrCompatibilityGroup.has(e1.type) && ElementNamespace.mrCompatibilityGroup.has(e2.type);
            let eCompatible = ElementNamespace.eCompatibilityGroup.has(e1.type) && ElementNamespace.eCompatibilityGroup.has(e2.type);
            let oCompatible = ElementNamespace.oCompatibilityGroup.has(e1.type) && ElementNamespace.oCompatibilityGroup.has(e2.type);
            let maxSourceBonds = ElementNamespace.elementTypes[e1.type].maxConnections;
            let maxTargetBonds = ElementNamespace.elementTypes[e2.type].maxConnections;
            let numTargetBonds = graph.bonds.filter(b => b.target.id == e2.id || b.source.id == e2.id).length;
            let numSourceBonds = graph.bonds.filter(b => b.target.id == e1.id || b.source.id == e1.id).length;
            let edgesLikeThisCount = graph.bonds.filter(b => (b.target.id == e1.id && b.source.id == e2.id) || (b.target.id == e2.id && b.source.id == e1.id)).length;
            return (mtCompatible || mrCompatible || eCompatible || oCompatible) && (numSourceBonds < maxSourceBonds) && (numTargetBonds < maxTargetBonds) && (edgesLikeThisCount === 0);
        }
        ElementNamespace.isCompatible = isCompatible;
    })(ElementNamespace = exports.ElementNamespace || (exports.ElementNamespace = {}));
});
define("types/graphs/SystemDiagram", ["require", "exports", "types/graphs/BaseGraph"], function (require, exports, BaseGraph_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagram = void 0;
    class SystemDiagram extends BaseGraph_2.BaseGraph {
        constructor(nodes, edges) {
            super(nodes, edges);
            this.nodes = nodes;
        }
    }
    exports.SystemDiagram = SystemDiagram;
});
define("types/display/SystemDiagramDisplay", ["require", "exports", "types/bonds/GraphBond", "types/elements/ElementNamespace", "types/elements/SystemDiagramElement", "types/display/BaseGraphDisplay", "backendManager"], function (require, exports, GraphBond_2, ElementNamespace_1, SystemDiagramElement_1, BaseGraphDisplay_1, backendManager_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SystemDiagramDisplay = void 0;
    class SystemDiagramDisplay extends BaseGraphDisplay_1.BaseGraphDisplay {
        constructor(svg, systemDiagram) {
            super(svg, systemDiagram);
            this.edgeOrigin = null;
            this.velocityMap = {
                0: "",
                1: "⮢",
                2: "⮣",
                3: "⮥",
                4: "⮧",
                5: "⮡",
                6: "⮠",
                7: "⮦",
                8: "⮤"
            };
            this.velocityOffsets = [[-15, -37], [-5, -37], [30, -5], [30, 7], [18, 40], [3, 40], [-30, 10], [-30, 0]];
            this.justClickedEdge = false;
            this.selectedElements = [];
            this.copiedElements = [];
            this.copiedBonds = [];
            this.ctrlPressed = false;
            this.highestElemId = systemDiagram.nodes.length + 1;
            this.edgeCircle = this.svgG.append("circle");
            this.edgeCircle.attr("r", "5")
                .attr("fill", "green")
                .attr("style", "cursor: pointer; display: none;");
            this.rejectX = this.svgG.append("path");
            this.rejectX
                .attr("d", d3.svg.symbol().type("cross").size(100))
                .style("fill", "red")
                .style("display", "none");
        }
        getSelection() {
            return [].concat(this.selectedElements).concat(this.selectedBonds);
        }
        moveCircle(e) {
            d3.event.stopPropagation();
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
            this.rejectX.attr("transform", "translate(" + (e.x + coords[0]) + "," + (e.y + coords[1]) + ") rotate(45)");
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
        setEdgeMarkerVisible(e) {
            if (!this.edgeOrigin || ElementNamespace_1.ElementNamespace.isCompatible(this.edgeOrigin, e, this)) {
                this.edgeCircle.style("display", "block");
                this.rejectX.style("display", "none");
            }
            else {
                this.rejectX.style("display", "block");
                this.edgeCircle.style("display", "none");
            }
        }
        renderElements(newElements) {
            let graph = this;
            newElements.classed("boglElem", true)
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
                graph.moveSelectionRect();
                graph.moveCircle.call(graph, e);
            })
                .on("mouseenter", function (e) {
                graph.setEdgeMarkerVisible.call(graph, e);
            })
                .on("mouseleave", function () {
                graph.edgeCircle.style("display", "none");
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
            image.attr("href", function (d) { return "/images/elements/" + ElementNamespace_1.ElementNamespace.elementTypes[d.type].image + ".svg"; })
                .classed("hoverImg", true)
                .attr("x", "-25px")
                .attr("y", "-25px")
                .attr("preserveAspectRatio", "xMidYMid meet")
                .attr("height", "50px")
                .attr("width", "50px");
            let asterisk = newElements.append("text");
            asterisk
                .text("*")
                .attr("x", "16")
                .attr("y", "-13")
                .style("font-size", "30px")
                .style("display", e => {
                return e.modifiers.length > 0 ? "block" : "none";
            });
            group.selectAll("text").html(null);
            group.each(function (d) {
                if (d.velocity != 0) {
                    let text = group.append("text");
                    text.text((d) => graph.velocityMap[d.velocity])
                        .each(function (d) {
                        if (d.velocity != 0) {
                            let velocityClass = "";
                            if (d.velocity == 1 || d.velocity == 2) {
                                velocityClass = "topVelocity";
                            }
                            else if (d.velocity == 3 || d.velocity == 4) {
                                velocityClass = "rightVelocity";
                            }
                            else if (d.velocity == 5 || d.velocity == 6) {
                                velocityClass = "bottomVelocity";
                            }
                            else {
                                velocityClass = "leftVelocity";
                            }
                            this.classList.add("velocityArrow");
                            this.classList.add(velocityClass);
                        }
                    })
                        .attr("x", (d) => {
                        return d.velocity != 0 ? graph.velocityOffsets[d.velocity - 1][0] : 0;
                    })
                        .attr("y", (d) => {
                        return d.velocity != 0 ? graph.velocityOffsets[d.velocity - 1][1] : 0;
                    });
                }
            });
            image.on("mouseenter", function () {
                graph.edgeCircle.style("display", "none");
            })
                .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d);
            })
                .on("mouseleave", function (e) {
                graph.setEdgeMarkerVisible.call(graph, e);
            });
            box.on("mousemove", function (e) {
                graph.moveSelectionRect();
                graph.moveCircle.call(graph, e);
            })
                .on("mouseenter", function (e) {
                graph.setEdgeMarkerVisible.call(graph, e);
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
            let graph = this;
            paths.classed("hoverablePath", true);
            this.svgG.selectAll("g:not(.boglElem) > g > .velocityArrow").remove();
            paths.each((e, i) => {
                if (e.velocity != 0) {
                    let velocityClass = "";
                    let xOffset = 0;
                    let yOffset = 0;
                    let mult = Math.abs(Math.cos((Math.atan2(e.source.y - e.target.y, e.target.x - e.source.x) + Math.PI) % (2 * Math.PI)));
                    let v = e.velocity;
                    if (v == 2 || v == 3) {
                        velocityClass = "topVelocity";
                        yOffset = -7 * mult;
                        xOffset = -3;
                    }
                    else if (v == 4) {
                        velocityClass = "rightVelocity";
                        yOffset = 7 * mult;
                        xOffset = 0;
                    }
                    else if (v == 5) {
                        velocityClass = "rightVelocityMath";
                        yOffset = 7 * mult;
                        xOffset = -3;
                    }
                    else if (v == 6) {
                        velocityClass = "bottomVelocityMath";
                        yOffset = 7 * mult;
                        xOffset = 3;
                    }
                    else if (v == 7) {
                        velocityClass = "bottomVelocity";
                        yOffset = 7 * mult;
                        xOffset = 0;
                    }
                    else if (v == 1 || v == 8) {
                        velocityClass = "leftVelocity";
                        yOffset = -7 * mult;
                        xOffset = 0;
                    }
                    d3.select(paths[0][i].parentNode).append("text").classed("velocityArrow " + velocityClass, true)
                        .text(graph.velocityMap[e.velocity]).attr("x", (e.target.x - e.source.x) / 2 + e.source.x + xOffset).attr("y", (e.target.y - e.source.y) / 2 + e.source.y + yOffset);
                }
            });
        }
        updateModifierMenu() {
            if ((this.selectedElements.length > 0 || this.selectedBonds.length > 0) && this.selectedElements.length > 0) {
                let allAllowedModifiers = [];
                let selectedModifiers = [0, 0, 0, 0, 0, 0, 0];
                for (const e of this.selectedElements) {
                    allAllowedModifiers = allAllowedModifiers.concat(ElementNamespace_1.ElementNamespace.elementTypes[e.type].allowedModifiers);
                    e.modifiers.forEach(m => selectedModifiers[m]++);
                }
                selectedModifiers = selectedModifiers.map(m => {
                    if (m == this.selectedElements.length) {
                        return 2;
                    }
                    else if (m > 0) {
                        return 1;
                    }
                    return 0;
                });
                DotNet.invokeMethodAsync("BoGLWeb", "SetCheckboxes", selectedModifiers);
                DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", [...new Set(allAllowedModifiers)]);
            }
            else {
                DotNet.invokeMethodAsync("BoGLWeb", "ClearCheckboxes");
                DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", []);
            }
        }
        updateVelocityMenu() {
            DotNet.invokeMethodAsync("BoGLWeb", "SetVelocityDisabled", this.selectedElements.length == 0 && this.selectedBonds.length == 0);
            let velocities = [];
            for (const el of this.getSelection()) {
                if (velocities.find(e => e == el.velocity) == null) {
                    velocities.push(el.velocity);
                }
            }
            DotNet.invokeMethodAsync("BoGLWeb", "SetVelocity", velocities);
        }
        pathMouseDown(bond) {
            this.justClickedEdge = true;
            super.pathMouseDown(bond);
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
            if (this.handleAreaSelectionEnd())
                return;
            if (this.edgeOrigin && el !== this.edgeOrigin) {
                this.addBond(this.edgeOrigin, el);
            }
            else if (!this.edgeOrigin) {
                this.setFollowingEdge(el);
                d3.event.stopPropagation();
            }
        }
        nodeMouseDown(el) {
            d3.event.stopPropagation();
            this.mouseDownNode = el;
            if (this.edgeOrigin == el) {
                this.setFollowingEdge(null);
            }
            this.justDragged = false;
        }
        setSelection(elList, bondList) {
            this.selectedElements = elList;
            this.selectedBonds = bondList;
        }
        addBond(source, target) {
            let isCompatible = ElementNamespace_1.ElementNamespace.isCompatible(source, target, this);
            if (isCompatible) {
                let bond = new GraphBond_2.GraphBond(source, target);
                this.bonds.push(bond);
                let selectedElements = this.selectedElements;
                let selectedBonds = this.selectedBonds;
                this.setSelection([], [bond]);
                this.setFollowingEdge(null);
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [JSON.stringify(bond)], ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
            }
            else {
                this.setFollowingEdge(null);
                this.updateGraph();
            }
            this.updateMenus();
        }
        nodeMouseUp(el) {
            d3.event.stopPropagation();
            this.mouseDownNode = null;
            if (this.handleAreaSelectionEnd())
                return;
            if (this.edgeOrigin !== el && this.edgeOrigin !== null) {
                this.addBond(this.edgeOrigin, el);
            }
            else {
                if (!this.justDragged) {
                    let addEl = [];
                    let removeEl = [];
                    let removeEdges = [];
                    if (d3.event.ctrlKey || d3.event.metaKey) {
                        if (this.selectionContains(el)) {
                            this.removeFromSelection(el, false);
                            removeEl = [el];
                        }
                        else {
                            this.addToSelection(el, false);
                            addEl = [el];
                        }
                    }
                    else {
                        if (!this.selectionContains(el)) {
                            addEl = [el];
                            removeEl = this.selectedElements;
                            removeEdges = this.selectedBonds;
                            this.setSelection([el], []);
                        }
                    }
                    this.updateGraph();
                    DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(addEl.concat([])), ...this.listToIDObjects(removeEl.concat(removeEdges)));
                    this.updateMenus();
                }
            }
            this.justDragged = false;
        }
        updateMenus() {
            this.updateModifierMenu();
            this.updateVelocityMenu();
            this.updateTopMenu();
        }
        svgMouseUp() {
            this.setFollowingEdge(null);
            let selectedElements = this.selectedElements;
            let selectedBonds = this.selectedBonds;
            if (this.justClickedEdge) {
                this.justClickedEdge = false;
            }
            else if (this.draggingElement != null) {
                let newElementSelection;
                let newBondSelection;
                if (ElementNamespace_1.ElementNamespace.elementTypes[this.draggingElement].isMultiElement) {
                    document.body.style.cursor = "auto";
                    let xycoords = d3.mouse(this.svgG.node());
                    let multiElementType = ElementNamespace_1.ElementNamespace.elementTypes[this.draggingElement];
                    let subElementList = [];
                    let subBondList = [];
                    for (let i = 0; i < multiElementType.subElements.length; i++) {
                        let subElementType = multiElementType.subElements[i];
                        let subElementOffset = multiElementType.offsets[i];
                        let element = new SystemDiagramElement_1.SystemDiagramElement(this.highestElemId++, subElementType, xycoords[0] + subElementOffset[0], xycoords[1] + subElementOffset[1], 0, []);
                        subElementList.push(element);
                        this.elements.push(subElementList[i]);
                        this.addToSelection(element, false);
                    }
                    for (let i = 0; i < multiElementType.subElementEdges.length; i++) {
                        let element1 = subElementList[multiElementType.subElementEdges[i][0]];
                        let element2 = subElementList[multiElementType.subElementEdges[i][1]];
                        let bond = new GraphBond_2.GraphBond(element1, element2, 0);
                        this.bonds.push(bond);
                        this.addToSelection(bond, false);
                        subBondList.push(bond);
                    }
                    newElementSelection = subElementList;
                    newBondSelection = subBondList;
                }
                else {
                    document.body.style.cursor = "auto";
                    let xycoords = d3.mouse(this.svgG.node());
                    let element = new SystemDiagramElement_1.SystemDiagramElement(this.highestElemId++, this.draggingElement, xycoords[0], xycoords[1], 0, []);
                    this.elements.push(element);
                    newElementSelection = [element];
                    newBondSelection = [];
                }
                this.setSelection(newElementSelection, newBondSelection);
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [].concat(newElementSelection).concat(newBondSelection).map(e => JSON.stringify(e)), ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
                this.updateMenus();
            }
            else if (!this.justScaleTransGraph) {
                this.setSelection([], []);
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), [], [], ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)));
                this.updateMenus();
            }
            if (this.justScaleTransGraph) {
                this.justScaleTransGraph = false;
            }
        }
        checkCtrlCombo(a) {
            return (d3.event.keyCode == a && this.ctrlPressed) || (this.ctrlPressed && this.lastKeyDown == a);
        }
        copySelection() {
            this.copiedElements = this.selectedElements.map(e => e.copy(this.highestElemId++, 75));
            this.copiedBonds = this.selectedBonds.filter(b => this.selectionContains(b.source) && this.selectionContains(b.target))
                .map(b => b.copy(this.copiedElements[this.selectedElements.findIndex(a => a.id == b.source.id)], this.copiedElements[this.selectedElements.findIndex(a => a.id == b.target.id)]));
            DotNet.invokeMethodAsync("BoGLWeb", "SetHasCopied", true);
        }
        deleteSelection(needsConfirmation = true) {
            return __awaiter(this, void 0, void 0, function* () {
                let result;
                let graph = this;
                if (needsConfirmation) {
                    result = yield DotNet.invokeMethodAsync("BoGLWeb", "showDeleteConfirmationModal", this.getSelection().length > 1);
                }
                if (!needsConfirmation || result) {
                    let splicedBonds = [];
                    for (let e of this.selectedBonds) {
                        this.bonds = this.bonds.filter(bond => bond != e);
                    }
                    for (let e of this.selectedElements) {
                        let toSplice = this.bonds.filter(function (l) {
                            return (l.source === e || l.target === e);
                        });
                        toSplice.forEach(function (l) {
                            splicedBonds.push(l);
                            graph.bonds.splice(graph.bonds.indexOf(l), 1);
                        });
                        this.updateVelocityMenu();
                        this.elements = this.elements.filter(el => el != e);
                    }
                    let selectionStrings = this.getSelection().map(e => JSON.stringify(e));
                    this.setSelection([], []);
                    this.updateGraph();
                    DotNet.invokeMethodAsync("BoGLWeb", "URDeleteSelection", selectionStrings, splicedBonds.map(e => JSON.stringify(e)));
                    this.updateMenus();
                }
            });
        }
        pasteSelection() {
            let selectedElements = this.selectedElements;
            let selectedBonds = this.selectedBonds;
            this.elements = this.elements.concat(this.copiedElements);
            this.bonds = this.bonds.concat(this.copiedBonds);
            this.setSelection(this.copiedElements, this.copiedBonds);
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [].concat(this.copiedElements).concat(this.copiedBonds).map(e => JSON.stringify(e)), ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
            this.copySelection();
            this.updateMenus();
        }
        svgKeyDown() {
            return __awaiter(this, void 0, void 0, function* () {
                let graph = this;
                if (this.lastKeyDown == d3.event.keyCode)
                    return;
                if (!this.ctrlPressed) {
                    this.ctrlPressed = d3.event.keyCode == this.CTRL_KEY;
                }
                this.lastKeyDown = d3.event.keyCode;
                switch (d3.event.keyCode) {
                    case this.BACKSPACE_KEY:
                    case this.DELETE_KEY:
                        d3.event.preventDefault();
                        this.deleteSelection();
                        break;
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
                if (this.checkCtrlCombo(this.A_KEY)) {
                    DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...graph.listToIDObjects([].concat(this.elements.filter(e => !this.selectedElements.includes(e))).concat(this.bonds.filter(e => !this.selectedBonds.includes(e)))), [], []);
                    this.setSelection(this.elements, this.bonds);
                    this.updateGraph();
                    this.updateMenus();
                }
                else if (this.checkCtrlCombo(this.C_KEY)) {
                    this.copySelection();
                }
                else if (this.checkCtrlCombo(this.X_KEY)) {
                    this.copySelection();
                    this.deleteSelection();
                }
                else if (this.checkCtrlCombo(this.V_KEY)) {
                    this.pasteSelection();
                }
                else if (this.checkCtrlCombo(this.Z_KEY)) {
                    backendManager_1.backendManager.getBackendManager().handleUndoRedo(true);
                }
                else if (this.checkCtrlCombo(this.Y_KEY)) {
                    backendManager_1.backendManager.getBackendManager().handleUndoRedo(false);
                }
            });
        }
        svgMouseMove() {
            this.edgeCircle.style("display", "none");
            this.rejectX.style("display", "none");
        }
        svgKeyUp() {
            if (d3.event.keyCode == this.CTRL_KEY) {
                this.ctrlPressed = false;
            }
            this.lastKeyDown = -1;
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
        dragmoveEdge() {
            if (this.edgeOrigin) {
                this.dragBond.attr("d", "M" + this.edgeOrigin.x + "," + this.edgeOrigin.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            }
        }
        zoomed() {
            if (!this.edgeOrigin) {
                this.justScaleTransGraph = true;
                if (this.prevScale !== d3.event.scale || d3.event.sourceEvent.buttons == 2) {
                    this.changeScale(d3.event.translate[0], d3.event.translate[1], d3.event.scale);
                }
            }
        }
        ;
    }
    exports.SystemDiagramDisplay = SystemDiagramDisplay;
});
define("types/display/BaseGraphDisplay", ["require", "exports", "types/bonds/GraphBond", "types/elements/GraphElement", "types/display/SystemDiagramDisplay"], function (require, exports, GraphBond_3, GraphElement_3, SystemDiagramDisplay_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BaseGraphDisplay = void 0;
    class BaseGraphDisplay {
        constructor(svg, baseGraph) {
            this.selectedClass = "selected";
            this.bondClass = "bond";
            this.BACKSPACE_KEY = 8;
            this.DELETE_KEY = 46;
            this.ENTER_KEY = 13;
            this.A_KEY = 65;
            this.C_KEY = 67;
            this.X_KEY = 88;
            this.V_KEY = 86;
            this.Z_KEY = 90;
            this.Y_KEY = 89;
            this.CTRL_KEY = 17;
            this.ARROW_LEFT = 37;
            this.ARROW_UP = 38;
            this.ARROW_RIGHT = 39;
            this.ARROW_DOWN = 40;
            this.PAN_SPEED = 2.0;
            this.dragAllowed = false;
            this.prevScale = 1;
            this.initXPos = null;
            this.initYPos = null;
            this.svgX = 0;
            this.svgY = 0;
            this.draggingElement = null;
            this.selectedElements = [];
            this.selectedBonds = [];
            this.mouseDownNode = null;
            this.justDragged = false;
            this.justScaleTransGraph = false;
            this.lastKeyDown = -1;
            this.highestElemId = 0;
            this.elementsBeforeDrag = null;
            this.dragXOffset = 0;
            this.dragYOffset = 0;
            this.startedSelectionDrag = false;
            this.elements = baseGraph.nodes || [];
            this.bonds = baseGraph.edges || [];
            svg.selectAll('*').remove();
            this.svg = svg;
            this.svgG = svg.append("g");
            let svgG = this.svgG;
            this.dragBond = this.svgG.append("svg:path");
            this.dragBond.attr("class", "link dragline hidden")
                .attr("d", "M0,0L0,0");
            this.bondSelection = svgG.append("g").selectAll("g");
            this.elementSelection = svgG.append("g").selectAll("g");
            svg.call(this.dragSvg()).on("dblclick.zoom", null);
            let graph = this;
            svg.on("mouseup", function (d) { graph.svgMouseUp.call(graph, d); });
            svg.on("mousemove", function (d) { graph.svgMouseMove.call(graph, d); });
        }
        svgMouseMove() { }
        pathExtraRendering(path) { }
        renderElements(newElements) { }
        getSelection() {
            return [].concat(this.selectedElements).concat(this.selectedBonds);
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
            }
            else {
                this.justScaleTransGraph = false;
            }
        }
        svgKeyDown() {
            this.lastKeyDown = d3.event.keyCode;
            switch (d3.event.keyCode) {
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
        checkCtrlCombo(a) {
            return d3.event && ((d3.event.keyCode == a && this.lastKeyDown == this.CTRL_KEY) || (d3.event.keyCode == this.CTRL_KEY && this.lastKeyDown == a));
        }
        svgKeyUp() {
            if (this.checkCtrlCombo(this.A_KEY)) {
                let removeFromSelection = [].concat(this.elements.filter(e => !this.selectedElements.includes(e))).concat(this.bonds.filter(e => !this.selectedBonds.includes(e)));
                this.setSelection(this.elements, this.bonds);
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(removeFromSelection), [], []);
                this.updateMenus();
            }
        }
        pathMouseDown(bond) {
            d3.event.stopPropagation();
            let addEdges = [];
            let removeEl = [];
            let removeEdges = [];
            if (d3.event.ctrlKey || d3.event.metaKey) {
                if (this.selectionContains(bond)) {
                    this.removeFromSelection(bond);
                    removeEdges = [bond];
                }
                else {
                    this.addToSelection(bond);
                    addEdges = [bond];
                }
            }
            else {
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
            var _a, _b;
            if (!d3.select("#selectionRect").node())
                return false;
            let selectionBounds = d3.select("#selectionRect").node().getBoundingClientRect();
            if (Math.round(selectionBounds.width) > 0 && Math.round(selectionBounds.height) > 0) {
                let newSelection = [];
                if (this instanceof SystemDiagramDisplay_1.SystemDiagramDisplay) {
                    for (const el of this.elementSelection.selectAll(".outline")) {
                        if (this.checkOverlap(selectionBounds, el[0].getBoundingClientRect())) {
                            newSelection.push(el[0].__data__);
                        }
                    }
                }
                else {
                    for (const el of this.elementSelection[0]) {
                        if (this.checkOverlap(selectionBounds, el.getBoundingClientRect())) {
                            newSelection.push(el.__data__);
                        }
                    }
                }
                for (const bond of this.bondSelection[0]) {
                    if (bond && this.checkOverlap(selectionBounds, bond.getBoundingClientRect())) {
                        newSelection.push(bond.__data__);
                    }
                }
                let removeList = [];
                let addList = [];
                if (((_a = d3.event.sourceEvent) === null || _a === void 0 ? void 0 : _a.ctrlKey) || ((_b = d3.event.sourceEvent) === null || _b === void 0 ? void 0 : _b.metaKey)) {
                    for (const e of newSelection) {
                        if (this.selectionContains(e)) {
                            this.removeFromSelection(e, false);
                            removeList.push(e);
                        }
                        else {
                            this.addToSelection(e, false);
                            addList.push(e);
                        }
                    }
                }
                else {
                    this.setSelection(newSelection.filter(e => e instanceof GraphElement_3.GraphElement), newSelection.filter(e => e instanceof GraphBond_3.GraphBond));
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
        nodeMouseUp(el) {
            d3.event.stopPropagation();
            this.mouseDownNode = null;
            if (this.handleAreaSelectionEnd())
                return;
            if (!this.justDragged) {
                let addEl = [];
                let remove = [];
                if (d3.event.ctrlKey || d3.event.metaKey) {
                    if (this.selectionContains(el)) {
                        this.removeFromSelection(el);
                        remove = [el];
                    }
                    else {
                        this.addToSelection(el);
                        addEl = [el];
                    }
                }
                else {
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
        addToSelection(e, undoRedo = true) {
            if (e instanceof GraphElement_3.GraphElement) {
                this.selectedElements.push(e);
            }
            else {
                this.selectedBonds.push(e);
            }
            if (undoRedo) {
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects([e]), [], []);
            }
        }
        selectionContains(e) {
            if (e instanceof GraphElement_3.GraphElement) {
                return this.selectedElements.includes(e);
            }
            else {
                return this.selectedBonds.includes(e);
            }
        }
        removeFromSelection(e, undoRedo = true) {
            if (e instanceof GraphElement_3.GraphElement) {
                this.selectedElements = this.selectedElements.filter(d => d != e);
            }
            else {
                this.selectedBonds = this.selectedBonds.filter(d => d != e);
            }
            if (undoRedo) {
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), [], [], ...this.listToIDObjects([e]));
            }
        }
        setSelection(elList, bondList) {
            this.selectedElements = elList;
            this.selectedBonds = bondList;
        }
        nodeMouseDown(el) {
            d3.event.stopPropagation();
            this.mouseDownNode = el;
            this.justDragged = false;
        }
        getEdgePosition(sourceEl, targetEl) {
            let x = targetEl.x - sourceEl.x;
            let y = targetEl.y - sourceEl.y;
            let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
            let thetaUR = Math.atan2(30, 30);
            let thetaUL = Math.PI - thetaUR;
            let thetaLL = Math.PI + thetaUR;
            let thetaLR = 2 * Math.PI - thetaUR;
            let coords = [];
            if ((theta >= 0 && theta < thetaUR) || (theta >= thetaLR && theta < 2 * Math.PI)) {
                coords = [30, -30 * Math.tan(theta)];
            }
            else if (theta >= thetaUR && theta < thetaUL) {
                coords = [30 * 1 / Math.tan(theta), -30];
            }
            else if (theta >= thetaUL && theta < thetaLL) {
                coords = [-30, 30 * Math.tan(theta)];
            }
            else {
                coords = [-30 * 1 / Math.tan(theta), 30];
            }
            return coords;
        }
        drawPath(d) {
            if (this.startedSelectionDrag && this instanceof SystemDiagramDisplay_1.SystemDiagramDisplay) {
                return "M" + d.source.x + "," + d.source.y + "L" + d.target.x + "," + d.target.y;
            }
            else {
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
        changeScale(x, y, scale) {
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
        listToIDObjects(eList) {
            let elements = eList.filter(e => e instanceof GraphElement_3.GraphElement).map(e => e.id);
            let bonds = eList.filter(e => e instanceof GraphBond_3.GraphBond).map(e => JSON.stringify({ source: e.source.id, target: e.target.id }));
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
        dragSvg() {
            let graph = this;
            return d3.behavior.zoom()
                .on("zoom", function () {
                graph.zoomed.call(graph);
                if (graph.dragAllowed) {
                    graph.dragX = d3.event.translate[0];
                    graph.dragY = d3.event.translate[1];
                }
                else {
                    graph.moveSelectionRect();
                }
            })
                .on("zoomstart", function () {
                var _a, _b;
                graph.dragAllowed = d3.event.sourceEvent.buttons === 2;
                graph.dragX = (_a = graph.dragX) !== null && _a !== void 0 ? _a : graph.svgX;
                graph.dragY = (_b = graph.dragY) !== null && _b !== void 0 ? _b : graph.svgY;
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
                if (!(d3.event.sourceEvent.shiftKey))
                    d3.select("body").style("cursor", "move");
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
            paths.enter()
                .append("path")
                .classed("link", true)
                .attr("d", function (d) { return graph.drawPath.call(graph, d); })
                .on("mousedown", function (d) {
                graph.pathMouseDown.call(graph, d);
            });
            paths.classed(this.selectedClass, function (d) {
                return graph.selectedBonds.includes(d);
            }).attr("d", function (d) { return graph.drawPath.call(graph, d); });
            this.pathExtraRendering(paths);
            paths.exit().remove();
        }
        fullRenderElements(dragmove = false) {
            if (dragmove) {
                this.elementSelection.filter(e => this.selectedElements.includes(e)).attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
                return;
            }
            this.elementSelection = this.elementSelection.data(this.elements, function (d) { return d.id.toString(); });
            this.elementSelection.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            this.elementSelection.selectAll("*").remove();
            let newElements = this.elementSelection;
            newElements.enter().append("g");
            newElements.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            let graph = this;
            newElements.classed(this.selectedClass, function (d) {
                return graph.selectedElements.includes(d);
            });
            this.renderElements(newElements);
            this.elementSelection.exit().remove();
        }
        dragmove(el) {
            if (this.mouseDownNode) {
                if (!this.selectedElements.includes(el)) {
                    let selection = this.getSelection();
                    this.setSelection([el], []);
                    this.updateGraph();
                    DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects([el]), ...this.listToIDObjects(selection));
                }
                this.startedSelectionDrag = true;
                let dx = d3.event.dx;
                let dy = d3.event.dy;
                this.dragXOffset += dx;
                this.dragYOffset += dy;
                for (const el of this.selectedElements) {
                    el.x += dx;
                    el.y += dy;
                }
                this.updateGraph(true);
            }
            else {
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
        updateGraph(dragmove = false) {
            this.drawPaths();
            this.fullRenderElements(dragmove);
        }
        zoomed() {
            this.justScaleTransGraph = true;
            if (this.prevScale !== d3.event.scale || d3.event.sourceEvent.buttons == 2) {
                this.changeScale(d3.event.translate[0], d3.event.translate[1], d3.event.scale);
            }
        }
    }
    exports.BaseGraphDisplay = BaseGraphDisplay;
});
define("types/display/BondGraphDisplay", ["require", "exports", "types/display/BaseGraphDisplay"], function (require, exports, BaseGraphDisplay_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.BondGraphDisplay = void 0;
    class BondGraphDisplay extends BaseGraphDisplay_2.BaseGraphDisplay {
        constructor(id, svg, bondGraph) {
            super(svg, bondGraph);
            this.dragging = false;
            this.id = id;
            this.testSVG = d3.select("#app").append("svg");
            this.testSVG.style("position", "absolute")
                .style("left", "-10000000px")
                .style("top", "-10000000px");
            this.defs = svg.append("svg:defs");
            this.makeBaseMarker("causal_stroke_" + id, 1, 5, 10, 10, false)
                .append("path")
                .attr("d", "M1,10L1,-10");
            this.makeBaseMarker("causal_stroke_" + id + "_selected", 1, 5, 10, 10, true)
                .append("path")
                .attr("d", "M1,10L1,-10");
            this.makeBaseMarker("arrow_" + id, 10, 0, 10, 10, false)
                .append("path")
                .attr("d", "M10,0L2,5");
            this.makeBaseMarker("arrow_" + id + "_selected", 10, 0, 10, 10, true)
                .append("path")
                .attr("d", "M10,0L2,5");
            let arrowAndFlat = this.makeBaseMarker("causal_stroke_and_arrow_" + id, 10, 10, 20, 20, false);
            arrowAndFlat.append("path")
                .attr("d", "M10,10L2,15");
            arrowAndFlat.append("path")
                .attr("d", "M10,5L10,15");
            arrowAndFlat = this.makeBaseMarker("causal_stroke_and_arrow_" + id + "_selected", 10, 10, 20, 20, true);
            arrowAndFlat.append("path")
                .attr("d", "M10,10L2,15");
            arrowAndFlat.append("path")
                .attr("d", "M10,5L10,15");
        }
        makeBaseMarker(id, refX, refY, w, h, isSelected) {
            let marker = this.defs.append("svg:marker");
            marker.attr("id", id)
                .attr("refX", refX)
                .attr("refY", refY)
                .attr("markerWidth", w)
                .attr("markerHeight", h)
                .attr("orient", "auto-start-reverse")
                .style("stroke", isSelected ? "rgb(6, 82, 255)" : "#333");
            return marker;
        }
        updateGraph(dragmove = false) {
            this.fullRenderElements(dragmove);
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
        renderElements(newElements) {
            let graph = this;
            newElements.classed("boglElem", true)
                .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
                .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d);
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
            paths.style('marker-end', (d) => {
                if (d.hasDirection) {
                    return 'url(#' + (d.causalStroke && !d.causalStrokeDirection ? "causal_stroke_and_arrow_" : "arrow_") + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + ')';
                }
            })
                .style('marker-start', (d) => {
                if (d.hasDirection) {
                    return (d.causalStroke && d.causalStrokeDirection ? 'url(#causal_stroke_' + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + ')' : "");
                }
            })
                .style('stroke-width', 2);
        }
    }
    exports.BondGraphDisplay = BondGraphDisplay;
});
define("backendManager", ["require", "exports", "types/bonds/BondGraphBond", "types/bonds/GraphBond", "types/bonds/GraphBondID", "types/display/BondGraphDisplay", "types/display/SystemDiagramDisplay", "types/elements/BondGraphElement", "types/elements/ElementNamespace", "types/elements/SystemDiagramElement", "types/graphs/BondGraph", "types/graphs/SystemDiagram"], function (require, exports, BondGraphBond_1, GraphBond_4, GraphBondID_1, BondGraphDisplay_1, SystemDiagramDisplay_2, BondGraphElement_1, ElementNamespace_2, SystemDiagramElement_2, BondGraph_1, SystemDiagram_1) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.backendManager = void 0;
    var backendManager;
    (function (backendManager) {
        class BackendManager {
            parseAndDisplayBondGraph(id, jsonString, svg) {
                let bg = JSON.parse(jsonString);
                let minX = Infinity;
                let minY = Infinity;
                let maxX = -Infinity;
                let maxY = -Infinity;
                let elements = JSON.parse(bg.elements).map((e, i) => {
                    if (e.x < minX)
                        minX = e.x;
                    if (e.y < minY)
                        minY = e.y;
                    if (e.x > maxX)
                        maxX = e.x;
                    if (e.y > maxY)
                        maxY = e.y;
                    return new BondGraphElement_1.BondGraphElement(i, e.label, e.x, e.y);
                });
                elements.forEach(e => {
                    e.x += (maxX - minX) / 2 - maxX;
                    e.y += (maxY - minY) / 2 - maxY;
                });
                let bonds = JSON.parse(bg.bonds).map(b => {
                    return new BondGraphBond_1.BondGraphBond(elements[b.sourceID], elements[b.targetID], b.causalStroke, b.causalStrokeDirection, !b.hasDirection && id != 0, b.velocity);
                });
                let bondGraph = new BondGraphDisplay_1.BondGraphDisplay(id, svg, new BondGraph_1.BondGraph(elements, bonds));
                if (id == 0) {
                    window.unsimpBG = bondGraph;
                }
                else if (id == 1) {
                    window.simpBG = bondGraph;
                }
                else {
                    window.causalBG = bondGraph;
                }
                bondGraph.updateGraph();
                this.zoomCenterGraph(JSON.stringify(id + 2));
            }
            displayUnsimplifiedBondGraph(jsonString) {
                this.parseAndDisplayBondGraph(0, jsonString, window.unsimpBGSVG);
            }
            displaySimplifiedBondGraph(jsonString) {
                this.parseAndDisplayBondGraph(1, jsonString, window.simpBGSVG);
            }
            displayCausalBondGraphOption(jsonStrings, index) {
                this.parseAndDisplayBondGraph(2, jsonStrings[index], window.causalBGSVG);
            }
            loadSystemDiagram(jsonString) {
                var _a;
                let edges = [];
                let minX = Infinity;
                let minY = Infinity;
                let maxX = -Infinity;
                let maxY = -Infinity;
                let parsedJson = JSON.parse(jsonString);
                let elements = new Map();
                let i = 0;
                for (let e of parsedJson.elements) {
                    if (e.x < minX)
                        minX = e.x;
                    if (e.y < minY)
                        minY = e.y;
                    if (e.x > maxX)
                        maxX = e.x;
                    if (e.y > maxY)
                        maxY = e.y;
                    if (e.id != null) {
                        elements.set(e.id, new SystemDiagramElement_2.SystemDiagramElement(e.id, e.type, e.x, e.y, e.velocity, e.modifiers));
                    }
                    else {
                        elements.set(i++, new SystemDiagramElement_2.SystemDiagramElement(i, e.type, e.x, e.y, e.velocity, e.modifiers));
                    }
                }
                elements.forEach(e => {
                    e.x += (maxX - minX) / 2 - maxX;
                    e.y += (maxY - minY) / 2 - maxY;
                });
                for (let edge of parsedJson.edges) {
                    let bond = new GraphBond_4.GraphBond(elements.get(edge.source), elements.get(edge.target));
                    bond.velocity = (_a = edge.velocity) !== null && _a !== void 0 ? _a : 0;
                    edges.push(bond);
                }
                window.systemDiagram = new SystemDiagramDisplay_2.SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram_1.SystemDiagram([], []));
                DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", Array.from(elements.values()).map(e => JSON.stringify(e)).concat(edges.map(e => JSON.stringify(e))), ...window.systemDiagram.listToIDObjects([].concat(window.systemDiagram.selectedElements).concat(window.systemDiagram.selectedBonds)), false);
                let systemDiagram = new SystemDiagramDisplay_2.SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram_1.SystemDiagram(Array.from(elements.values()), edges));
                systemDiagram.draggingElement = null;
                window.systemDiagram = systemDiagram;
                systemDiagram.updateGraph();
                this.zoomCenterGraph("1");
            }
            zoomCenterGraph(index) {
                let graph = this.getGraphByIndex(index);
                let prevDisplay = graph.svgG.node().parentElement.parentElement.parentElement.style.display;
                graph.svgG.node().parentElement.parentElement.parentElement.style.display = "block";
                let svgDim = graph.svgG.node().getBBox();
                let windowDim = graph.svgG.node().parentElement.getBoundingClientRect();
                let scale = 1;
                if (svgDim.width / svgDim.height > windowDim.width / windowDim.height) {
                    scale = (0.8 * windowDim.width) / svgDim.width;
                }
                else {
                    scale = (0.8 * windowDim.height) / svgDim.height;
                }
                scale = Math.min(Math.max(scale, 0.25), 1.75);
                let xTrans = -svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2);
                let yTrans = -svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2);
                graph.changeScale(xTrans, yTrans, scale);
                graph.svgG.node().parentElement.parentElement.parentElement.style.display = prevDisplay;
            }
            openFile() {
                return __awaiter(this, void 0, void 0, function* () {
                    let fileHandle;
                    [fileHandle] = yield window.showOpenFilePicker();
                    const file = yield fileHandle.getFile();
                    const contents = yield file.text();
                    return contents;
                });
            }
            getTabNum() {
                return parseInt(window.tabNum);
            }
            saveAsFile(fileName, contentStreamReference) {
                return __awaiter(this, void 0, void 0, function* () {
                    const arrayBuffer = yield contentStreamReference.arrayBuffer();
                    const blob = new Blob([arrayBuffer]);
                    const pickerOptions = {
                        suggestedName: `systemDiagram.bogl`,
                        types: [
                            {
                                description: 'A BoGL File',
                                accept: {
                                    'text/plain': ['.bogl'],
                                },
                            },
                        ],
                    };
                    const fileHandle = yield window.showSaveFilePicker(pickerOptions);
                    window.filePath = fileHandle;
                    const writableFileStream = yield fileHandle.createWritable();
                    yield writableFileStream.write(blob);
                    yield writableFileStream.close();
                });
            }
            saveFile(fileName, contentStreamReference) {
                return __awaiter(this, void 0, void 0, function* () {
                    const arrayBuffer = yield contentStreamReference.arrayBuffer();
                    const blob = new Blob([arrayBuffer]);
                    const pickerOptions = {
                        suggestedName: `systemDiagram.bogl`,
                        types: [
                            {
                                description: 'A BoGL File',
                                accept: {
                                    'text/plain': ['.bogl'],
                                },
                            },
                        ],
                    };
                    if (window.filePath == null) {
                        window.filePath = yield window.showSaveFilePicker(pickerOptions);
                    }
                    const writableFileStream = yield window.filePath.createWritable();
                    yield writableFileStream.write(blob);
                    yield writableFileStream.close();
                });
            }
            cut() {
                this.getSystemDiagramDisplay().copySelection();
                this.getSystemDiagramDisplay().deleteSelection();
            }
            copy() {
                this.getSystemDiagramDisplay().copySelection();
            }
            paste() {
                this.getSystemDiagramDisplay().pasteSelection();
            }
            delete(needsConfirmation = true) {
                this.getSystemDiagramDisplay().deleteSelection(needsConfirmation);
            }
            areMultipleElementsSelected() {
                return this.getSystemDiagramDisplay().selectedElements.length > 1 || this.getSystemDiagramDisplay().selectedBonds.length > 1;
            }
            getSystemDiagramDisplay() {
                return this.getGraphByIndex("1");
            }
            getSystemDiagram() {
                return JSON.stringify({
                    elements: window.systemDiagram.elements,
                    bonds: window.systemDiagram.bonds
                });
            }
            setModifier(i, value) {
                let prevModVals = window.systemDiagram.selectedElements.map(e => e.modifiers.includes(i));
                if (value) {
                    for (const el of window.systemDiagram.selectedElements) {
                        if (ElementNamespace_2.ElementNamespace.elementTypes[el.type].allowedModifiers.includes(i) && !el.modifiers.includes(i)) {
                            el.modifiers.push(i);
                        }
                    }
                }
                else {
                    for (const el of window.systemDiagram.selectedElements) {
                        if (el.modifiers.includes(i)) {
                            el.modifiers.splice(el.modifiers.indexOf(i), 1);
                        }
                    }
                }
                window.systemDiagram.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionModifier", window.systemDiagram.selectedElements.map(e => e.id), i, value, prevModVals);
                window.systemDiagram.updateModifierMenu();
            }
            getGraphByIndex(i) {
                if (i == "1") {
                    return window.systemDiagram;
                }
                else if (i == "2") {
                    return window.unsimpBG;
                }
                else if (i == "3") {
                    return window.simpBG;
                }
                else {
                    return window.causalBG;
                }
            }
            setZoom(i) {
                let graph = this.getGraphByIndex(window.tabNum);
                let windowDim = graph.svg.node().parentElement.getBoundingClientRect();
                let xOffset = (graph.prevScale * 100 - i) * (graph.svgX - graph.initXPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);
                let yOffset = (graph.prevScale * 100 - i) * (graph.svgY - graph.initYPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);
                if (graph.prevScale * 100 - i != 0) {
                    graph.changeScale(windowDim.width / 2 - (windowDim.width / 2 - graph.svgX) - xOffset, windowDim.height / 2 - (windowDim.height / 2 - graph.svgY) - yOffset, i / 100);
                }
            }
            setTab(key) {
                window.tabNum = key;
                DotNet.invokeMethodAsync("BoGLWeb", "SetScale", this.getGraphByIndex(key).prevScale);
            }
            setVelocity(velocity) {
                let prevVelVals = window.systemDiagram.getSelection().map(e => e.velocity);
                for (const e of window.systemDiagram.getSelection()) {
                    if (e instanceof GraphBond_4.GraphBond || ElementNamespace_2.ElementNamespace.elementTypes[e.type].velocityAllowed) {
                        e.velocity = velocity;
                    }
                }
                window.systemDiagram.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionVelocity", ...window.systemDiagram.listToIDObjects(window.systemDiagram.getSelection()), velocity, prevVelVals);
            }
            generateURL() {
                return JSON.stringify({
                    elements: window.systemDiagram.elements.map(e => {
                        e.x = Math.round(e.x * 10) / 10;
                        e.y = Math.round(e.y * 10) / 10;
                        return e;
                    }),
                    bonds: window.systemDiagram.bonds
                }, function (key, val) {
                    return val.toFixed ? Number(val.toFixed(3)) : val;
                });
            }
            textToClipboard(text) {
                navigator.clipboard.writeText(text);
            }
            hideMenu(menuId) {
                let el = document.getElementById(menuId);
                if (document.getElementById(menuId)) {
                    el = el.parentElement.parentElement;
                    if (el.getAttribute("hidden-menu") != "true") {
                        el.setAttribute("hidden-menu", "true");
                    }
                }
            }
            closeMenu(menuName) {
                switch (menuName) {
                    case "File":
                        this.hideMenu("fileMenu");
                        break;
                    case "Edit":
                        this.hideMenu("editMenu");
                        break;
                    case "Help":
                        this.hideMenu("helpMenu");
                        this.hideMenu("exampleMenu");
                        this.hideMenu("mechTransMenu");
                        this.hideMenu("mechRotMenu");
                        this.hideMenu("elecMenu");
                }
            }
            runTutorial() {
                this.closeMenu("Help");
                window.introJs().setOptions({
                    showStepNumbers: false,
                    hideNext: true,
                    steps: [{
                            intro: '<p><b>Welcome To BoGL Web</b></p><p>' +
                                'This application is used to construct system diagrams and generate bond graphs from those diagrams</p>'
                        }, {
                            element: document.querySelector('.card-container'),
                            intro: '<p><b>The Canvas</b></p><p>The highlighted space is the Canvas where you can construct, move, and rearrange your system diagrams.</p>'
                        }, {
                            element: document.querySelector('#graphMenu'),
                            intro: '<p><b>The Element Palette</b></p><p>This is the element palette. After expanding the menus, you can select and drag elements onto the canvas to construct system diagrams</p>'
                        }, {
                            element: document.querySelector('.card-container'),
                            intro: '<p><b>Constructing a System Diagram</b></p><p>Select and drag an element to add it to the Canvas, and then select near its black border to start creating an edge.  You can then select near a second element to finish making the edge. If you see a green circle, your edge is valid, if you see a red X when you try to make an edge, it means the edge you are trying to make is invalid (the two elements do not make sense to be connected).' +
                                '<br><br><img src="images/tutorial/EdgeCreationGif-Edited.gif" width="100%">' +
                                '</p>'
                        },
                        {
                            element: document.querySelector('#modifierMenu'),
                            intro: '<p><b>The Modifier Menu</b></p><p>Use this menu to add modifiers to the selected element. Some modifiers require multiple elements to be selected. You can do this by holding down the control key and clicking elements you want to select, or drag the cursor across the canvas with the left mouse button to create a selection region. All elements that are completely or partially inside the region will be selected.</p>'
                        }, {
                            element: document.querySelector('#zoomMenu'),
                            intro: '<p><b>The Zoom Menu</b></p><p>This menu allows you to zoom in and out of the canvas. You can use the zoom slider, or your scroll wheel.' +
                                '<br><br><img src="images/tutorial/ZoomGif-Edited.gif" width="100%">' +
                                '</p>'
                        }, {
                            element: document.querySelector('#generateButton'),
                            intro: '<p><b>The Generate Button</b></p><p>The generate button allows you to turn your system diagram into a bond graph. While the bond graph is generating you will see a loading bar which signifies that BoGL Web is processing your System Diagram. This can take a few seconds.</p>'
                        }, {
                            element: document.querySelector('.ant-tabs-nav-list'),
                            intro: '<p><b>The Tabs</b></p><p>These tabs change what stage of the bond graph generation is being displayed. You can look at the unsimplified bond graph, the simplified bond graph, or the causal bond graph</p>'
                        }, {
                            element: document.querySelector('.ant-menu-horizontal > :nth-child(2)'),
                            intro: '<p><b>The File Menu</b></p>' +
                                '<p>This is the file menu. Selecting it opens a menu which allows you to:' +
                                '<ul>' +
                                '<li>Create a new system diagram</li>' +
                                '<li>Open a previously saved .bogl file from your computer</li>' +
                                '<li>Save a .bogl file representing the System Diagram to your computer</li>' +
                                '<li>Generate a URL that that can be used to chare your System Diagram</li>' +
                                '</ul></p>'
                        }, {
                            element: document.querySelector('.ant-menu-horizontal > :nth-child(3)'),
                            intro: '<p><b>The Edit Menu</b></p>' +
                                '<p>This is the edit menu. Selecting it open a menu which allows you to:' +
                                '<ul>' +
                                '<li>Copy, cut, and paste elements of the system diagram</li>' +
                                '<li>Undo and redo changes</li>' +
                                '<li>Delete elements from the System Diagram</li>' +
                                '</ul></p>'
                        }, {
                            element: document.querySelector('#iconButtons'),
                            intro: '<p><b>The Toolbar</b></p><p>You can perform similar features to the edit menu here. By selecting the icons you can save a System Diagram, cut, copy, paste, undo, redo, and delete an element or edge from the System Diagram.</p>'
                        }, {
                            element: document.querySelector('.ant-menu-horizontal > :nth-child(4)'),
                            intro: '<p><b>The Help Menu</b></p>' +
                                '<p>This is the help menu. Selecting it opens a menu which allows you to:' +
                                '<ul>' +
                                '<li>Confirm deleting many items at once. Selecting this option will allow you to select multiple items and then delete them all at once.</li>' +
                                '<li>Start this tutorial again</li>' +
                                '<li>Load example System Diagrams</li>' +
                                '<li>Report bugs that you find</li>' +
                                '<li>Learn about who created BoGL Web System</li>' +
                                '</ul></p>'
                        }]
                }).onbeforechange(function () {
                    window.dispatchEvent(new Event('resize'));
                }).start();
            }
            parseElementAndEdgeStrings(objects) {
                let elements = [];
                let bonds = [];
                for (const object of objects) {
                    let json = JSON.parse(object);
                    if (json.hasOwnProperty("id")) {
                        elements.push(new SystemDiagramElement_2.SystemDiagramElement(json.id, json.type, json.x, json.y, json.velocity, json.modifiers));
                    }
                    else {
                        bonds.push(new GraphBond_4.GraphBond(json.source, json.target, json.velocity));
                    }
                }
                return [elements, bonds];
            }
            parseEdgeIDStrings(edgeIDs) {
                let edges = [];
                let i = 0;
                for (const edgeString of edgeIDs) {
                    let json = JSON.parse(edgeString);
                    edges.push(new GraphBondID_1.GraphBondID(json.source, json.target, i));
                    i++;
                }
                return edges;
            }
            checkBondIDs(bondIDs, b) {
                let sourceID = b.source.id;
                let targetID = b.target.id;
                return bondIDs.find(e => e.checkEquality(sourceID, targetID));
            }
            handleUndoRedo(undo) {
                return __awaiter(this, void 0, void 0, function* () {
                    DotNet.invokeMethodAsync("BoGLWeb", "UndoRedoHandler", parseInt(window.tabNum), undo);
                });
            }
            urDoAddSelection(newObjects, prevSelElIDs, prevSelectedEdges, highlight, isUndo) {
                let sysDiag = window.systemDiagram;
                let [elements, bonds] = this.parseElementAndEdgeStrings(newObjects);
                if (isUndo) {
                    let elIDs = elements.map(e => e.id);
                    let elBonds = bonds.map(b => { return new GraphBondID_1.GraphBondID(b.source.id, b.target.id); });
                    sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                    sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                    let prevSelEdgeIDs = this.parseEdgeIDStrings(prevSelectedEdges);
                    if (highlight) {
                        sysDiag.setSelection(sysDiag.elements.filter(e => prevSelElIDs.includes(e.id)), sysDiag.bonds.filter(b => this.checkBondIDs(prevSelEdgeIDs, b)));
                    }
                    else {
                        sysDiag.setSelection([], []);
                    }
                }
                else {
                    sysDiag.elements = sysDiag.elements.concat(elements);
                    sysDiag.bonds = sysDiag.bonds.concat(bonds);
                    if (highlight) {
                        sysDiag.setSelection(elements, bonds);
                    }
                    else {
                        sysDiag.setSelection([], []);
                    }
                }
                sysDiag.updateGraph();
                sysDiag.updateMenus();
            }
            urDoDeleteSelection(deletedObjects, unselectedDeletedEdges, isUndo) {
                let sysDiag = window.systemDiagram;
                let [elements, bonds] = this.parseElementAndEdgeStrings(deletedObjects);
                let [_, unselectedBonds] = this.parseElementAndEdgeStrings(unselectedDeletedEdges);
                if (isUndo) {
                    sysDiag.elements = sysDiag.elements.concat(elements);
                    unselectedBonds = unselectedBonds.map(b => {
                        b.source = sysDiag.elements.find(e => e.id == b.source.id);
                        b.target = sysDiag.elements.find(e => e.id == b.target.id);
                        return b;
                    });
                    bonds = bonds.map(b => {
                        b.source = sysDiag.elements.find(e => e.id == b.source.id);
                        b.target = sysDiag.elements.find(e => e.id == b.target.id);
                        return b;
                    });
                    sysDiag.bonds = sysDiag.bonds.concat(bonds).concat(unselectedBonds);
                    sysDiag.setSelection(elements, bonds);
                }
                else {
                    let elIDs = elements.map(e => e.id);
                    let elBonds = bonds.concat(unselectedBonds).map(b => { return new GraphBondID_1.GraphBondID(b.source.id, b.target.id); });
                    sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                    sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                    sysDiag.setSelection([], []);
                }
                sysDiag.updateGraph();
                sysDiag.updateMenus();
            }
            urDoChangeSelection(elIDsToAdd, edgesToAdd, elIDsToRemove, edgesToRemove, isUndo) {
                let diagram = this.getGraphByIndex(window.tabNum);
                let addToSelectionEdges = this.parseEdgeIDStrings(edgesToAdd);
                let removeFromSelectionEdges = this.parseEdgeIDStrings(edgesToRemove);
                let elAddSet = isUndo ? elIDsToRemove : elIDsToAdd;
                let elRemoveSet = isUndo ? elIDsToAdd : elIDsToRemove;
                let edgeAddSet = isUndo ? removeFromSelectionEdges : addToSelectionEdges;
                let edgeRemoveSet = isUndo ? addToSelectionEdges : removeFromSelectionEdges;
                diagram.selectedElements = diagram.selectedElements.concat(diagram.elements.filter(e => elAddSet.includes(e.id)));
                diagram.selectedBonds = diagram.selectedBonds.concat(diagram.bonds.filter(b => this.checkBondIDs(edgeAddSet, b)));
                diagram.selectedElements = diagram.selectedElements.filter(e => !elRemoveSet.includes(e.id));
                diagram.selectedBonds = diagram.selectedBonds.filter(b => !this.checkBondIDs(edgeRemoveSet, b));
                diagram.updateGraph();
                diagram.updateMenus();
            }
            urDoMoveSelection(elements, xOffset, yOffset, isUndo) {
                let diagram = this.getGraphByIndex(window.tabNum);
                diagram.elements.filter(e => elements.includes(e.id)).forEach(e => {
                    e.x = e.x + (isUndo ? -1 : 1) * xOffset;
                    e.y = e.y + (isUndo ? -1 : 1) * yOffset;
                });
                diagram.updateGraph();
            }
            urDoChangeSelectionVelocity(elIDs, edgeIDs, velID, prevVelVals, isUndo) {
                let sysDiag = window.systemDiagram;
                let bondIDs = this.parseEdgeIDStrings(edgeIDs);
                sysDiag.elements.filter(e => elIDs.includes(e.id)).forEach(e => e.velocity = isUndo ? prevVelVals[elIDs.findIndex(i => i == e.id)] : velID);
                sysDiag.bonds.filter(b => this.checkBondIDs(bondIDs, b)).forEach(b => b.velocity = isUndo ? prevVelVals[elIDs.length + this.checkBondIDs(bondIDs, b).velID] : velID);
                sysDiag.updateGraph();
                sysDiag.updateVelocityMenu();
            }
            urDoChangeSelectionModifier(elIDs, modID, modVal, prevModVals, isUndo) {
                let sysDiag = window.systemDiagram;
                elIDs.forEach(function (id, i) {
                    let el = sysDiag.elements.find(e => e.id == id);
                    if (isUndo) {
                        if (prevModVals[i] && !el.modifiers.includes(modID)) {
                            el.modifiers.push(modID);
                        }
                        else if (!prevModVals[i] && el.modifiers.includes(modID)) {
                            el.modifiers.splice(el.modifiers.indexOf(modID), 1);
                        }
                    }
                    else {
                        if (modVal && ElementNamespace_2.ElementNamespace.elementTypes[el.type].allowedModifiers.includes(modID) && !el.modifiers.includes(modID)) {
                            el.modifiers.push(modID);
                        }
                        else if (el.modifiers.includes(modID)) {
                            el.modifiers.splice(el.modifiers.indexOf(modID), 1);
                        }
                    }
                });
                sysDiag.updateGraph();
                sysDiag.updateModifierMenu();
            }
            initInstance(instance) {
                this.instance = instance;
            }
        }
        backendManager.BackendManager = BackendManager;
        function getBackendManager() {
            return new BackendManager();
        }
        backendManager.getBackendManager = getBackendManager;
    })(backendManager = exports.backendManager || (exports.backendManager = {}));
});
define("types/display/BondGraphBond", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.SubmenuID = void 0;
    class SubmenuID {
        constructor(index, id) {
            this.index = index;
            this.id = id;
            this.hasClickAction = false;
        }
    }
    exports.SubmenuID = SubmenuID;
});
define("main", ["require", "exports", "types/elements/ElementNamespace", "types/display/SystemDiagramDisplay", "backendManager", "types/graphs/SystemDiagram", "types/display/BondGraphBond"], function (require, exports, ElementNamespace_3, SystemDiagramDisplay_3, backendManager_2, SystemDiagram_2, BondGraphBond_2) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    exports.populateMenu = void 0;
    var getBackendManager = backendManager_2.backendManager.getBackendManager;
    function populateMenu() {
        ElementNamespace_3.ElementNamespace.categories.map((c, i) => {
            ElementNamespace_3.ElementNamespace.elementTypes.filter(e => e.category === i).forEach(e => {
                const group = document.createElement('div');
                group.classList.add("groupDiv");
                group.addEventListener("mousedown", function () {
                    document.body.style.cursor = "grabbing";
                    window.systemDiagram.draggingElement = e.id;
                });
                document.getElementById(c.folderName).appendChild(group);
                var box = document.createElement('div');
                box.classList.add("box");
                group.appendChild(box);
                var image = document.createElement('img');
                image.src = "images/elements/" + e.image + ".svg";
                image.draggable = false;
                image.classList.add("elemImage");
                box.appendChild(image);
            });
        });
    }
    exports.populateMenu = populateMenu;
    var topMenuButtons;
    function loadPage() {
        return __awaiter(this, void 0, void 0, function* () {
            window.tabNum = "1";
            let sliderHolder = document.querySelector("#zoomMenu .ant-slider-handle");
            let sliderImg = document.createElement("img");
            sliderImg.src = "images/sliderIcon.svg";
            sliderImg.id = "sliderImg";
            sliderImg.draggable = false;
            sliderHolder.appendChild(sliderImg);
            window.backendManager = backendManager_2.backendManager;
            window.systemDiagramSVG = d3.select("#systemDiagram").append("svg");
            window.systemDiagramSVG.classed("graphSVG", true);
            const urlParams = new URLSearchParams(window.location.search);
            const myParam = urlParams.get('q');
            if (myParam !== null) {
                let sysDiagramString = yield DotNet.invokeMethodAsync("BoGLWeb", "uncompressUrl", myParam);
                getBackendManager().loadSystemDiagram(sysDiagramString);
            }
            else {
                window.systemDiagram = new SystemDiagramDisplay_3.SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram_2.SystemDiagram([], []));
                window.systemDiagram.updateGraph();
                backendManager_2.backendManager.getBackendManager().zoomCenterGraph("1");
                window.systemDiagram.changeScale(window.systemDiagram.svgX, window.systemDiagram.svgY, 1);
            }
            document.querySelectorAll('input[type="checkbox"]').forEach(e => e.addEventListener("click", () => e.focus()));
            document.addEventListener("mouseup", function () {
                document.body.style.cursor = "auto";
                window.systemDiagram.draggingElement = null;
            });
            populateMenu();
            window.unsimpBGSVG = d3.select("#unsimpBG").append("svg");
            window.unsimpBGSVG.classed("graphSVG", true);
            window.simpBGSVG = d3.select("#simpBG").append("svg");
            window.simpBGSVG.classed("graphSVG", true);
            window.causalBGSVG = d3.select("#causalBG").append("svg");
            window.causalBGSVG.classed("graphSVG", true);
            d3.select(window).on("keydown", function () {
                let graph = backendManager_2.backendManager.getBackendManager().getGraphByIndex(window.tabNum);
                graph.svgKeyDown.call(graph);
            })
                .on("keyup", function () {
                let graph = backendManager_2.backendManager.getBackendManager().getGraphByIndex(window.tabNum);
                graph.svgKeyUp.call(graph);
            });
            topMenuButtons = document.getElementsByClassName('topMenu');
            for (let i = 0; i < 3; i++) {
                topMenuButtons.item(i).click();
                clickSubmenus(i);
                menuClickAction(topMenuButtons.item(i), i);
            }
            document.getElementsByClassName("page").item(0).addEventListener("click", () => {
                for (let i = 0; i < Object.keys(menuIdMap).length; i++) {
                    let el = document.getElementById(menuIdMap[i]);
                    if (el) {
                        el.parentElement.parentElement.setAttribute("hidden-menu", "true");
                    }
                }
            });
            window.onbeforeunload = function (e) {
                return "Are you sure you want to exit BoGL Web? Your current progress will be lost unless you download it or make a URL from it.";
            };
        });
    }
    var menuIdMap = {
        0: "fileMenu",
        1: "editMenu",
        2: "helpMenu",
        3: "exampleMenu",
        4: "mechTransMenu",
        5: "mechRotMenu",
        6: "elecMenu"
    };
    var submenuMap = {
        2: [new BondGraphBond_2.SubmenuID(3, 3)],
        3: [new BondGraphBond_2.SubmenuID(1, 4), new BondGraphBond_2.SubmenuID(2, 5), new BondGraphBond_2.SubmenuID(3, 6)]
    };
    var menuClickingDone = false;
    function findParentMenu(menuId) {
        for (let key of Object.keys(submenuMap)) {
            if (submenuMap[key].some(sub => sub.id == menuId)) {
                return parseInt(key);
            }
        }
        return null;
    }
    function findAllParentMenus(menuId) {
        let parent = findParentMenu(menuId);
        if (parent != null) {
            return [parent, ...findAllParentMenus(parent)];
        }
        return [];
    }
    function menuClickAction(menuTitle, k) {
        menuTitle.addEventListener("click", (e) => {
            e.stopPropagation();
            let parents = findAllParentMenus(k);
            waitForMenuClickingDone(() => {
                var _a;
                let el = document.getElementById(menuIdMap[k]);
                if (el) {
                    el = (_a = el.parentElement) === null || _a === void 0 ? void 0 : _a.parentElement;
                    el.setAttribute("hidden-menu", (el.getAttribute("hidden-menu") == "false").toString());
                    if (![0, 1, 2].includes(k)) {
                        let menuTitleBounds = menuTitle.getBoundingClientRect();
                        el.style.top = menuTitleBounds.top + "px";
                        el.style.left = (menuTitleBounds.left + menuTitleBounds.width + 4) + "px";
                    }
                    if (el.getAttribute("hidden-menu") == "false" && submenuMap.hasOwnProperty(k)) {
                        for (let sub of submenuMap[k]) {
                            if (!sub.hasClickAction) {
                                let el = document.getElementById(menuIdMap[k]).parentElement.children[sub.index];
                                menuClickAction(el, sub.id);
                                sub.hasClickAction = true;
                            }
                        }
                    }
                    for (let i = 0; i < Object.keys(menuIdMap).length; i++) {
                        el = document.getElementById(menuIdMap[i]);
                        if (i == k || parents.includes(i) || !el)
                            continue;
                        el = el.parentElement.parentElement;
                        el.setAttribute("hidden-menu", "true");
                    }
                }
            });
        });
    }
    function pollDOM() {
        const el = document.getElementById('graphMenu') && document.getElementsByClassName('topMenu').length > 0;
        if (el) {
            loadPage();
        }
        else {
            setTimeout(pollDOM, 20);
        }
    }
    function clickSubmenus(menuId) {
        var _a, _b;
        const cond = (_b = (_a = document.getElementById(menuIdMap[menuId])) === null || _a === void 0 ? void 0 : _a.parentElement) === null || _b === void 0 ? void 0 : _b.parentElement;
        if (cond) {
            for (let submenu of submenuMap[menuId]) {
                let submenuEl = document.getElementById(menuIdMap[menuId]).parentElement.children[submenu.index];
                submenuEl.click();
                clickSubmenus(submenu.id);
            }
        }
        else if (submenuMap.hasOwnProperty(menuId)) {
            setTimeout(() => clickSubmenus(menuId), 20);
        }
        if (menuId == 6) {
            menuClickingDone = true;
        }
    }
    function waitForMenuClickingDone(func) {
        if (menuClickingDone) {
            func();
        }
        else {
            setTimeout(() => waitForMenuClickingDone(func), 20);
        }
    }
    pollDOM();
});
//# sourceMappingURL=build.js.map