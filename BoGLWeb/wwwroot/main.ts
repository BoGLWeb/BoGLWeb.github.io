"use strict";
import { BaseGraph } from "./types/baseGraph";

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
    // eventually want to pass these in from C#, which can likely access files easier
    makeSection(graph, "mechTrans", ["mass", "spring", "damper", "ground", "force_input", "gravity", "velocity_input"]);
    makeSection(graph, "mechRot", ["flywheel", "spring", "damper", "torque_input", "velocity_input"]);
    makeSection(graph, "transElem", ["lever", "pulley", "belt", "shaft", "gear", "gear_pair", "rack", "rack_pinion"]);
    makeSection(graph, "electrical", ["inductor", "capacitor", "resistor", "transformer", "junction_palette", "ground", "current_input", "voltage_input"]);
    makeSection(graph, "actuators", ["pm_motor", "vc_transducer"]);
}

function loadPage() {
    console.log("Loading")
    var svg = d3.select("#graph").append("svg")
        .attr("width", width)
        .attr("height", height);

    var graph = new BaseGraph(svg, [], []);
    graph.draggingElement = null;

    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        graph.draggingElement = null;
    });

    var docEl = document.documentElement,
        bodyEl = document.getElementsByTagName("body")[0];

    var width = window.innerWidth || docEl.clientWidth || bodyEl.clientWidth,
        height = window.innerHeight || docEl.clientHeight || bodyEl.clientHeight;

    populateMenu(graph);
    graph.setIdCt(2);
    graph.updateGraph();
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