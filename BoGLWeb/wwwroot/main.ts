"use strict";
import { BaseGraph } from "./types/BaseGraph";
import { BondGraph } from "./types/BondGraph";
import { BondGraphBond } from "./types/BondGraphBond";
import { BondGraphElement } from "./types/elements/BondGraphElement";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { ElementType } from "./types/elements/ElementType";
import { SystemDiagram } from "./types/SystemDiagram";

function makeElementSource(graph: BaseGraph, section: HTMLElement, elementID: number, link: string) {
    const group = document.createElement('div');

    group.classList.add("groupDiv");
    group.addEventListener("mousedown", function () {
        document.body.style.cursor = "grabbing";
        graph.draggingElement = elementID;
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

function makeSection(graph: BaseGraph, sectionName: string, elements: ElementType[]) {
    let sectionElem = document.getElementById(sectionName);
    elements.forEach(e => makeElementSource(graph, sectionElem, e.id, "images/elements/" + e.image + ".svg"));
}

function populateMenu(graph: BaseGraph) {
    ElementNamespace.categories.map((c, i) => {
        makeSection(graph, c.folderName, ElementNamespace.elementTypes.filter(e => e.category === i));
    });
}

function loadPage() {
    var systemDiagramSVG = d3.select("#systemDiagram").append("svg");
    systemDiagramSVG.classed("graphSVG", true);

    var systemDiagram = new SystemDiagram(systemDiagramSVG, [], []);
    systemDiagram.draggingElement = null;

    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        systemDiagram.draggingElement = null;
    });

    populateMenu(systemDiagram);
    systemDiagram.updateGraph();

    var bondGraphSVG = d3.select("#bondGraph").append("svg");
    bondGraphSVG.classed("graphSVG", true);
    let n1 = new BondGraphElement(0, "1", 50, 50);
    let n2 = new BondGraphElement(1, "R:b", 50, -50);
    let n3 = new BondGraphElement(2, "I:m", 150, 50);
    let n4 = new BondGraphElement(3, "C:1/k", 50, 150);
    let n5 = new BondGraphElement(4, "Se:F(t)", -50, 50);
    var bondGraph = new BondGraph(bondGraphSVG, [n1, n2, n3, n4, n5], [new BondGraphBond(n1, n2, "flat", "arrow"), new BondGraphBond(n1, n3, "", "flat_and_arrow"),
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