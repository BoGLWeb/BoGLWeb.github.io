"use strict";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { backendManager } from "./backendManager";
import { SystemDiagram } from "./types/graphs/SystemDiagram";
import getBackendManager = backendManager.getBackendManager;

export function populateMenu() {
    ElementNamespace.categories.map((c, i) => {
        ElementNamespace.elementTypes.filter(e => e.category === i).forEach(e => {
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

var topMenuButtons;

async function loadPage() {
    window.tabNum = "1"; 
    let sliderHolder = document.querySelector("#zoomMenu .ant-slider-handle");
    let sliderImg: any = document.createElement("img"); 
    sliderImg.src = "images/sliderIcon.svg";
    sliderImg.id = "sliderImg";
    sliderImg.draggable = false;
    sliderHolder.appendChild(sliderImg);

    window.backendManager = backendManager;
    window.systemDiagramSVG = d3.select("#systemDiagram").append("svg");
    window.systemDiagramSVG.classed("graphSVG", true);

    const urlParams = new URLSearchParams(window.location.search);
    const myParam = urlParams.get('q');
    if (myParam !== null){
        let sysDiagramString  = await DotNet.invokeMethodAsync("BoGLWeb", "uncompressUrl", myParam);
        getBackendManager().loadSystemDiagram(sysDiagramString);
    } else {
        window.systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram([], []));
        window.systemDiagram.updateGraph();
        backendManager.getBackendManager().zoomCenterGraph("1");
        window.systemDiagram.changeScale(window.systemDiagram.svgX, window.systemDiagram.svgY, 1);
    }

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
        let graph = backendManager.getBackendManager().getGraphByIndex(window.tabNum);
        graph.svgKeyDown.call(graph);
    })
    .on("keyup", function () {
        let graph = backendManager.getBackendManager().getGraphByIndex(window.tabNum);
        graph.svgKeyUp.call(graph);
    });

    topMenuButtons = document.getElementsByClassName('topMenu');

    for (let i = 0; i < topMenuButtons.length; i++) {
        let el = topMenuButtons.item(i);
        (el as HTMLElement).click();
    }

    for (let i = 0; i < topMenuButtons.length; i++) {
        menuClickAction(topMenuButtons.item(i), i);
    }

    document.getElementsByClassName("page").item(0).addEventListener("click", (e) => {
        for (let i = 0; i < 3; i++) {
            let el = document.getElementById(menuIdMap[i]);
            if (el) {
                el = el.parentElement.parentElement;
                if (el.getAttribute("hidden-menu") != "true") {
                    el.setAttribute("hidden-menu", "true");
                }
            }
        }
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
}

function menuClickAction(newEl: Node, k: number) {
    let firstTime = true;
    newEl.addEventListener("click", (e) => {
        e.stopPropagation();
        for (let i = 0; i < 3; i++) {
            let el = document.getElementById(menuIdMap[i]).parentElement.parentElement;
            if (firstTime && k == 2) {
                let examplesMenu = document.querySelector("#helpMenu").parentElement.children[3];
                menuClickAction(examplesMenu, 3);
                firstTime = false;
            }
            if (el.getAttribute("hidden-menu") != "true" && !(i == k && el.getAttribute("hidden-menu") == null)) {
                console.log("Hiding ", i);
                el.setAttribute("hidden-menu", "true");
                if (i == 2) {
                    for (let j = 3; j < 7; j++) {
                        let el = document.getElementById(menuIdMap[j]);
                        if (el) {
                            el = el.parentElement.parentElement;
                            el.setAttribute("hidden-menu", "true");
                        }
                    }
                }
            } else if (i == k) {
                console.log("Showing ", i);
                el.setAttribute("hidden-menu", "false");
            }
        }
    });
}

function pollDOM() {
    const el = document.getElementById('graphMenu') && document.getElementsByClassName('topMenu').length > 0;

    if (el) {
        loadPage();
    } else {
        setTimeout(pollDOM, 20);
    }
}

function assignMenuClickActions() {
    const cond = document.getElementById(menuIdMap[0])?.parentElement?.parentElement && document.getElementById(menuIdMap[1])?.parentElement?.parentElement
        && document.getElementById(menuIdMap[2])?.parentElement?.parentElement;

    if (cond) {
        for (let i = 0; i < topMenuButtons.length; i++) {
            let el = topMenuButtons.item(i);
            var oldEl = el;
            var newEl = oldEl.cloneNode(true);
            oldEl.parentNode.replaceChild(newEl, oldEl);
            menuClickAction(newEl, i);
            let menu = document.getElementById(menuIdMap[i]).parentElement.parentElement;
            menu.setAttribute("hidden-menu", "true");
        }
    } else {
        setTimeout(assignMenuClickActions, 20);
    }
}


pollDOM();
