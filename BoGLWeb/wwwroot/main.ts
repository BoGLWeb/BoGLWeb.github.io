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
            image.title = e.name;
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

    window.onbeforeunload = function (e) {
        return "Are you sure you want to exit BoGL Web? Your current progress will be lost unless you download it or make a URL from it.";
    };

    document.querySelectorAll('input[type="checkbox"]').forEach(e => e.addEventListener("click", () => (e as HTMLElement).focus()));


    const ele = document.getElementById('graphMenu');
    let x = 0;
    let w = 0;

    const mouseDownHandler = function (e) {
        x = e.clientX;

        const styles = window.getComputedStyle(ele);
        w = parseInt(styles.width, 10);

        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler);
    };

    const mouseMoveHandler = function (e) {
        const dx = e.clientX - x;
        ele.style.flex = "0 0 " + Math.max(Math.min(w + dx, 700), 225) + "px";
        console.log("0 0 " + Math.max(Math.min(w + dx, 700), 225) + "px");
    };

    const mouseUpHandler = function () {
        document.removeEventListener('mousemove', mouseMoveHandler);
        document.removeEventListener('mouseup', mouseUpHandler);
    };

    const resizers = ele.querySelectorAll('.resizer');

    [].forEach.call(resizers, function (resizer) {
        resizer.addEventListener('mousedown', mouseDownHandler);
    });

    // @ts-ignore
    let html = katex.renderToString("\\mathrm{ d }_{ \\mathrm{ ij } } \\mathrm{ \\mathrm{= } } \\sqrt{\\sum_{ \\mathrm{ k = 1 } }^ { \\mathrm{ p }} \\left(\\mathrm{ x } ^\\mathrm{ k }\\left(\\mathrm{ i }\\right) \\mathrm{-}\\ \\mathrm{ x } ^\\mathrm{ k } \\left(\\mathrm{ j }\\right) \\right) ^\\mathrm{ 2 }}", {
        throwOnError: false
    });
    const parser = new DOMParser();
    document.getElementById("latexTest").appendChild(parser.parseFromString(html, "application/xml").children[0].children[0]);
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

var menuClickingDone = false;

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
    menuTitle.addEventListener("click", (e) => {
        e.stopPropagation();
        let parents = findAllParentMenus(k);
        waitForMenuClickingDone(() => {
            let el = document.getElementById(menuIdMap[k]);
            if (el) {
                el = el.parentElement?.parentElement;
                el.setAttribute("hidden-menu", (el.getAttribute("hidden-menu") == "false").toString());
                if (![0, 1, 2].includes(k)) {
                    let menuTitleBounds = (menuTitle as Element).getBoundingClientRect();
                    el.style.top = menuTitleBounds.top + "px";
                    el.style.left = (menuTitleBounds.left + menuTitleBounds.width + 4) + "px";
                }

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

    if (cond) {
        for (let submenu of submenuMap[menuId] as SubmenuID[]) {
            let submenuEl = document.getElementById(menuIdMap[menuId]).parentElement.children[submenu.index];
            (submenuEl as HTMLElement).click();
            clickSubmenus(submenu.id);
        }
    } else if (submenuMap.hasOwnProperty(menuId)) {
        setTimeout(() => clickSubmenus(menuId), 20);
    }
    if (menuId == 6) {
        menuClickingDone = true;
    }
}

function waitForMenuClickingDone(func) {
    if (menuClickingDone) {
        func();
    } else {
        setTimeout(() => waitForMenuClickingDone(func), 20);
    }
}


pollDOM();
