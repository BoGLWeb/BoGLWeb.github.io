"use strict";
import { BaseGraph } from "./types/BaseGraph";
import { SystemDiagram } from "./types/SystemDiagram";

function makeElementSource(graph: BaseGraph, section: HTMLElement, link: string) {
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

function makeSection(graph: BaseGraph, sectionName: string, images: string[]) {
    let sectionElem = document.getElementById(sectionName);
    images.forEach(image => makeElementSource(graph, sectionElem, "images/" + sectionName + "/" + image + ".svg"));
}

function populateMenu(graph: BaseGraph) {
    // eventually want to pass these in from C#, which can likely access files easier
    makeSection(graph, "mechTrans", ["mass", "spring", "damper", "ground", "force_input", "gravity", "velocity_input"]);
    makeSection(graph, "mechRot", ["flywheel", "spring", "damper", "torque_input", "velocity_input"]);
    makeSection(graph, "transElem", ["lever", "pulley", "belt", "shaft", "gear", "gear_pair", "rack", "rack_pinion"]);
    makeSection(graph, "electrical", ["inductor", "capacitor", "resistor", "transformer", "junction_palette", "ground", "current_input", "voltage_input"]);
    makeSection(graph, "actuators", ["pm_motor", "vc_transducer"]);
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
    let node1 = new BondGraphElement(0, "images/mechTrans/mass.svg", 50, 50);
    let node2 = new BondGraphElement(1, "images/mechTrans/ground.svg", 200, 200);
    var bondGraph = new BaseGraph(bondGraphSVG, [node1, node2], [new BondGraphBond(node1, node2)]);
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