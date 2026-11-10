import { ZoomEvent } from "../../type_libraries/d3";
import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { GraphBond } from "../bonds/GraphBond";
import { ElementNamespace } from "../elements/ElementNamespace";
import { SystemDiagramElement } from "../elements/SystemDiagramElement";
import { SystemDiagram } from "../graphs/SystemDiagram";
import { BaseGraphDisplay } from "./BaseGraphDisplay";
import { MultiElementType } from "../elements/MultiElementType";
import { GraphElement } from "../elements/GraphElement";
import { backendManager } from "../../backendManager";

// describes the specialized functionality needed for a system diagram
export class SystemDiagramDisplay extends BaseGraphDisplay {
    private dismissBondMenu: () => void;

    setSketchBordersVisible(visible: boolean) {
        // Keep the preference on the canvas so it survives graph redraws and loads.
        this.svg.classed("hideSketchBorders", !visible);
    }

    closeBondMenu() {
        if (this.dismissBondMenu) this.dismissBondMenu();
    }

    updateGraph(dragmove: boolean = false) {
        this.closeBondMenu();
        super.updateGraph(dragmove);
    }

    // Increase this to zoom sketch artwork inside its fixed border (for example, 1.5).
    sketchImageScale: number = 2;

    // velocity constants
    velocityOffsets = [[-15, -37], [-5, -37], [30, -5], [30, 7], [18, 40], [3, 40], [-30, 10], [-30, 0]];
    velocityMap = {
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

    // edge and edge creation variables
    edgeCircle: SVGSelection;
    rejectX: SVGSelection;
    edgeOrigin: SystemDiagramElement = null;
    justClickedEdge: boolean = false;
    sketchBondGraphType: string = "unsimplified";

    // element and edge variables
    selectedElements: SystemDiagramElement[] = [];
    copiedElements: SystemDiagramElement[] = [];
    copiedEdges: GraphBond[] = [];
    ctrlPressed: boolean = false;
    elements: SystemDiagramElement[];

    constructor(svg: SVGSelection, systemDiagram: SystemDiagram) {
        super(svg, systemDiagram);

        // increments the highest element ID on element creation to prevent duplicate IDs
        this.highestElemId = systemDiagram.nodes.length + 1;
        this.edgeCircle = this.svgG.append("circle");
        this.edgeCircle.attr("r", "5")
            .attr("fill", "green")
            .attr("style", "cursor: pointer; display: none;")
            .style("pointer-events", "none");
        this.rejectX = this.svgG.append("path");
        this.rejectX
            .attr("d", d3.svg.symbol().type("cross").size(100))
            .style("fill", "red")
            .style("pointer-events", "none")
            .style("display", "none");
    }

    // returns a combined list of selected elements and bonds
    getSelection() {
        return ([] as (SystemDiagramElement | GraphBond)[]).concat(this.selectedElements).concat(this.selectedBonds);
    }

    // moves a green circle or red X around the perimeter of the hovered element
    moveIndicator(e: SystemDiagramElement) {
        d3.event.stopPropagation();
        let coordinates = d3.mouse(<Event>d3.event.currentTarget);
        let x = coordinates[0];
        let y = coordinates[1];
        let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
        let fourth = 1 / 4 * Math.PI;
        let s = 30;
        let coords = [];
        // handles quads 1, 2, 3, and 4
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

    // draws an edge that extends from a source node to the current mouse position
    setFollowingEdge(sourceNode: SystemDiagramElement) {
        this.edgeOrigin = sourceNode;
        if (sourceNode == null) {
            // hide edge if there is no source
            this.dragBond.classed("hidden", true);
        } else {
            let mouse = d3.mouse(this.svgG.node());
            let sourceEnd = this.getEdgePosition(sourceNode, { x: mouse[0], y: mouse[1] } as GraphElement);
            this.dragBond.attr("d", "M" + (sourceNode.x + sourceEnd[0]) + "," + (sourceNode.y + sourceEnd[1]) + "L" + mouse[0] + "," + mouse[1]);
            this.dragBond.classed("hidden", false);
        }
    }

    // sets the visibility of the edge marker
    setEdgeMarkerVisible(e: SystemDiagramElement) {
        if (!this.edgeOrigin || ElementNamespace.isCompatible(this.edgeOrigin, e, this)) {
            this.edgeCircle.style("display", "block");
            this.rejectX.style("display", "none");
        } else {
            this.rejectX.style("display", "block");
            this.edgeCircle.style("display", "none");
        }
    }

    // renders the system diagram elements
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

        let group = newElements.append("g");
        group.attr("style", "fill:inherit;")
            .attr("index", function (d, i) { return d.id.toString(); });

        // creates the invisible outer boundary that shows the edge indicator when hovered over
        let edgeHover = group.append("rect");
        edgeHover.attr("width", "80px")
            .attr("height", "80px")
            .attr("x", "-40px")
            .attr("y", "-40px")
            .style("pointer-events", "all")
            .classed("edgeHover", true)
            .on("mousemove", function (e) {
                graph.moveSelectionRect();
                graph.moveIndicator.call(graph, e);
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

        // creates the outline of the element
        let box = hoverBox.append("rect");
        box.classed("outline", true)
            .classed("sketchOutline", d => ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch)
            .attr("width", "60px")
            .attr("height", "60px")
            .attr("x", "-30px")
            .attr("y", "-30px");

        // adds the image representing the element
        // A fixed viewport crops enlarged artwork without changing the outline.
        const imageViewport = hoverBox.append("svg")
            .attr("x", -25)
            .attr("y", -25)
            .attr("width", 50)
            .attr("height", 50)
            .style("overflow", "hidden");
        let image = imageViewport.append("image");
        image.attr("href", function (d) {
            if(!ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch) {
                return "/images/elements/" + ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].image + ".svg";
            } else {
                return "/images/sketch/" + ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].image + ".svg";
            }
        })
            .classed("hoverImg", true)
            .attr("x", d => ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch ? 25 * (1 - graph.sketchImageScale) : 0)
            .attr("y", d => ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch ? 25 * (1 - graph.sketchImageScale) : 0)
            .attr("preserveAspectRatio", "xMidYMid meet")
            .attr("height", d => ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch ? 50 * graph.sketchImageScale : 50)
            .attr("width", d => ElementNamespace.elementTypes[(<SystemDiagramElement>d).type].isSketch ? 50 * graph.sketchImageScale : 50);

        // adds an asterisk that is visible when the element has a modifier applied
        let asterisk = newElements.append("text");
        asterisk
            .text("*")
            .attr("x", "16")
            .attr("y", "-13")
            .style("font-size", "30px")
            .style("display", e => {
                return (e as SystemDiagramElement).modifiers.length > 0 ? "block" : "none";
            });

        group.selectAll("text").html(null);

        // adds velocity markers to elements
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
                            this.classList.add("velocity_" + d.velocity + "_element");
                        }
                    })
                    .attr("x", (d: SystemDiagramElement) => {
                        return d.velocity != 0 ? graph.velocityOffsets[d.velocity - 1][0] : 0;
                    })
                    .attr("y", (d: SystemDiagramElement) => {
                        return d.velocity != 0 ? graph.velocityOffsets[d.velocity - 1][1] : 0;
                    });
            }
        });

        // Keep the editable number inside the bottom-right corner of the border.
        newElements.filter(d => ElementNamespace.elementTypes[(d as SystemDiagramElement).type].isSketch)
            .append("text")
            .attr("x", 23).attr("y", 10)
            .attr("text-anchor", "end")
            .style("font-size", "14px").style("fill", "black")
            .style("stroke", "white").style("stroke-width", "3px")
            .style("paint-order", "stroke").style("cursor", "text")
            .style("pointer-events", "all")
            .text((d: SystemDiagramElement) => String(d.sketchNumber ?? d.id))
            .on("mousedown", () => d3.event.stopPropagation())
            .on("mouseup", () => d3.event.stopPropagation())
            .on("dblclick", () => d3.event.stopPropagation())
            .on("click", function (d: SystemDiagramElement) {
                d3.event.stopPropagation();
                graph.editSketchNumber(d);
            })
            .append("title").text("Click to edit number");

        // initializes mouse events for image
        image.on("mouseenter", function () {
            graph.edgeCircle.style("display", "none");
        })
            .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d);
            })
            .on("mouseleave", function (e) {
                graph.setEdgeMarkerVisible.call(graph, e);
            });
        
        // initializes mouse events for outline box
        box.on("mousemove", function (e) {
            graph.moveSelectionRect();
            graph.moveIndicator.call(graph, e);
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

    private editSketchNumber(element: SystemDiagramElement) {
        const dialog = document.createElement("dialog");
        dialog.addEventListener("keydown", event => event.stopPropagation());
        dialog.addEventListener("keyup", event => event.stopPropagation());
        const form = document.createElement("form");
        form.method = "dialog";
        const label = document.createElement("label");
        label.textContent = "Element number: ";
        const input = document.createElement("input");
        input.type = "number";
        input.step = "any";
        input.required = true;
        input.value = String(element.sketchNumber ?? element.id);
        label.appendChild(input);
        form.appendChild(label);
        const save = document.createElement("button");
        save.textContent = "Save";
        save.type = "submit";
        form.appendChild(save);
        const cancel = document.createElement("button");
        cancel.textContent = "Cancel";
        cancel.type = "button";
        cancel.onclick = () => dialog.close();
        form.appendChild(cancel);
        form.onsubmit = event => {
            event.preventDefault();
            if (!input.checkValidity() || !Number.isFinite(input.valueAsNumber)) return;
            const previous = element.sketchNumber ?? element.id;
            const value = input.valueAsNumber;
            if (previous !== value) {
                const before = JSON.stringify({ id: element.id, number: previous });
                const after = JSON.stringify({ id: element.id, number: value });
                element.sketchNumber = value;
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSketchNumber", before, after);
                this.updateGraph();
            }
            dialog.close();
        };
        dialog.appendChild(form);
        dialog.addEventListener("close", () => dialog.remove());
        document.body.appendChild(dialog);
        dialog.showModal();
        input.focus();
        input.select();
    }

    restoreSketchNumber(state: string) {
        const saved = JSON.parse(state) as { id: number; number: number };
        const element = this.elements.find(e => e.id === saved.id);
        if (element) {
            element.sketchNumber = saved.number;
            this.updateGraph();
        }
    }

    // completes extra path rendering for system diagram paths
    pathExtraRendering(paths: BGBondSelection, pathGroup: BGBondSelection) {
        let graph = this;

        paths.classed("hoverablePath", true);
        const keepContextSelection = () => {
            if (d3.event.button === 2 && graph === window.sketchDiagram && graph.sketchBondGraphType === "causal") {
                d3.event.preventDefault();
                d3.event.stopPropagation();
            }
        };
        paths.on("mouseup.bondContextMenu", keepContextSelection);

        if (this === window.sketchDiagram && this.sketchBondGraphType !== "unsimplified") {
            paths.each(function (bond: GraphBond) {
                graph.drawSketchBondTick.call(graph, d3.select(this.parentNode), bond);
                d3.select(this.parentNode).selectAll(".sketchBondTick")
                    .style("pointer-events", graph.sketchBondGraphType === "causal" ? "stroke" : "none")
                    .on("mousedown", function () {
                        if (d3.event.button === 2) {
                            d3.event.preventDefault();
                            d3.event.stopPropagation();
                        } else {
                            graph.pathMouseDown(bond);
                        }
                    })
                    .on("mouseup", keepContextSelection)
                    .on("contextmenu", function () {
                        d3.event.preventDefault();
                        d3.event.stopPropagation();
                        graph.pathContextMenu(bond);
                    });
            });
        }

        // removes velocity arrows because they're not generated directly through d3 and are therefore not removed on update
        // if we can find a way to do this through D3 this removal would no longer be needed, which would be cool, but haven't found that yet
        this.svgG.selectAll("g:not(.boglElem) > g > .velocityArrow").remove()
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
                } else if (v == 4) {
                    velocityClass = "rightVelocityMath";
                    yOffset = 7 * mult;
                    xOffset = 0;
                } else if (v == 5) {
                    velocityClass = "rightVelocityMath";
                    yOffset = 7 * mult;
                    xOffset = -3;
                } else if (v == 6) {
                    velocityClass = "bottomVelocityMath";
                    yOffset = 7 * mult;
                    xOffset = 3;
                } else if (v == 7) {
                    velocityClass = "bottomVelocity";
                    yOffset = 7 * mult;
                    xOffset = 0;
                } else if (v == 1 || v == 8) {
                    velocityClass = "leftVelocity";
                    yOffset = -7 * mult;
                    xOffset = 0;
                }

                d3.select(paths[0][i].parentNode).append("text").classed("velocityArrow " + velocityClass + " velocity_" + v + "_edge", true)
                    .text(graph.velocityMap[e.velocity]).attr("x", (e.target.x - e.source.x) / 2 + e.source.x + xOffset).attr("y",
                        (e.target.y - e.source.y) / 2 + e.source.y + yOffset);
            }
        });
    }

    drawSketchBondTick(pathGroup: SVGSelection, bond: GraphBond) {
        let sourceEnd = this.getEdgePosition(bond.source, bond.target);
        let targetEnd = this.getEdgePosition(bond.target, bond.source);

        let x1 = bond.source.x + sourceEnd[0];
        let y1 = bond.source.y + sourceEnd[1];
        let x2 = bond.target.x + targetEnd[0];
        let y2 = bond.target.y + targetEnd[1];

        let dx = x2 - x1;
        let dy = y2 - y1;
        let length = Math.sqrt(dx * dx + dy * dy);

        if (length === 0) {
            return;
        }

        let unitX = dx / length;
        let unitY = dy / length;

        let tickLength = 16;

        let angle = -135 * Math.PI / 180;

        let tickDirX =
            unitX * Math.cos(angle) -
            unitY * Math.sin(angle);

        let tickDirY =
            unitX * Math.sin(angle) +
            unitY * Math.cos(angle);

        let tickX1 = x2;
        let tickY1 = y2;

        let tickX2 = x2 + tickDirX * tickLength;
        let tickY2 = y2 + tickDirY * tickLength;

        let isSelected = this.selectedBonds.includes(bond);

        pathGroup.append("path")
            .classed("sketchBondTick", true)
            .classed(this.selectedClass, isSelected)
            .attr(
                "d",
                `M${tickX1},${tickY1} L${tickX2},${tickY2}`
            );

        if (this.sketchBondGraphType === "causal") {
            let isAtSource = bond.causalStrokeDirection ?? true;
            let cx = isAtSource ? x1 : x2;
            let cy = isAtSource ? y1 : y2;

            let perpX = -unitY;
            let perpY = unitX;
            let causalHalfLength = 10;

            let causalX1 = cx - perpX * causalHalfLength;
            let causalY1 = cy - perpY * causalHalfLength;
            let causalX2 = cx + perpX * causalHalfLength;
            let causalY2 = cy + perpY * causalHalfLength;

            pathGroup.append("path")
                .classed("sketchBondTick", true)
                .classed(this.selectedClass, isSelected)
                .attr(
                    "d",
                    `M${causalX1},${causalY1} L${causalX2},${causalY2}`
                );
        }
    }

    // updates the modifier menu
    updateModifierMenu() {
        if (this.selectedElements.length > 0) {
            // if some elements are selected, update the modifier menu to match their modifiers
            let allAllowedModifiers = [];
            let selectedModifiers = [0, 0, 0, 0, 0, 0, 0];
            // count the number of times a modifier is applied to determine whether the checkbox is unchecked, partially
            // checked, or fully checked. Also record which modifiers are allowed for the selected elements
            for (const e of this.selectedElements) {
                allAllowedModifiers = allAllowedModifiers.concat(ElementNamespace.elementTypes[e.type].allowedModifiers);
                e.modifiers.forEach(m => selectedModifiers[m]++);
            }
            // if a modifier count matches the number of elements in the selection, check it fully;
            // if it is lower than that but not 0, check partially, otherwise leave unchecked
            selectedModifiers = selectedModifiers.map(m => {
                if (m == this.selectedElements.length) {
                    return 2;
                } else if (m > 0) {
                    return 1;
                }
                return 0;
            });
            DotNet.invokeMethodAsync("BoGLWeb", "SetCheckboxes", selectedModifiers);
            DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", [...new Set(allAllowedModifiers)]);
        } else {
            // if no elements are selected, clear checkboxes and disable them all
            DotNet.invokeMethodAsync("BoGLWeb", "ClearCheckboxes");
            DotNet.invokeMethodAsync("BoGLWeb", "SetDisabled", []);
        }
    }

    // updates the velocity menu
    updateVelocityMenu() {
        // if nothing is selected, disable the velocity menu, otherwise enable it
        DotNet.invokeMethodAsync("BoGLWeb", "SetVelocityDisabled", this.selectedElements.length == 0 && this.selectedBonds.length == 0);
        let velocities = [];
        for (const el of this.getSelection()) {
            if (velocities.find(e => e == el.velocity) == null) {
                velocities.push(el.velocity);
            }
        }
        DotNet.invokeMethodAsync("BoGLWeb", "SetVelocity", velocities);
    }

    // handles clicking on an edge
    pathMouseDown(bond: GraphBond) {
        this.justClickedEdge = true;
        super.pathMouseDown(bond);
    }

    // Opens editing actions for causal sketch bonds without changing selection.
    pathContextMenu(bond: GraphBond) {
        this.closeBondMenu();
        if (this !== window.sketchDiagram || this.sketchBondGraphType !== "causal") return;
        const event = d3.event as MouseEvent;
        const menu = document.createElement("div");
        menu.className = "sketchBondMenu";
        menu.setAttribute("role", "group");
        menu.setAttribute("aria-label", "Bond actions");
        const close = () => this.closeBondMenu();
        const outside = (e: Event) => { if (!menu.contains(e.target as Node)) close(); };
        const keyboard = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                e.preventDefault();
                e.stopPropagation();
                close();
            }
        };
        this.dismissBondMenu = () => {
            menu.remove();
            document.removeEventListener("pointerdown", outside, true);
            document.removeEventListener("keydown", keyboard, true);
            window.removeEventListener("scroll", close, true);
            window.removeEventListener("resize", close);
            this.dismissBondMenu = null;
        };
        const addAction = (label: string, reverse: boolean) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = label;
            button.addEventListener("click", () => {
                close();
                if (!this.bonds.includes(bond) || this !== window.sketchDiagram ||
                    window.tabNum !== "2" || this.sketchBondGraphType !== "causal") return;
                const before = this.sketchBondState(bond);
                if (reverse) [bond.source, bond.target] = [bond.target, bond.source];
                bond.causalStrokeDirection = !(bond.causalStrokeDirection ?? true);
                const after = this.sketchBondState(bond);
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSketchBond", before, after);
                this.updateGraph();
            });
            menu.appendChild(button);
        };
        addAction("Switch causal tick side", false);
        addAction("Reverse arrow", true);
        document.body.appendChild(menu);
        const bounds = menu.getBoundingClientRect();
        menu.style.left = Math.max(0, Math.min(event.clientX, window.innerWidth - bounds.width)) + "px";
        menu.style.top = Math.max(0, Math.min(event.clientY, window.innerHeight - bounds.height)) + "px";
        document.addEventListener("pointerdown", outside, true);
        document.addEventListener("keydown", keyboard, true);
        window.addEventListener("scroll", close, true);
        window.addEventListener("resize", close);
        (menu.firstElementChild as HTMLButtonElement).focus({ preventScroll: true });
    }

    private sketchBondState(bond: GraphBond): string {
        return JSON.stringify({ source: bond.source.id, target: bond.target.id,
            causalStrokeDirection: bond.causalStrokeDirection ?? true });
    }

    restoreSketchBond(state: string) {
        const saved = JSON.parse(state) as { source: number; target: number; causalStrokeDirection: boolean };
        const bond = this.bonds.find(b =>
            (b.source.id === saved.source && b.target.id === saved.target) ||
            (b.source.id === saved.target && b.target.id === saved.source));
        const source = this.elements.find(e => e.id === saved.source);
        const target = this.elements.find(e => e.id === saved.target);
        if (bond && source && target) {
            bond.source = source;
            bond.target = target;
            bond.causalStrokeDirection = saved.causalStrokeDirection;
            this.updateGraph();
        }
    }

    // handles clicking on the edge of an element, in the invisible border around it, starts edge creation
    handleEdgeDown(el: SystemDiagramElement) {
        d3.event.stopPropagation();
        if (!this.edgeOrigin) {
            this.setFollowingEdge(el);
            d3.event.stopPropagation();
        }
    }

    // handle ending a click or drag on the edge of an element in the invisible portion, can add a new bond or start bond creation
    handleEdgeUp(el: SystemDiagramElement) {
        d3.event.stopPropagation();

        if (this.handleAreaSelectionEnd()) return;
        if (this.edgeOrigin && el !== this.edgeOrigin) {
             this.addBond(this.edgeOrigin, el);
        } else if (!this.edgeOrigin) {
            this.setFollowingEdge(el);
            d3.event.stopPropagation();
        }
    }

    // handles mousedown on element, can start edge creation
    nodeMouseDown(el: SystemDiagramElement) {
        d3.event.stopPropagation();
        this.mouseDownNode = el;
        if (this.edgeOrigin == el) {
            this.setFollowingEdge(null);
        }
        this.justDragged = false;
    }

    // sets the selection to an element list and edge list
    setSelection(elList: GraphElement[], bondList: GraphBond[]) {
        this.selectedElements = elList as SystemDiagramElement[];
        this.selectedBonds = bondList;
    }

    // adds an edge if the edge is valid
    addBond(source, target) {
        // checks whether the source and target are compatible
        let isCompatible = ElementNamespace.isCompatible(source, target, this);

        if (isCompatible) {
            // if compatible, creates an edge and selects it
            let bond = new GraphBond(source, target);
            this.bonds.push(bond);
            let selectedElements = this.selectedElements;
            let selectedBonds = this.selectedBonds;
            this.setSelection([], [bond]);
            this.setFollowingEdge(null);
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [JSON.stringify(bond)], ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
        } else {
            // if not compatible, stops edge creation
            this.setFollowingEdge(null);
            this.updateGraph();
        }
        this.updateMenus();
    }

    // handles ending a click or drag on an element (not the invisible border)
    nodeMouseUp(el: SystemDiagramElement) {
        d3.event.stopPropagation();

        this.mouseDownNode = null;
        if (this.handleAreaSelectionEnd()) return;

        if (this.edgeOrigin !== el && this.edgeOrigin !== null) {
            // if coming from an element that is not this one, make an edge
            this.addBond(this.edgeOrigin, el);
        } else {
            // we're in the same node, change the current selection
            if (!this.justDragged) {
                let addEl = [];
                let removeEl = [];
                let removeEdges = [];
                if (d3.event.ctrlKey || d3.event.metaKey) {
                    // if CTRL+click, toggle element inclusion in the current selection
                    if (this.selectionContains(el)) {
                        this.removeFromSelection(el, false);
                        removeEl = [el];
                    } else {
                        this.addToSelection(el, false);
                        addEl = [el];
                    }
                } else {
                    // if th element is not included in the current selection, replace the current selection with this element
                    if (!this.selectionContains(el)) {
                        addEl = [el];
                        removeEl = this.selectedElements;
                        removeEdges = this.selectedBonds;
                        this.setSelection([el], []);
                    }
                }
                // update graph and undo/redo
                this.updateGraph();
                DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...this.listToIDObjects(addEl.concat([])), ...this.listToIDObjects(removeEl.concat(removeEdges)));
                this.updateMenus();
            }
        }
        this.justDragged = false;
    }

    // update the modifier, velocity, and top menus
    updateMenus() {
        this.updateModifierMenu();
        this.updateVelocityMenu();
        this.updateTopMenu();
    }

    // handles mouseup on SVG background
    svgMouseUp() {
        this.setFollowingEdge(null);
        let selectedElements = this.selectedElements;
        let selectedBonds = this.selectedBonds;
        if (this.justClickedEdge) {
            this.justClickedEdge = false;
        } else if (this.draggingElement != null) {
            // if dragging an element from the element menu, add it to the system diagram
            let newElementSelection;
            let newBondSelection;
            // if the element is a multi-element, add the multi-element
            if (ElementNamespace.elementTypes[this.draggingElement].isMultiElement) {
                // get mouse location
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());

                // store the multi-element type so it can be accessed easier later in the function
                let multiElementType = <MultiElementType>ElementNamespace.elementTypes[this.draggingElement];

                // create a list of the sub-elements
                let subElementList: SystemDiagramElement[] = [];
                let subBondList: GraphBond[] = [];

                // place sub-elements
                for (let i = 0; i < multiElementType.subElements.length; i++) {
                    let subElementType = multiElementType.subElements[i];
                    let subElementOffset = multiElementType.offsets[i];
                    let element = new SystemDiagramElement(this.highestElemId++, subElementType, xycoords[0] + subElementOffset[0], xycoords[1] + subElementOffset[1], 0, []);
                    subElementList.push(element);
                    this.elements.push(subElementList[i]);
                    this.addToSelection(element, false);
                }

                // add edges between sub-elements
                for (let i = 0; i < multiElementType.subElementEdges.length; i++) {
                    let element1 = subElementList[multiElementType.subElementEdges[i][0]];
                    let element2 = subElementList[multiElementType.subElementEdges[i][1]];
                    let bond = new GraphBond(element1, element2, 0);
                    this.bonds.push(bond);
                    this.addToSelection(bond, false);
                    subBondList.push(bond);
                }

                newElementSelection = subElementList;
                newBondSelection = subBondList;
            } else {
                document.body.style.cursor = "auto";
                let xycoords = d3.mouse(this.svgG.node());
                let element = new SystemDiagramElement(this.highestElemId++, this.draggingElement, xycoords[0], xycoords[1], 0, []);
                this.elements.push(element);
                newElementSelection = [element];
                newBondSelection = [];
            }
            // update the system diagram
            this.setSelection(newElementSelection, newBondSelection);
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [].concat(newElementSelection).concat(newBondSelection).map(e => JSON.stringify(e)), ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
            this.updateMenus();
        } else if (!this.justScaleTransGraph) {
            // if the background is clicked, clear the selection
            this.setSelection([], []);
            this.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), [], [], ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)));
            this.updateMenus();
        }
        if (this.justScaleTransGraph) {
            // the graph was dragged, not clicked
            this.justScaleTransGraph = false;
        }
    }

    // checks whether a CTRL combo has occurred
    checkCtrlCombo(a: number) {
        return (d3.event.keyCode == a && this.ctrlPressed) || (this.ctrlPressed && this.lastKeyDown == a);
    }

    // copies the selection
    copySelection() {
        this.copiedElements = this.selectedElements.map(e => e.copy(this.highestElemId++, 75));
        this.copiedEdges = this.selectedBonds.filter(b => this.selectionContains(b.source) && this.selectionContains(b.target))
            .map(b => b.copy(this.copiedElements[this.selectedElements.findIndex(a => a.id == b.source.id)], this.copiedElements[this.selectedElements.findIndex(a => a.id == b.target.id)]));
        DotNet.invokeMethodAsync("BoGLWeb", "SetHasCopied", true);
    }

    // deletes the selection, with an option to disable confirmation for deleting more than one thing
    async deleteSelection(needsConfirmation = true) {
        let result;
        let graph = this;

        // if the deletion needs confirmation, open the confirmation modal
        if (needsConfirmation) {
            result = await DotNet.invokeMethodAsync("BoGLWeb", "showDeleteConfirmationModal", this.getSelection().length > 1);
        }

        if (!needsConfirmation || result) {
            // if deletion is not refused, delete the selection
            let splicedBonds = [];
            // delete selected bonds
            for (let e of this.selectedBonds) {
                this.bonds = this.bonds.filter(bond => bond != e);
            }
            // delete selected elements and the bonds connected to them
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
    }

    // pastes the currently copied selection
    paste() {
        let selectedElements = this.selectedElements;
        let selectedBonds = this.selectedBonds;
        this.elements = this.elements.concat(this.copiedElements);
        this.bonds = this.bonds.concat(this.copiedEdges);
        this.setSelection(this.copiedElements, this.copiedEdges);
        this.updateGraph();
        DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", [].concat(this.copiedElements).concat(this.copiedEdges).map(e => JSON.stringify(e)),
            ...this.listToIDObjects([].concat(selectedElements).concat(selectedBonds)), true);
        this.copySelection();
        this.updateMenus();
    }

    // handles keydown on main svg, determines current and last key pressed to determine what action should be taken
    async svgKeyDown() {
        let graph = this;

        if (this.lastKeyDown == (<KeyboardEvent>d3.event).keyCode) return;
        if (!this.ctrlPressed) {
            this.ctrlPressed = (<KeyboardEvent>d3.event).keyCode == this.CTRL_KEY;
        }

        // makes sure repeated key presses don't register for each keydown
        this.lastKeyDown = (<KeyboardEvent>d3.event).keyCode;

        // handle single key commands
        switch ((<KeyboardEvent>d3.event).keyCode) {
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

        // handle multi-key command combos
        if (this.checkCtrlCombo(this.A_KEY)) {
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelection", parseInt(window.tabNum), ...graph.listToIDObjects(
                [].concat(this.elements.filter(e => !this.selectedElements.includes(e as SystemDiagramElement))).concat(this.bonds.filter(e => !this.selectedBonds.includes(e)))), [], []);
            this.setSelection(this.elements, this.bonds);
            this.updateGraph();
            this.updateMenus();
        } else if (this.checkCtrlCombo(this.C_KEY)) {
            this.copySelection();
        } else if (this.checkCtrlCombo(this.X_KEY)) {
            this.copySelection();
            this.deleteSelection();
        } else if (this.checkCtrlCombo(this.V_KEY)) {
            this.paste();
        } else if (this.checkCtrlCombo(this.Z_KEY)) {
            backendManager.getBackendManager().handleUndoRedo(true);
        } else if (this.checkCtrlCombo(this.Y_KEY)) {
            backendManager.getBackendManager().handleUndoRedo(false);
        }
    }

    // when the mouse moves on the SVG background, hide the edge indicator
    svgMouseMove() {
        this.edgeCircle.style("display", "none");
        this.rejectX.style("display", "none");
        if (this.edgeOrigin) {
            this.dragmoveEdge();
        }
    }

    // record whether we're in the middle of a CTRL combo
    svgKeyUp() {
        if (d3.event.keyCode == this.CTRL_KEY) {
            this.ctrlPressed = false;
        }
        this.lastKeyDown = -1;
    }

    // handles edge drag behavior
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

    // draws an edge that follows the mouse from a source node if the source node is not null
    dragmoveEdge() {
        if (this.edgeOrigin) {
            let mouse = d3.mouse(this.svgG.node());
            let sourceEnd = this.getEdgePosition(this.edgeOrigin, { x: mouse[0], y: mouse[1] } as GraphElement);
            this.dragBond.attr("d", "M" + (this.edgeOrigin.x + sourceEnd[0]) + "," + (this.edgeOrigin.y + sourceEnd[1]) + "L" + mouse[0] + "," + mouse[1]);
        }
    }

    // handles all scaling and translation in the system diagram
    zoomed() {
        if (!this.edgeOrigin) {
            this.justScaleTransGraph = true;
            if (this.prevScale !== d3.event.scale || d3.event.sourceEvent.buttons == 2) {
                this.changeScale((<ZoomEvent>d3.event).translate[0], (<ZoomEvent>d3.event).translate[1], (<ZoomEvent>d3.event).scale);
            }
        }
    };
}
