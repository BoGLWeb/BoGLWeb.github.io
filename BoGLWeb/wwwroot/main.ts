"use strict";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { backendManager } from "./backendManager";
import { SystemDiagram } from "./types/graphs/SystemDiagram";

export function populateMenu() {
    ElementNamespace.categories.map((c, i) => {
        ElementNamespace.elementTypes.filter(e => e.category === i).forEach(e => {
            const group = document.createElement('div');

            group.classList.add("groupDiv");
            group.addEventListener("mousedown", function () {
                document.body.style.cursor = "grabbing";
                (<any>window).systemDiagram.draggingElement = e.id;
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
    (<any>window).systemDiagramSVG = d3.select("#systemDiagram").append("svg");
    (<any>window).systemDiagramSVG.classed("graphSVG", true);
    (<any>window).systemDiagram = new SystemDiagramDisplay((<any>window).systemDiagramSVG, new SystemDiagram([], []));

    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        (<any>window).systemDiagram.draggingElement = null;
    });

    populateMenu();

    (<any>window).unsimpBGSVG = d3.select("#unsimpBG").append("svg");
    (<any>window).unsimpBGSVG.classed("graphSVG", true);
    (<any>window).simpBGSVG = d3.select("#simpBG").append("svg");
    (<any>window).simpBGSVG.classed("graphSVG", true);
    (<any>window).causalBGSVG = d3.select("#causalBG").append("svg");
    (<any>window).causalBGSVG.classed("graphSVG", true);
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
