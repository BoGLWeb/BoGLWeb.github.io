"use strict";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { backendManager } from "./backendManager";
import { SystemDiagram } from "./types/graphs/SystemDiagram";
import { SubmenuID } from "./types/display/SubmenuID";
import getBackendManager = backendManager.getBackendManager;

var topMenuButtons;
let hasAssignedInputClick = false;
var menuClickingDone = false;

// lists the DOM ids of menus by their id number
var menuIdMap = {
    0: "fileMenu",
    1: "editMenu",
    2: "helpMenu",
    3: "exampleMenu",
    4: "mechTransMenu",
    5: "mechRotMenu",
    6: "elecMenu"
}

// lists the index numbers and DOM id numbers of submenu children by their parent's id number
var submenuMap = {
    2: [new SubmenuID(3, 3)],
    3: [new SubmenuID(1, 4), new SubmenuID(2, 5), new SubmenuID(3, 6)]
}

// loads a system diagram from a string
async function loadSystemDiagram(text: string) {
    let systemDiagramText = await DotNet.invokeMethodAsync("BoGLWeb", "openSystemDiagram", text);
    if (systemDiagramText != null) {
        getBackendManager().loadSystemDiagram(systemDiagramText);
    }
}

// waits until a menu is clicked
function waitForMenuClickingDone(func) {
    if (menuClickingDone) {
        func();
    } else {
        setTimeout(() => waitForMenuClickingDone(func), 20);
    }
}

// finds the first parent menu of a submenu
function findParentMenu(menuId: number) {
    for (let key of Object.keys(submenuMap)) {
        if ((submenuMap[key] as SubmenuID[]).some(sub => sub.id == menuId)) {
            return parseInt(key);
        }
    }
    return null;
}

// finds all parent menus of a submenu recursively
function findAllParentMenus(menuId: number) {
    let parent = findParentMenu(menuId);
    if (parent != null) {
        return [parent, ...findAllParentMenus(parent)];
    }
    return [];
}

// add click action to each top menu and submenu
function menuClickAction(menuTitle: Node, k: number) {
    menuTitle.addEventListener("click", (e) => {
        e.stopPropagation();
        let parents = findAllParentMenus(k);

        waitForMenuClickingDone(() => {
            // assign onchange event for file upload input in File menu once it exists
            if (k == 0 && !hasAssignedInputClick) {
                hasAssignedInputClick = true;
                let input = document.getElementById("fileUpload") as HTMLInputElement;
                input.onchange = async () => {
                    let files = Array.from(input.files);
                    if (files[0].text) {
                        let text = await files[0].text();
                        loadSystemDiagram(text);
                    } else {
                        const reader = new FileReader();
                        reader.onload = event => {
                            loadSystemDiagram(event.target.result as string);
                        };
                        reader.readAsText(files[0]);
                    }
                }
            }

            let el = document.getElementById(menuIdMap[k]);
            if (el) {
                el = el.parentElement?.parentElement;
                // toggle whether clicked menu is hidden
                el.setAttribute("hidden-menu", (el.getAttribute("hidden-menu") == "false").toString());
                // if menu is a submenu, place it appropriately by its parent
                if (![0, 1, 2].includes(k)) {
                    let menuTitleBounds = (menuTitle as Element).getBoundingClientRect();
                    el.style.top = menuTitleBounds.top + "px";
                    el.style.left = (menuTitleBounds.left + menuTitleBounds.width + 4) + "px";
                }

                // if menu is a submenu and is visible, add click actions to submenu buttons now that they are visible
                if (el.getAttribute("hidden-menu") == "false" && submenuMap.hasOwnProperty(k)) {
                    for (let sub of submenuMap[k] as SubmenuID[]) {
                        if (!sub.hasClickAction) {
                            let el = document.getElementById(menuIdMap[k]).parentElement.children[sub.index];
                            menuClickAction(el, sub.id);
                            sub.hasClickAction = true;
                        }
                    }
                }

                // hide all menus that are not the current menu or one of its parents
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

// populates the element menu with buttons representing each element
function populateElementMenu() {
    // iterates through element categories and types to dynamically generate the element menu, making it easy to add new elements
    ElementNamespace.categories.map((c, i) => {
        ElementNamespace.elementTypes.filter(e => e.category === i).forEach(e => {
            const group = document.createElement('div');

            group.classList.add("groupDiv");
            // if the element is dragged, store it in system diagram
            group.addEventListener("mousedown", function () {
                document.body.style.cursor = "grabbing";
                window.systemDiagram.draggingElement = e.id;
            });

            document.getElementById(c.folderName).appendChild(group);

            // make HTML object for element with box and element image
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

// completes all actions that need to happen before the user interacts with the page
async function loadPage() {
    delete window.jQuery;
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

    // looks as URL and checks whether a system diagram needs to be loaded from URL
    const urlParams = new URLSearchParams(window.location.search);
    const myParam = urlParams.get('q');
    if (myParam !== null) {
        // if URL has system diagram, load that system diagram
        let sysDiagramString = await DotNet.invokeMethodAsync("BoGLWeb", "uncompressUrl", myParam);
        getBackendManager().loadSystemDiagram(sysDiagramString);
    } else {
        // if URL has no system diagram, load an empty systemm diagram
        window.systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram([], []));
        window.systemDiagram.updateGraph();
        backendManager.getBackendManager().zoomCenterGraph("1");
        window.systemDiagram.changeScale(window.systemDiagram.svgX, window.systemDiagram.svgY, 1);
    }

    // on mouseup, clear element dragged from menu
    document.addEventListener("mouseup", function () {
        document.body.style.cursor = "auto";
        window.systemDiagram.draggingElement = null;
    });

    // populate the element menu
    populateElementMenu();

    window.unsimpBGSVG = d3.select("#unsimpBG").append("svg");
    window.unsimpBGSVG.classed("graphSVG", true);
    window.simpBGSVG = d3.select("#simpBG").append("svg");
    window.simpBGSVG.classed("graphSVG", true);
    window.causalBGSVG = d3.select("#causalBG").append("svg");
    window.causalBGSVG.classed("graphSVG", true);

    // call SVG keyup and keydown functions on keyup or keydown
    d3.select(window).on("keydown", function () {
        let graph = backendManager.getBackendManager().getGraphByIndex(window.tabNum);
        graph.svgKeyDown.call(graph);
    })
    .on("keyup", function () {
        let graph = backendManager.getBackendManager().getGraphByIndex(window.tabNum);
        graph.svgKeyUp.call(graph);
    });

    topMenuButtons = document.getElementsByClassName('topMenu');

    // open all top menus and click their submenus recursively to load them in the DOM, then add click actions to these menus
    for (let i = 0; i < 3; i++) {
        topMenuButtons.item(i).click();
        clickSubmenus(i);
        menuClickAction(topMenuButtons.item(i), i);
    }

    // when anywhere outside a top menu is clicked, close all top menus and submenus
    document.getElementsByClassName("page").item(0).addEventListener("click", () => {
        for (let i = 0; i < Object.keys(menuIdMap).length; i++) {
            let el = document.getElementById(menuIdMap[i]);
            if (el) {
                el.parentElement.parentElement.setAttribute("hidden-menu", "true");
            }
        }
    });

    // add an alert before leaving the page prompting the user to save unfinished work
    window.onbeforeunload = function (e) {
        return "Are you sure you want to exit BoGL Web? Your current progress will be lost unless you download it or make a URL from it.";
    };

    // handle left menu resize for all tabs
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
    };

    const mouseUpHandler = function () {
        document.removeEventListener('mousemove', mouseMoveHandler);
        document.removeEventListener('mouseup', mouseUpHandler);
    };

    const resizers = ele.querySelectorAll('.resizer');

    [].forEach.call(resizers, function (resizer) {
        resizer.addEventListener('mousedown', mouseDownHandler);
    });

    // download all example .bogl files to allow them to be cached for use in PWA mode
    let examples = ["basic-two-mass-system", "basic-two-mass-system1", "basic-two-mass-system2", "masses_on_a_spring", "moving_masses", "spring_&_damper", "rack_pinion", "motor-gear-pair", "lrc_circuit"];

    for (let i in examples) {
        fetch("https://boglweb.github.io/rules-and-examples/examples/" + examples[i] + ".bogl");
    }

    // add mouseenter event to all checkboxes that focuses the checkbox before clicking on it, fixes a bug
    document.querySelectorAll('.ant-checkbox-wrapper').forEach(e => e.addEventListener("mouseenter", () => (e.children[0].children[0] as HTMLElement).focus()));

    // closes the zoom menu by default if the screen height is too small
    if (innerHeight < 635) {
        (document.querySelector("#zoomMenu > div > div > div") as HTMLElement).click();
    }
}

// poll the DOM until the graph menu and top menu have loaded, then call the load page function
function pollDOM() {
    const el = document.getElementById('graphMenu') && document.getElementsByClassName('topMenu').length > 0;

    if (el) {
        loadPage();
    } else {
        setTimeout(pollDOM, 20);
    }
}

// starts the DOM polling
pollDOM();


//sub-optional resolution sizes - open to changes
const min_width = 800;
const min_height = 800;

//popup not shown as a defualt 
//let popupShown = false;

function checkWindowSize() {
    if ((window.innerWidth < min_width ||window.innerHeight < min_height)) {
     
        alert('Warning: Window screen size is sub-optimal');
    } 
    console.log("Window Size Script loaded")
}
window.addEventListener('resize', checkWindowSize);
checkWindowSize();
