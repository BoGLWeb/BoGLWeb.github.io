import { ZoomEvent, DragEvent } from "../../type_libraries/d3";
import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { ElementNamespace } from "../elements/ElementNamespace";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";
import { SystemDiagram } from "../graphs/SystemDiagram";
import { BaseGraphDisplay } from "./BaseGraphDisplay";
import { MultiElementType } from "../elements/MultiElementType";
import { GraphElement } from "../elements/GraphElement";

export class SystemDiagramDisplay extends BaseGraphDisplay {
    edgeCircle: SVGSelection;
    rejectX: SVGSelection;
    edgeOrigin: SystemDiagramElement = null;
    selectedGroup: (SystemDiagramElement | GraphBond)[] = [];
    velocityMap = {
        1: "⮢",
        2: "⮣",
        3: "⮥",
        4: "⮧",
        5: "⮡",
        6: "⮠",
        7: "⮦",
        8: "⮤"
    };
    justClickedEdge: boolean = false;

    constructor(svg: SVGSelection, systemDiagram: SystemDiagram) {
        super(svg, systemDiagram);

        let graph = this;
        this.highestElemId = systemDiagram.nodes.length;

        // listen for key events
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
            .attr("style", "cursor: pointer; display: none;");
        this.rejectX = this.svgG.append("path");
        this.rejectX
            .attr("d", d3.svg.symbol().type("cross").size(100))
            .style("fill", "red")
            .style("display", "none");
    }

    moveCircle(e: SystemDiagramElement) {
        let coordinates = d3.mouse(<Event>d3.event.currentTarget);
        let x = coordinates[0];
        let y = coordinates[1];
        let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
        let fourth = 1 / 4 * Math.PI;
        let s = 30;
        let coords = [];
        // quads 1, 2, 3, and 4
        if ((theta >= 0 && theta < fourth) || (theta >= 7 * fourth && theta < 8 * fourth)) {
            coords = [s, -s * Math.tan(theta)]
        } else if (theta >= fourth && theta < 3 * fourth) {
            coords = [s * 1 / Math.tan(theta), -s]
        } else if (theta >= 3 * fourth && theta < 5 * fourth) {
            coords = [-s, s * Math.tan(theta)]
        } else {
            coords = [-s * 1 / Math.tan(theta), s]
        }
        this.edgeCircle.attr("cx", e.x + coords[0]).attr("cy", e.y + coords[1]);
        this.rejectX.attr("transform", "translate(" + (e.x + coords[0]) + "," + (e.y + coords[1]) + ") rotate(45)")
    }

    setFollowingEdge(sourceNode: SystemDiagramElement) {
        this.edgeOrigin = sourceNode;
        if (sourceNode == null) {
            // hide edge
            this.dragBond.classed("hidden", true);
        } else {
            this.dragBond.attr("d", "M" + sourceNode.x + "," + sourceNode.y + "L" + d3.mouse(this.svgG.node())[0] + "," + d3.mouse(this.svgG.node())[1]);
            this.dragBond.classed("hidden", false);
        }
    }

    setEdgeMarkerVisible(e: SystemDiagramElement) {
        if (!this.edgeOrigin || ElementNamespace.isCompatible(this.edgeOrigin, e, this)) {
            this.edgeCircle.style("display", "block");
            this.rejectX.style("display", "none");
        } else {
            this.rejectX.style("display", "block");
            this.edgeCircle.style("display", "none");
        }
    }

    renderElements(newElements: GraphElementSelection) {
        let graph = this;
        newElements.classed("boglElem", true)
            .on("mouseout", function () {
                d3.select(this).classed(graph.bondClass, false);
            })
            .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
            .call(this.drag);

        let selectedElements = graph.selectedGroup.filter(e => e instanceof SystemDiagramElement) as GraphElement[];
        newElements.classed(this.selectedClass, function (d) {
            return selectedElements.includes(d);
        });

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
        image.attr("href", function (d) { return "/images/elements/" + ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].image + ".svg"; })
            .classed("hoverImg", true)
            .attr("x", "-25px")
            .attr("y", "-25px")
            .attr("preserveAspectRatio", "xMidYMid meet")
            .attr("height", "50px")
            .attr("width", "50px");

        group.selectAll("text").html(null);

        group.each(function (d: SystemDiagramElement) {
            if (d.velocity != 0) {
                let text = group.append("text");
                text.text((d: SystemDiagramElement) => graph.velocityMap[d.velocity])
                    .each(function (d: SystemDiagramElement) {
                        if (d.velocity != 0) {
                            let velocityClass = "";
                            if (d.velocity == 1 || d.velocity == 2) {
                                velocityClass = "topVelocity";
                            } else if (d.velocity == 3 || d.velocity == 4) {
                                velocityClass = "rightVelocity";
                            } else if (d.velocity == 5 || d.velocity == 6) {
                                velocityClass = "bottomVelocity";
                            } else {
                                velocityClass = "leftVelocity";
                            }
                            this.classList.add("velocityArrow");
                            this.classList.add(velocityClass);
                        }
                    })
                    .attr("x", (d: SystemDiagramElement) => {
                        let xOffset = 0;
                        if (d.velocity != 0) {
                            if (d.velocity == 1 || d.velocity == 2) {
                                xOffset = -5;
                            } else if (d.velocity == 3 || d.velocity == 4) {
                                xOffset = 30;
                            } else if (d.velocity == 5 || d.velocity == 6) {
                                xOffset = -5;
                            } else {
                                xOffset = -30;
                            }
                        }
                        return xOffset;
                    });
                text.attr("y", (d: SystemDiagramElement) => {
                    let yOffset = 0;
                    if (d.velocity != 0) {
                        if (d.velocity == 1 || d.velocity == 2) {
                            yOffset = -37;
                        } else if (d.velocity == 3 || d.velocity == 4) {
                            yOffset = 7;
                        } else if (d.velocity == 5 || d.velocity == 6) {
                            yOffset = 38;
                        } else {
                            yOffset = 0;
                        }
                    }
                    return yOffset;
                });
            }
        });

        // determine whether mouse is near edge of element
        image.on("mouseenter", function () {
            graph.edgeCircle.style("display", "none");
        })
            .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d3.select(this.parentNode.parentNode.parentNode), d);
            })
            .on("mouseleave", function (e) {
                graph.setEdgeMarkerVisible.call(graph, e);
            });

        // edgeMouseUp
        box.on("mousemove", function (e) {
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

    pathExtraRendering(paths: BGBondSelection) {
        let graph = this;

        // update existing bondSelection
        paths.classed(this.selectedClass, function (d) {
            return graph.selectedGroup.find(p => p == d) != null;
        }).attr("d", function (d: GraphBond) { return graph.drawPath.call(graph, d); });

        paths.classed("hoverablePath", true);
        if (paths.node()) {
            d3.select(paths.node().parentNode).selectAll("text").html(null);
        }
        paths.each(e => {
            if (e.velocity != 0) {
                let velocityClass = "";
                let xOffset = 0;
                let yOffset = 0;
                let mult = Math.abs(Math.cos((Math.atan2(e.source.y - e.target.y, e.target.x - e.source.x) + Math.PI) % (2 * Math.PI)));
                if (e.velocity == 1 || e.velocity == 2) {
                    velocityClass = "topVelocity";
                    yOffset = -7 * mult;
                    xOffset = -3;
                } else if (e.velocity == 3 || e.velocity == 4) {
                    velocityClass = "rightVelocity";
                    yOffset = 7 * mult;
                    xOffset = 0;
                } else if (e.velocity == 5 || e.velocity == 6) {
                    velocityClass = "bottomVelocity";
                    yOffset = 7 * mult;
                    xOffset = -5;
                } else {
                    velocityClass = "leftVelocity";
                    yOffset = -7 * mult;
                    xOffset = 0;
                }

                d3.select(paths.node().parentNode).append("text").classed("velocityArrow " + velocityClass, true)
                    .text(graph.velocityMap[e.velocity]).attr("x", (e.target.x - e.source.x) / 2 + e.source.x + xOffset).attr("y",
                        (e.target.y - e.source.y) / 2 + e.source.y + yOffset);
            }
        });
    }

    selectionHasAnyElement() {
        return this.selectedGroup.find(e => e instanceof SystemDiagramElement);
    }

    selectionHasBond(bond: GraphBond) {
        return this.selectedGroup.find(e => e == bond);
    }

    selectionHasNode(node: SystemDiagramElement) {
        return this.selectedGroup.find(e => e == node);
    }

    updateModifierMenu() {
        if (this.selectedGroup.length > 0 && this.selectionHasAnyElement()) {
            let selectedElements = this.selectedGroup.filter(el => el instanceof SystemDiagramElement) as SystemDiagramElement[];
            let allAllowedModifiers = [];
            let selectedModifiers = [0, 0, 0, 0, 0, 0, 0];
            for (const e of selectedElements) {
                allAllowedModifiers = allAllowedModifiers.concat(ElementNamespace.elementTypes[e.type].allowedModifiers);
                e.modifiers.forEach(m => selectedModifiers[m]++);
            }
            selectedModifiers = selectedModifiers.map(m => {
                if (m == selectedElements.length) {
                    return 2;
                } else if (m > 0) {
                    return 1;
                }
                return 0;
            });
            DotNet.invokeMethodAsync("BoGLWeb", "SetCheckboxes", selectedModifiers);
            DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", [...new Set(allAllowedModifiers)]);
        } else {
            DotNet.invokeMethodAsync("BoGLWeb", "ClearCheckboxes");
            DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", []);
        }
    }

    updateVelocityMenu() {
        DotNet.invokeMethodAsync("BoGLWeb", "SetVelocityDisabled", this.selectedGroup.length == 0);
        let velocities = [];
        for (const el of this.selectedGroup) {
            if (velocities.find(e => e == el.velocity) == null) {
                velocities.push(el.velocity);
            }
        }
        DotNet.invokeMethodAsync("BoGLWeb", "SetVelocity", velocities);
    }

    // remove bonds associated with a node
    spliceLinksForNode(el: SystemDiagramElement) {
        let graph = this;

        let toSplice = this.bonds.filter(function (l) {
            return (l.source === el || l.target === el);
        });
        toSplice.map(function (l) {
            graph.bonds.splice(graph.bonds.indexOf(l), 1);
        });
        this.updateVelocityMenu();
    }

    addSelectEdge(d3Bond: SVGSelection, bond: GraphBond) {
        this.selectedGroup.push(bond);
        this.updateVelocityMenu();
        this.updateGraph();
    }

    addSelectNode(d3Elem: SVGSelection, el: SystemDiagramElement) {
        this.selectedGroup.push(el);
        this.updateModifierMenu();
        this.updateVelocityMenu();
    }

    removeSelectFromEdge(edge: GraphBond) {
        let graph = this;
        graph.bondSelection.filter(cd => cd === edge).classed(graph.selectedClass, false);
        this.selectedGroup = this.selectedGroup.filter(e => e != edge);
        this.updateVelocityMenu();
    }

    removeSelectFromNode(el: SystemDiagramElement) {
        this.selectedGroup = this.selectedGroup.filter(e => e != el);
        this.updateModifierMenu();
        this.updateVelocityMenu();
        this.updateGraph();
    }

    clearSelection() {
        this.selectedGroup = [];
        this.updateModifierMenu();
        this.updateVelocityMenu();
        this.updateGraph();
    }

    pathMouseDown(d3Bond: SVGSelection, bond: GraphBond) {
        d3.event.stopPropagation();
        this.justClickedEdge = true;

        if (d3.event.ctrlKey || d3.event.metaKey) {
            if (this.selectionHasBond(bond)) {
                this.removeSelectFromEdge(bond);
            } else {
                this.addSelectEdge(d3Bond, bond);
            }
        } else {
            if (!this.selectionHasBond(bond)) {
                this.clearSelection();
                this.addSelectEdge(d3Bond, bond);
            }
        }

        this.updateGraph();
    }

    handleEdgeDown(el: SystemDiagramElement) {
        d3.event.stopPropagation();
        if (!this.edgeOrigin) {
            this.setFollowingEdge(el);
            d3.event.stopPropagation();
        }
    }

    handleEdgeUp(el: SystemDiagramElement) {
        d3.event.stopPropagation();
        let isCompatible = ElementNamespace.isCompatible(this.edgeOrigin, el, this);
        if (this.edgeOrigin && el !== this.edgeOrigin) {
            if (isCompatible) {
                let bond = new GraphBond(this.edgeOrigin, el, 0);
                this.bonds.push(bond);
                this.selectedGroup = [bond];
            }
            this.setFollowingEdge(null);
            this.updateGraph();
        } else if (!this.edgeOrigin) {
            this.setFollowingEdge(el);
            d3.event.stopPropagation();
        }
    }

    // mousedown on element
    nodeMouseDown(el: SystemDiagramElement) {
        d3.event.stopPropagation();
        this.mouseDownNode = el;
        this.justDragged = false;
    }

    nodeMouseUp(d3Elem: SVGSelection, el: SystemDiagramElement) {
        d3.event.stopPropagation();

        let isCompatible = ElementNamespace.isCompatible(this.edgeOrigin, el, this);
        this.mouseDownNode = null;

        if (this.edgeOrigin !== el && this.edgeOrigin !== null) {
            if (isCompatible) {
                let bond = new GraphBond(this.edgeOrigin, el);
                this.bonds.push(bond);
                this.selectedGroup = [bond];
            }
            this.setFollowingEdge(null);
            this.updateGraph();
        } else {
            // we"re in the same node
            if (!this.justDragged) {
                if (d3.event.ctrlKey || d3.event.metaKey) {
                    if (this.selectionHasNode(el)) {
                        this.removeSelectFromNode(el);
                    } else {
                        this.addSelectNode(d3Elem, el);
                    }
                } else {
                    if (!this.selectionHasNode(el)) {
                        this.clearSelection();
                        this.addSelectNode(d3Elem, el);
                    }
                }
                this.updateGraph();
            }
        }
        this.justDragged = false;
    }

    // mouseup on main svg
    svgMouseUp() {
        this.setFollowingEdge(null);
        if (this.justClickedEdge) {
            this.justClickedEdge = false;
        } else if (this.draggingElement != null) {
            this.selectedGroup = [];
            if (ElementNamespace.elementTypes[this.draggingElement].isMultiElement) {
                //Get mouse location
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());

                //Store the multi-element type so it can be accessed easier later in the function
                let multiElementType = <MultiElementType>ElementNamespace.elementTypes[this.draggingElement];

                //Create a list of the sub-elements
                let subElementList: SystemDiagramElement[] = [];

                //Place sub-elements
                for (let i = 0; i < multiElementType.subElements.length; i++) {
                    let subElementType = multiElementType.subElements[i];
                    let subElementOffset = multiElementType.offsets[i];
                    let element = new SystemDiagramElement(this.highestElemId++, subElementType, xycoords[0] + subElementOffset[0], xycoords[1] + subElementOffset[1], 0, []);
                    subElementList.push(element);
                    this.elements.push(subElementList[i]);
                    this.selectedGroup.push(element);
                }

                //Add edges between sub-elements
                for (let i = 0; i < multiElementType.subElementEdges.length; i++) {
                    let element1 = subElementList[multiElementType.subElementEdges[i][0]];
                    let element2 = subElementList[multiElementType.subElementEdges[i][1]];
                    let bond = new GraphBond(element1, element2, 0);
                    this.bonds.push(bond);
                    this.selectedGroup.push(bond);
                }
            } else {
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());
                let element = new SystemDiagramElement(this.highestElemId++, this.draggingElement, xycoords[0], xycoords[1], 0, []);
                this.elements.push(element);
                this.selectedGroup.push(element);
            }
            //Update the system diagram
            this.updateGraph();
        } else if (!this.justScaleTransGraph) {
            this.clearSelection();
            this.updateGraph();
        }
        if (this.justScaleTransGraph) {
            // dragged not clicked
            this.justScaleTransGraph = false;
        }
    }

    // keydown on main svg
    svgKeyDown() {
        // make sure repeated key presses don"t register for each keydown
        if (this.lastKeyDown !== -1) return;
        this.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;
        let graph = this;

        switch ((<KeyboardEvent>d3.event).keyCode) {
            case this.BACKSPACE_KEY:
            case this.DELETE_KEY:
                d3.event.preventDefault();
                for (let e of this.selectedGroup.filter(e => e instanceof GraphBond) as GraphBond[]) {
                    this.bonds.splice(this.bonds.indexOf(e), 1);
                    this.bonds = this.bonds.filter(bond => bond != e);
                }
                for (let e of this.selectedGroup.filter(e => e instanceof SystemDiagramElement) as SystemDiagramElement[]) {
                    this.elements.splice(this.elements.indexOf(e), 1);
                    graph.spliceLinksForNode(e);
                    this.elements = this.elements.filter(el => el != e);
                }
                this.updateModifierMenu();
                this.updateVelocityMenu();
                this.selectedGroup = [];
                this.updateGraph();
                break;
        }
    }

    svgKeyUp() {
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

    dragmove(el: SystemDiagramElement) {
        if (this.mouseDownNode) {
            if (!this.selectedGroup.includes(el)) {
                this.selectedGroup = [el];
            }

            for (const el of this.selectedGroup.filter(e => e instanceof SystemDiagramElement) as SystemDiagramElement[]) {
                el.x += (<DragEvent>d3.event).dx;
                el.y += (<DragEvent>d3.event).dy;
            }

            this.updateGraph();
        }
    }

    zoomed() {
        if (!this.edgeOrigin) {
            this.justScaleTransGraph = true;
            if (this.prevScale !== d3.event.scale || d3.event.sourceEvent.buttons == 2) {
                this.changeScale((<ZoomEvent>d3.event).translate[0], (<ZoomEvent>d3.event).translate[1], (<ZoomEvent>d3.event).scale, false);
            }
        }
    };
}