"use strict";
import { BaseGraphDisplay } from "./types/display/BaseGraphDisplay";
import { BondGraphDisplay } from "./types/display/BondGraphDisplay";
import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { BondGraphElement } from "./types/elements/BondGraphElement";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { GraphBond } from "./types/bonds/GraphBond";
import { SystemDiagramElement } from "./types/elements/SystemDiagramElement";
import { SystemDiagram } from "./types/graphs/SystemDiagram";

function populateMenu(graph: BaseGraphDisplay) {
    ElementNamespace.categories.map((c, i) => {
        ElementNamespace.elementTypes.filter(e => e.category === i).forEach(e => {
            const group = document.createElement('div');

            group.classList.add("groupDiv");
            group.addEventListener("mousedown", function () {
                document.body.style.cursor = "grabbing";
                graph.draggingElement = e.id;
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

function loadPage() {
    var systemDiagramSVG = d3.select("#systemDiagram").append("svg");
    systemDiagramSVG.classed("graphSVG", true);

    var systemDiagram = new SystemDiagramDisplay(systemDiagramSVG, [], []);
    systemDiagram.draggingElement = null;

    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        systemDiagram.draggingElement = null;
    });

    populateMenu(systemDiagram);
    systemDiagram.updateGraph();

    var bondGraphSVG = d3.select("#bondGraph").append("svg");
    bondGraphSVG.classed("graphSVG", true);

    let systemDiagramString = '{"elements":[{"type":16,"x":-198.55480367585608,"y":-80.42269005847913,"modifiers":[6,3],"velocity":2},{"type":16,"x":52.111862990811005,"y":-80.42269005847902,"modifiers":[3],"velocity":0},{"type":24,"x":-160.33258145363413,"y":-40.42269005847925,"modifiers":[],"velocity":3},{"type":24,"x":92.11186299081078,"y":-40.42269005847925,"modifiers":[],"velocity":0},{"type":5,"x":-45.49924812030031,"y":68.02175438596555,"modifiers":[],"velocity":0},{"type":22,"x":-84.33133825239054,"y":335.11693698114834,"modifiers":[],"velocity":0},{"type":22,"x":130.33532841427643,"y":149.78360364781452,"modifiers":[],"velocity":0},{"type":22,"x":-296.33133825239065,"y":156.45027031448126,"modifiers":[],"velocity":0}],"edges":[{"source":7,"target":5,"velocity":1},{"source":6,"target":5,"velocity":0}]}';
    let parsedJson = JSON.parse(systemDiagramString);
    let elements = []
    for (let element of parsedJson.elements) {
        elements.push(element as unknown as SystemDiagramElement);
    }
    let edges = []
    for (let edge of parsedJson.edges) {
        edges.push(new GraphBond(elements[edge.source], elements[edge.target]));
    }
    console.log(new SystemDiagram(elements, edges));

    // example bond graph
    let n1 = new BondGraphElement(0, "1", 50, 50);
    let n2 = new BondGraphElement(1, "R:b", 50, -50);
    let n3 = new BondGraphElement(2, "I:m", 150, 50);
    let n4 = new BondGraphElement(3, "C:1/k", 50, 150);
    let n5 = new BondGraphElement(4, "Se:F(t)", -50, 50);
    var bondGraph = new BondGraphDisplay(bondGraphSVG, [n1, n2, n3, n4, n5], [new BondGraphBond(n1, n2, "flat", "arrow"), new BondGraphBond(n1, n3, "", "flat_and_arrow"),
    new BondGraphBond(n1, n4, "flat", "arrow"), new BondGraphBond(n1, n5, "flat_and_arrow", "")]);
    bondGraph.updateGraph();
}

function pollDOM() {
    const el = document.getElementById('graphMenu');

    if (el != null) {
        loadPage();
    } else {
        setTimeout(pollDOM, 20);
    }
}

pollDOM();
