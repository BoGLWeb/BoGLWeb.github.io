import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { GraphBond } from "./types/bonds/GraphBond";
import { BondGraphDisplay } from "./types/display/BondGraphDisplay";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { BondGraphElement } from "./types/elements/BondGraphElement";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { SystemDiagramElement } from "./types/elements/SystemDiagramElement";
import { BondGraph } from "./types/graphs/BondGraph";
import { SystemDiagram } from "./types/graphs/SystemDiagram";
import { SVGSelection } from "./type_libraries/d3-selection";

export namespace backendManager {
    export class BackendManager {

        public parseAndDisplayBondGraph(id: number, jsonString: string, svg: SVGSelection) {
            let bg = JSON.parse(jsonString);

            let minX = Infinity;
            let minY = Infinity;
            let maxX = -Infinity;
            let maxY = -Infinity;
            let elements = JSON.parse(bg.elements).map((e, i) => {
                if (e.x < minX) minX = e.x;
                if (e.y < minY) minY = e.y;
                if (e.x > maxX) maxX = e.x;
                if (e.y > maxY) maxY = e.y;
                return new BondGraphElement(i, e.label, e.x, e.y);
            }) as BondGraphElement[];

            elements.forEach(e => {
                e.x += (maxX - minX) / 2 - maxX;
                e.y += (maxY - minY) / 2 - maxY;
            });

            let bonds = JSON.parse(bg.bonds).map(b => {
                return new BondGraphBond(elements[b.sourceID], elements[b.targetID], b.causalStroke, b.causalStrokeDirection, b.velocity);
            }) as BondGraphBond[];
            let bondGraph = new BondGraphDisplay(id, svg, new BondGraph(elements, bonds));

            if (id == 0) {
                window.unsimpBG = bondGraph;
            } else if (id == 1) {
                window.simpBG = bondGraph;
            } else {
                window.causalBG = bondGraph;
            }
            bondGraph.updateGraph();
            this.zoomCenterGraph(JSON.stringify(id + 2));
        }

        public displayUnsimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(0, jsonString, window.unsimpBGSVG);
        }

        public displaySimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(1, jsonString, window.simpBGSVG);
        }

        public displayCausalBondGraphOption(jsonStrings: Array<string>, index: number) {
            this.parseAndDisplayBondGraph(2, jsonStrings[index], window.causalBGSVG);
        }

        public loadSystemDiagram(jsonString: string) {
            let edges = [];
            let minX = Infinity;
            let minY = Infinity;
            let maxX = -Infinity;
            let maxY = -Infinity;
            
            let parsedJson = JSON.parse(jsonString);

            let elements = new Map<number, SystemDiagramElement>();
            let i = 0;
            for (let e of parsedJson.elements) {
                if (e.x < minX) minX = e.x;
                if (e.y < minY) minY = e.y;
                if (e.x > maxX) maxX = e.x;
                if (e.y > maxY) maxY = e.y;
                
                if(e.id != null){
                    elements.set(e.id, new SystemDiagramElement(e.id, e.type, e.x, e.y, e.velocity, e.modifiers));
                }else{
                    elements.set(i++, new SystemDiagramElement(i, e.type, e.x, e.y, e.velocity, e.modifiers));
                }
            }

            elements.forEach(e => {
                e.x += (maxX - minX) / 2 - maxX;
                e.y += (maxY - minY) / 2 - maxY;
            });

            for (let edge of parsedJson.edges) {
                let bond = new GraphBond(elements.get(edge.source), elements.get(edge.target));
                bond.velocity = edge.velocity ?? 0;
                edges.push(bond);
            }

            let systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram(Array.from(elements.values()), edges));
            systemDiagram.draggingElement = null;
            window.systemDiagram = systemDiagram;
            systemDiagram.updateGraph();
            this.zoomCenterGraph("1");
        }

        public zoomCenterGraph(index: string) {
            let graph = this.getGraphByIndex(index);
            let prevDisplay = graph.svgG.node().parentElement.parentElement.parentElement.style.display;
            graph.svgG.node().parentElement.parentElement.parentElement.style.display = "block";
            let svgDim = (graph.svgG.node() as SVGSVGElement).getBBox();
            let windowDim = document.getElementById("systemDiagram").getBoundingClientRect();
            let scale = 1;
            if (svgDim.width / svgDim.height > windowDim.width / windowDim.height) {
                scale = (0.8 * windowDim.width) / svgDim.width;
            } else {
                scale = (0.8 * windowDim.height) / svgDim.height;
            }
            scale = Math.min(Math.max(scale, 0.25), 1.75);
            let xTrans = -svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2);
            let yTrans = -svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2);
            console.log(svgDim, xTrans, yTrans);
            graph.changeScale(xTrans, yTrans, scale);
            graph.svgG.node().parentElement.parentElement.parentElement.style.display = prevDisplay;
        }

        public async openFile() {
            let fileHandle;
            [fileHandle] = await window.showOpenFilePicker();
            const file = await fileHandle.getFile();
            const contents = await file.text();
            return contents;
        }

        public async saveAsFile(fileName: string, contentStreamReference: any) {
            const arrayBuffer = await contentStreamReference.arrayBuffer();
            const blob = new Blob([arrayBuffer]);

            const pickerOptions = {
                suggestedName: `systemDiagram.bogl`,
                types: [
                    {
                        description: 'A BoGL File',
                        accept: {
                            'text/plain': ['.bogl'],
                        },
                    },
                ],
            };

            const fileHandle = await window.showSaveFilePicker(pickerOptions);
            window.filePath = fileHandle;
            const writableFileStream = await fileHandle.createWritable();
            await writableFileStream.write(blob);
            await writableFileStream.close();
        }

        public async saveFile(fileName: string, contentStreamReference: any) {
            const arrayBuffer = await contentStreamReference.arrayBuffer();
            const blob = new Blob([arrayBuffer]);

            const pickerOptions = {
                suggestedName: `systemDiagram.bogl`,
                types: [
                    {
                        description: 'A BoGL File',
                        accept: {
                            'text/plain': ['.bogl'],
                        },
                    },
                ],
            };

            if (window.filePath == null) {
                window.filePath = await window.showSaveFilePicker(pickerOptions);
            }

            const writableFileStream = await window.filePath.createWritable();
            await writableFileStream.write(blob);
            await writableFileStream.close();
        }

        public cut() {
            this.getSystemDiagramDisplay().copySelection();
            this.getSystemDiagramDisplay().deleteSelection();
        }

        public copy() {
            this.getSystemDiagramDisplay().copySelection();
        }

        public paste() {
            this.getSystemDiagramDisplay().pasteSelection();
        }

        public delete(needsConfirmation = true) {
            this.getSystemDiagramDisplay().deleteSelection(needsConfirmation);
        }
        
        public areMultipleElementsSelected() {
            return this.getSystemDiagramDisplay().selectedElements.length > 1 || this.getSystemDiagramDisplay().selectedBonds.length > 1;
        }

        public getSystemDiagramDisplay() {
            return this.getGraphByIndex("1") as SystemDiagramDisplay;
        }

        public getSystemDiagram() {
            return JSON.stringify({
                elements: window.systemDiagram.elements,
                bonds: window.systemDiagram.bonds
            });
        }

        public setModifier(i: number, value: boolean) {
            if (value) { // adding modifier
                for (const el of window.systemDiagram.selectedElements) {
                    if (ElementNamespace.elementTypes[el.type].allowedModifiers.includes(i) && !el.modifiers.includes(i)) {
                        el.modifiers.push(i);
                    }
                }
            } else { // removing modifiers
                for (const el of window.systemDiagram.selectedElements) {
                    if (el.modifiers.includes(i)) {
                        el.modifiers.splice(el.modifiers.indexOf(i), 1);
                    }
                }
            }
            window.systemDiagram.updateModifierMenu();
        }

        public getGraphByIndex(i: string) {
            if (i == "1") {
                return window.systemDiagram;
            } else if (i == "2") {
                return window.unsimpBG;
            } else if (i == "3") {
                return window.simpBG;
            } else {
                return window.causalBG;
            }
        }

        public setZoom(i: number) {
            let graph = this.getGraphByIndex(window.tabNum);
            let windowDim = graph.svg.node().parentElement.getBoundingClientRect();

            let xOffset = (graph.prevScale * 100 - i) * (graph.svgX - graph.initXPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);
            let yOffset = (graph.prevScale * 100 - i) * (graph.svgY - graph.initYPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);

            console.log(xOffset, windowDim.width / 2 - (windowDim.width / 2 - graph.svgX), (windowDim.width / 2 - graph.svgX), graph.svgX);
            if (graph.prevScale * 100 - i != 0) {
                graph.changeScale(windowDim.width / 2 - (windowDim.width / 2 - graph.svgX) - xOffset, windowDim.height / 2 - (windowDim.height / 2 - graph.svgY) - yOffset, i / 100);
            }
        }

        public setTab(key: string) {
            window.tabNum = key;
            DotNet.invokeMethodAsync("BoGLWeb", "SetScale", this.getGraphByIndex(key).prevScale);
        }

        public setVelocity(velocity: number) {
            for (const e of window.systemDiagram.getSelection()) {
                if (e instanceof GraphBond || ElementNamespace.elementTypes[e.type].velocityAllowed) {
                    e.velocity = velocity;
                }
            }
            window.systemDiagram.updateGraph();
        }

        public generateURL() {
            return JSON.stringify({
                elements: window.systemDiagram.elements,
                bonds: window.systemDiagram.bonds
            }, function (key, val) {
                return val.toFixed ? Number(val.toFixed(3)) : val;
            });
        }

        public textToClipboard(text: string) {
            navigator.clipboard.writeText(text);
        }

        private hideMenu(menuId: string) {
            let el = document.getElementById(menuId);
            if (document.getElementById(menuId)) {
                el = el.parentElement.parentElement;
                if (el.getAttribute("hidden-menu") != "true") {
                    console.log("Hiding ", j);
                    el.setAttribute("hidden-menu", "true");
                }
            }
        }

        public closeMenu(menuName: string) {
            switch (menuName) {
                case "File":
                    this.hideMenu("fileMenu");
                    break;
                case "Edit":
                    this.hideMenu("editMenu");
                    break;
                case "Help":
                    this.hideMenu("helpMenu");
                    this.hideMenu("exampleMenu");
                    this.hideMenu("mechTransMenu");
                    this.hideMenu("mechRotMenu");
                    this.hideMenu("elecMenu");
            }
        }

        public runTutorial() {
            this.closeMenu("Help");
            window.introJs().setOptions({
                showStepNumbers: false,
                hideNext: true,
                steps: [{
                    intro: '<p><b>Welcome To BoGL Web</b></p><p>This application is used to construct system diagrams and generate bond graphs from those diagrams.</p>'
                }, {
                    element: document.querySelector('.ant-tabs-content'),
                    intro: '<p><b>The Canvas</b></p><p>This is the canvas, where you can construct, move, and rearrange your system diagrams.</p>'
                }, {
                    element: document.querySelector('#graphMenu'),
                    intro: '<p><b>The Element Palette</b></p><p>This is the element palette. After expanding the menus, you can select and drag elements into the canvas to construct system diagrams.</p>'
                }, {
                    element: document.querySelector('.ant-tabs-content'),
                    intro: '<p><b>Constructing a System Diagram</b></p><p>You can use your cursor to select and drag elements into the canvas. To create an edge, hover your cursor over the border of the elements, where you will see a green circle. If you click and draw you will see a black line symbolizing the edge that you can drop in another element to complete an edge. If you see a red X when you try to make an edge, it means the edge you are trying to make is invalid (the two elements do not make sense to be connected).</p>'
                },
                {
                    element: document.querySelector('#modifierMenu'),
                    intro: '<p><b>The Modifier Menu</b></p><p>In this menu you can add modifiers to the selected element. Some modifiers require multiple elements to be selected. You can do this by holding down Ctrl and clicking elements you want to select or by dragging the canvas with the left mouse button to create a selection region.</p>'
                }, {
                    element: document.querySelector('#zoomMenu'),
                    intro: '<p><b>The Zoom Menu</b></p><p>This menu allows you to zoom in and out of the canvas. You can also use the scroll wheel to perform this action. Dragging the canvas with the right mouse click will pan the graph.</p>'
                }, {
                    element: document.querySelector('#generateButton'),
                    intro: '<p><b>The Generate Button</b></p><p>The generate button allows you to turn your system diagram into a bond graph. While the bond graph is generating you will see a loading bar which signifies that BoGL Web is processing your System Diagram. This can take a few seconds.</p>'
                }, {
                    element: document.querySelector('.ant-tabs-nav-list'),
                    intro: '<p><b>The Tabs</b></p><p>These tabs store different stages of the bond graph generation. You can look at the unsimplified bond graph, the simplified bond graph, or the causal bond graph.</p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(2)'),
                    intro: '<p><b>The File Menu</b></p>' +
                        '<p>This is the file menu. It allows you to:' +
                        '<ul>' +
                        '<li>Create a new system diagram</li>' +
                        '<li>Open a previously saved .bogl file from your computer</li>' +
                        '<li>Save a .bogl file to your computer</li>' +
                        '<li>Export an image of your system diagram or bond graph</li>' +
                        '<li>Generate a URL that links to your system diagram</li>' +
                        '</ul></p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(3)'),
                    intro: '<p><b>The Edit Menu</b></p>' +
                        '<p>This is the edit menu. It allows you to:' +
                        '<ul>' +
                        '<li>Copy, cut, and paste elements of the system diagram</li>' +
                        '<li>Undo and redo changes</li>' +
                        '<li>Delete elements from the System Diagram</li>' +
                        '</ul></p>'
                }, {
                    element: document.querySelector('#iconButtons'),
                    intro: '<p><b>The Icons</b></p><p>You can perform similar features to the edit menu here. By pressing the icons you can save a system diagram, cut, copy, paste, undo, redo, and delete an element or edge from the system diagram.</p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(4)'),
                    intro: '<p><b>The Help Menu</b></p>' +
                        '<p>This is the help menu. It allows you to:' +
                        '<ul>' +
                        '<li>Confirm deleting many items at once. Selecting this option will allow you to select multiple items and then delete them all at once.</li>' +
                        '<li>Start this tutorial again</li>' +
                        '<li>Load example system diagrams</li>' +
                        '<li>Report bugs that you find</li>' +
                        '<li>Learn about who created BoGL Web System</li>' +
                        '</ul></p>'
                }]
            }).start();
        }
        
        instance: any;
        
        public initInstance(instance: any){
            this.instance = instance;
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}