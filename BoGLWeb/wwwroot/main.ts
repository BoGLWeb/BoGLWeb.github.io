"use strict";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { backendManager } from "./backendManager";
import { SystemDiagram } from "./types/graphs/SystemDiagram";
import getBackendManager = backendManager.getBackendManager;
import { SubmenuID } from "./types/display/BondGraphBond";

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

    document.querySelectorAll('input[type="checkbox"]').forEach(e => e.addEventListener("click", () => (e as HTMLElement).focus()));
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

// holds the index numbers of submenu children
var submenuMap = {
    2: [new SubmenuID(3, 3)],
    3: [new SubmenuID(1, 4), new SubmenuID(2, 5), new SubmenuID(3, 6)]
}

function findParentMenu(menuId: number) {
    for (let key of Object.keys(submenuMap)) {
        if ((submenuMap[key] as SubmenuID[]).some(sub => sub.id == menuId)) {
            return parseInt(key);
        }
    }
    return null;
}

function findAllParentMenus(menuId: number) {
    let parent = findParentMenu(menuId);
    if (parent != null) {
        return [parent, ...findAllParentMenus(parent)];
    }
    return [];
}

function menuClickAction(menuTitle: Node, k: number) {
    let submenuInitializing = ![0, 1, 2].includes(k);
    menuTitle.addEventListener("click", (e) => {
        if (!submenuInitializing) {
            e.stopPropagation();
        }
        submenuInitializing = false;
        let parents = findAllParentMenus(k);
        let el = document.getElementById(menuIdMap[k]);
        if (el) {
            el = el.parentElement?.parentElement;
            el.setAttribute("hidden-menu", (el.getAttribute("hidden-menu") == "false").toString());

            if (el.getAttribute("hidden-menu") == "false" && submenuMap.hasOwnProperty(k)) {
                for (let sub of submenuMap[k] as SubmenuID[]) {
                    if (!sub.hasClickAction) {
                        let el = document.getElementById(menuIdMap[k]).parentElement.children[sub.index];
                        menuClickAction(el, sub.id);
                        sub.hasClickAction = true;
                    }
                }
            }

            for (let i = 0; i < Object.keys(menuIdMap).length; i++) {
                el = document.getElementById(menuIdMap[i]);
                if (i == k || parents.includes(i) || !el) continue;
                el = el.parentElement.parentElement;
                el.setAttribute("hidden-menu", "true");
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

// clicks through all menus to get them in the DOM
function clickSubmenus(menuId: number) {
    const cond = document.getElementById(menuIdMap[menuId])?.parentElement?.parentElement;

    if (cond && submenuMap.hasOwnProperty(menuId)) {
        for (let submenu of submenuMap[menuId] as SubmenuID[]) {
            let submenuEl = document.getElementById(menuIdMap[menuId]).parentElement.children[submenu.index];
            (submenuEl as HTMLElement).click();
            clickSubmenus(submenu.id);
        }
    } else {
        setTimeout(() => clickSubmenus(menuId), 20);
    }
}


pollDOM();
