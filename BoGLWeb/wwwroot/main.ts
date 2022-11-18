"use strict";
import { BaseGraphDisplay } from "./types/display/BaseGraphDisplay";
import { BondGraphDisplay } from "./types/display/BondGraphDisplay";
import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { BondGraphElement } from "./types/elements/BondGraphElement";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { backendManager } from "./backendManager";
import { BondGraph } from "./types/graphs/BondGraph";

export function populateMenu(graph: BaseGraphDisplay) {
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
    (<any>window).backendManager = backendManager;

    var bondGraphSVG = d3.select("#bondGraph").append("svg");
    bondGraphSVG.classed("graphSVG", true);

    // example bond graph
    let n1 = new BondGraphElement(0, "1", 50, 50);
    let n2 = new BondGraphElement(1, "R:b", 50, -50);
    let n3 = new BondGraphElement(2, "I:m", 150, 50);
    let n4 = new BondGraphElement(3, "C:1/k", 50, 150);
    let n5 = new BondGraphElement(4, "Se:F(t)", -50, 50);
    var bondGraph = new BondGraphDisplay(bondGraphSVG, new BondGraph([n1, n2, n3, n4, n5], [new BondGraphBond(n1, n2, "flat", "arrow"), new BondGraphBond(n1, n3, "", "flat_and_arrow"),
    new BondGraphBond(n1, n4, "flat", "arrow"), new BondGraphBond(n1, n5, "flat_and_arrow", "")]));
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
