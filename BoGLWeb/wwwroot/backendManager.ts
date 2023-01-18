import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { GraphBond } from "./types/bonds/GraphBond";
import { GraphBondID } from "./types/bonds/GraphBondID";
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
            let elements = JSON.parse(bg.elements).map((e, i) => {
                return new BondGraphElement(i, e.label, e.x, e.y);
            }) as BondGraphElement[];
            let bonds = JSON.parse(bg.bonds).map(b => {
                return new BondGraphBond(elements[b.sourceID], elements[b.targetID], b.causalStroke, b.causalStrokeDirection, b.velocity);
            }) as BondGraphBond[];
            let bondGraph = new BondGraphDisplay(id, svg, new BondGraph(elements, bonds));

            bondGraph.changeScale(0, 0, 1, false);
            if (id == 0) {
                window.unsimpBG = bondGraph;
            } else if (id == 1) {
                window.simpBG = bondGraph;
            } else {
                window.causalBG = bondGraph;
            }
            bondGraph.updateGraph();
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
            let parsedJson = JSON.parse(jsonString);
            let elements = []
            let i = 0;
            for (let el of parsedJson.elements) {
                let e = new SystemDiagramElement(i++, el.type, el.x, el.y, el.velocity, el.modifiers);
                elements.push(e);
            }
            let edges = [];
            for (let edge of parsedJson.edges) {
                let bond = new GraphBond(elements[edge.source], elements[edge.target]);
                bond.velocity = edge.velocity ?? 0;
                edges.push(bond);
            }

            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", elements.map(e => JSON.stringify(e)).concat(edges.map(e => JSON.stringify(e))),
                window.systemDiagram.listToIDObjects(window.systemDiagram.selectedElements), window.systemDiagram.listToIDObjects(window.systemDiagram.selectedBonds));

            var systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram(elements, edges));
            systemDiagram.draggingElement = null;

            window.systemDiagram = systemDiagram;
            systemDiagram.updateGraph();

            let svgDim = d3.select('#systemDiagram > svg > g').node().getBBox();
            let windowDim = document.getElementById("systemDiagram").getBoundingClientRect();
            let scale = 1;
            if (svgDim.width / svgDim.height > windowDim.width / windowDim.height) {
                scale = (0.8 * windowDim.width) / svgDim.width;
            } else {
                scale = (0.8 * windowDim.height) / svgDim.height;
            }
            let xTrans = -svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2);
            let yTrans = -svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2);
            systemDiagram.changeScale(xTrans, yTrans, scale, false);
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
        
        public areMultipleElementsSelected(){
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
            let prevModVals = window.systemDiagram.selectedElements.map(e => e.modifiers.includes(i));

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
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionModifier", window.systemDiagram.selectedElements.map(e => e.id), i, value, prevModVals);
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

            // converts SVG position to svg center of view window
            let svgDim = (graph.svgG.node() as SVGGraphicsElement).getBBox();
            let windowDim = graph.svg.node().parentElement.getBoundingClientRect();
            let scale = i / 100;
            let xTrans = -svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2);
            let yTrans = -svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2);

            let scaleDiff = 1 - (i / 100);

            if (!graph.zoomWithSlider) {
                graph.zoomWithSlider = true;
                graph.initXPos = (graph.initXPos - scaleDiff * xTrans) / (1 - scaleDiff);
                graph.initYPos = (graph.initYPos - scaleDiff * yTrans) / (1 - scaleDiff);
            }

            graph.changeScale(graph.initXPos + ((xTrans - graph.initXPos) * scaleDiff), graph.initYPos + ((yTrans - graph.initYPos) * scaleDiff), i / 100, true);
        }

        public setTab(key: string) {
            window.tabNum = key;
        }

        public setVelocity(velocity: number) {
            let prevVelVals = window.systemDiagram.getSelection().map(e => e.velocity);
            for (const e of window.systemDiagram.getSelection()) {
                if (e instanceof GraphBond || ElementNamespace.elementTypes[e.type].velocityAllowed) {
                    e.velocity = velocity;
                }
            }
            window.systemDiagram.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionVelocity", window.systemDiagram.listToIDObjects(window.systemDiagram.getSelection()), velocity, prevVelVals);
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

        public runTutorial() {
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

        public parseElementAndEdgeStrings(objects: string[]): [SystemDiagramElement[], GraphBond[]] {
            let elements: SystemDiagramElement[] = [];
            let bonds: GraphBond[] = [];
            for (const object of objects) {
                let json = JSON.parse(object);
                if (json.hasOwnProperty("id")) {
                    elements.push(new SystemDiagramElement(json.id, json.type, json.x, json.y, json.velocity, json.modifiers));
                } else {
                    bonds.push(new GraphBond(json.source, json.target, json.velocity));
                }
            }
            return [elements, bonds];
        }

        public parseElementAndEdgeIDStrings(objects: string[]): [number[], GraphBondID[], number[]] {
            let elements: number[] = [];
            let elementIndeces: number[] = [];
            let edges: GraphBondID[] = [];
            let i = 0;
            for (const object of objects) {
                let json = JSON.parse(object);
                if (json.hasOwnProperty("id")) {
                    elements.push(json.id);
                    elementIndeces.push(i);
                } else {
                    edges.push(new GraphBondID(json.sourceId, json.targetId, i));
                }
                i++;
            }
            return [elements, edges, elementIndeces];
        }

        checkBondIDs(elemIDs: GraphBondID[], b: GraphBond): GraphBondID {
            let sourceID = b.source.id;
            let targetID = b.target.id;
            return elemIDs.find(e => e.checkEquality(sourceID, targetID));
        }

        public urDoAddSelection(newObjects: string[], prevSelectedElements: string[], prevSelectedEdges: string[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(newObjects);
            if (isUndo) {
                let elIDs = elements.map(e => e.id);
                let elBonds = bonds.map(b => { return new GraphBondID(b.source.id, b.target.id); });
                sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                let [prevSelElIDs, prevSelEdgeIDs] = this.parseElementAndEdgeIDStrings(prevSelectedElements.concat(prevSelectedEdges));
                sysDiag.setSelection(sysDiag.elements.filter(e => prevSelElIDs.includes(e.id)), sysDiag.bonds.filter(b => this.checkBondIDs(prevSelEdgeIDs, b)));
            } else {
                sysDiag.elements = sysDiag.elements.concat(elements);
                sysDiag.bonds = sysDiag.bonds.concat(bonds);
                sysDiag.setSelection(elements, bonds);
            }
            sysDiag.updateModifierMenu();
            sysDiag.updateVelocityMenu();
            sysDiag.updateGraph();
        }

        public urDoDeleteSelection(deletedObjects: string[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(deletedObjects);
            if (isUndo) {
                sysDiag.elements = sysDiag.elements.concat(elements);
                sysDiag.bonds = sysDiag.bonds.concat(bonds);
                sysDiag.setSelection(elements, bonds);
            } else {
                let elIDs = elements.map(e => e.id);
                let elBonds = bonds.map(b => { return new GraphBondID(b.source.id, b.target.id); });
                sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                sysDiag.setSelection([], []);
            }
            sysDiag.updateModifierMenu();
            sysDiag.updateVelocityMenu();
            sysDiag.updateGraph();
        }

        public urDoChangeSelection(addToSelection: string[], removeFromSelection: string[], isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            let [addToSelectionEl, addToSelectionEdges] = this.parseElementAndEdgeIDStrings(addToSelection);
            let [removeFromSelectionEl, removeFromSelectionEdges] = this.parseElementAndEdgeIDStrings(removeFromSelection);
            let elAddSet = isUndo ? removeFromSelectionEl : addToSelectionEl;
            let elRemoveSet = isUndo ? addToSelectionEl : removeFromSelectionEl;
            let edgeAddSet = isUndo ? removeFromSelectionEdges : addToSelectionEdges;
            let edgeRemoveSet = isUndo ? addToSelectionEdges : removeFromSelectionEdges;
            // @ts-ignore // may want to fix this later, but shouldn't be an issue as long as tab index is correctly recorded 
            diagram.selectedElements = diagram.selectedElements.concat(diagram.elements.filter(e => elAddSet.includes(e.id)));
            diagram.selectedBonds = diagram.selectedBonds.concat(diagram.bonds.filter(b => this.checkBondIDs(edgeAddSet, b)));
            diagram.selectedElements = diagram.selectedElements.filter(e => !elRemoveSet.includes(e.id));
            diagram.selectedBonds = diagram.selectedBonds.filter(b => !this.checkBondIDs(edgeRemoveSet, b));
            if (diagram instanceof SystemDiagramDisplay) {
                diagram.updateModifierMenu();
                diagram.updateVelocityMenu();
            }
            diagram.updateGraph();
        }

        public urDoMoveSelection(elements: number[], xOffset: number, yOffset: number, isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            diagram.elements.filter(e => elements.includes(e.id)).forEach(e => {
                e.x = e.x + (isUndo ? -1 : 1) * xOffset;
                e.y = e.y + (isUndo ? -1 : 1) * yOffset;
            });
            diagram.updateGraph();
        }

        public urDoChangeSelectionVelocity(elementsAndEdges: string[], velID: number, prevVelVals: number[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elementIDs, bondIDs, numberIDs] = this.parseElementAndEdgeIDStrings(elementsAndEdges);
            sysDiag.elements.filter(e => elementIDs.includes(e.id)).forEach(e => e.velocity = isUndo ? prevVelVals[numberIDs[elementIDs.findIndex(i => i == e.id)]] : velID);
            sysDiag.bonds.filter(b => this.checkBondIDs(bondIDs, b)).forEach(b => b.velocity = isUndo ? prevVelVals[this.checkBondIDs(bondIDs, b).id] : velID);
            sysDiag.updateVelocityMenu();
            sysDiag.updateGraph();
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