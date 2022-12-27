import {BondGraphBond} from "./types/bonds/BondGraphBond";
import {GraphBond} from "./types/bonds/GraphBond";
import {BondGraphDisplay} from "./types/display/BondGraphDisplay";
import {SystemDiagramDisplay} from "./types/display/SystemDiagramDisplay";
import {BondGraphElement} from "./types/elements/BondGraphElement";
import {SystemDiagramElement} from "./types/elements/SystemDiagramElement";
import {BondGraph} from "./types/graphs/BondGraph";
import {SystemDiagram} from "./types/graphs/SystemDiagram";
import {SVGSelection} from "./type_libraries/d3-selection";

export namespace backendManager {
    export class BackendManager {
        public parseAndDisplayBondGraph(id: number, jsonString: string, svg: SVGSelection) {
            let bg = JSON.parse(jsonString);
            let elements = JSON.parse(bg.elements).map((e, i) => {
                e.id = i;
                return e;
            }) as BondGraphElement[];
            let bonds = JSON.parse(bg.bonds).map(b => {
                b.source = elements[b.sourceID];
                b.target = elements[b.targetID];
                return b;
            }) as BondGraphBond[];
            let bondGraph = new BondGraphDisplay(id, svg, new BondGraph(elements, bonds));

            bondGraph.changeScale(0, 0, 1, false);
            if (id == 0) {
                (<any>window).unsimpBG = bondGraph;
            } else if (id == 1) {
                (<any>window).simpBG = bondGraph;
            } else {
                (<any>window).causalBG = bondGraph;
            }
            bondGraph.updateGraph();
        }

        public displayUnsimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(0, jsonString, (<any>window).unsimpBGSVG);
        }

        public displaySimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(1, jsonString, (<any>window).simpBGSVG);
        }

        public displayCausalBondGraphOption(jsonStrings: Array<string>, index: number) {
            this.parseAndDisplayBondGraph(2, jsonStrings[index], (<any>window).causalBGSVG);
        }

        public loadSystemDiagram(jsonString: string) {
            let parsedJson = JSON.parse(jsonString);
            let elements = []
            let i = 0;
            for (let element of parsedJson.elements) {
                let e = element as unknown as SystemDiagramElement;
                e.id = i;
                elements.push(e);
                i++;
            }
            let edges = [];
            for (let edge of parsedJson.edges) {
                let bond = new GraphBond(elements[edge.source], elements[edge.target]);
                bond.velocity = edge.velocity ?? 0;
                edges.push(bond);
            }

            var systemDiagram = new SystemDiagramDisplay((<any> window).systemDiagramSVG, new SystemDiagram(elements, edges));
            systemDiagram.draggingElement = null;

            (<any>window).systemDiagram = systemDiagram;
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
            [fileHandle] = await (<any>window).showOpenFilePicker();
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
            
            const fileHandle = await (<any> window).showSaveFilePicker(pickerOptions);
            (<any> window).filePath = fileHandle;
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
            
            if ((<any> window).filePath == null) {
                (<any> window).filePath = await (<any>window).showSaveFilePicker(pickerOptions);
            }
            
            const writableFileStream = await (<any> window).filePath.createWritable();
            await writableFileStream.write(blob);
            await writableFileStream.close();
        }

        public getSystemDiagram() {
            return JSON.stringify({
                elements: (<any>window).systemDiagram.elements,
                bonds: (<any>window).systemDiagram.bonds
            });
        }

        public setModifier(i: number, value: boolean) {
            let element = (<any>window).systemDiagram.state.selectedElement;
            if (element) {
                if (value) { // adding modifier
                    element.modifiers.push(i);
                } else { // removing modifier
                    element.modifiers.splice(element.modifiers.indexOf(i), 1);
                }
            }
        }

        public getGraphByIndex(i: string) {
            if (i == "1") {
                return (<any>window).systemDiagram;
            } else if (i == "2") {
                return (<any>window).unsimpBG;
            } else if (i == "3") {
                return (<any>window).simpBG;
            } else {
                return (<any>window).causalBG;
            }
        }

        public setZoom(i: number) {
            let graph = this.getGraphByIndex((<any>window).tabNum);

            // converts SVG position to svg center of view window
            let svgDim = graph.svgG.node().getBBox();
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
            (<any>window).tabNum = key;
        }
        
        public setVelocity(velocity: number) {
            let element = (<any>window).systemDiagram.state.selectedElement;
            let edge = (<any>window).systemDiagram.state.selectedBond;
            if (element) {
                element.velocity = velocity;
            } else if (edge) {
                edge.velocity = velocity;
            }
            (<any>window).systemDiagram.updateGraph();
        }
        
        public generateURL(){
            return JSON.stringify({
                elements: (<any>window).systemDiagram.elements,
                bonds: (<any>window).systemDiagram.bonds
            }, function (key, val) {
                return val.toFixed ? Number(val.toFixed(3)) : val;
            });
        }
        
        public textToClipboard(text: string) {
            navigator.clipboard.writeText(text);
        }
        
        public runTutorial(){
            (<any> window).introJs().setOptions({
                steps: [{
                    title: 'Welcome to BoGL Web',
                    intro: 'This application is used to construct system diagrams and generate bond graphs from those diagrams.'
                },{
                    element: document.querySelector('.ant-tabs-content'),
                    title: 'The Canvas',
                    intro: 'This is the canvas, where you can construct, move, and rearrange your system diagrams. First, use your mouse to select and drag some elements into the canvas. Then hover your cursor over the border of the elements, where you will see a green circle. This circle indicates that you can create an edge (connection) between two elements. Clicking and dragging will show a black line symbolizing the edge that you can drop in another element to complete an edge. If you see a red X when you try to make an edge, it means the edge you are trying to make is invalid (the two elements do not make sense to be connected).'
                },{
                    element: document.querySelector('#graphMenu'),
                    intro: 'This is the element palette. After expanding the menus, you can select and drag elements into the canvas to construct system diagrams.'
                },{
                    element: document.querySelector('#modifierMenu'),
                    intro: 'In this menu can add modifiers to the selected element. Some modifiers require multiple elements to be selected. You can do this by (describe how to multiselect).'
                },{
                    element: document.querySelector('#zoomMenu'),
                    intro: 'This menu allows you to zoom in and out of the canvas. You can also use the scroll wheel to perform this action. Dragging the canvas with the right mouse click will pan the graph.'
                },{
                    element: document.querySelector('#generateButton'),
                    intro: 'The generate button allows you to turn your system diagram into a bond graph. While the bond graph is generating you will see a loading bar which signifies that BoGL Web is processing your System Diagram. This can take a few seconds.'
                },{
                    element: document.querySelector('.ant-tabs-nav-list'),
                    intro: 'These tabs store different stages of the bond graph generation. You can look at the unsimplified bond graph, the simplified bond graph, or the causal bond graph.'
                },{
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(2)'),
                    intro: 'This is the file menu. It allows you to:\n' +
                        'Create a new system diagram\n' +
                        'Open a previously saved .bogl file from your computer\n' +
                        'Save a .bogl file to your computer\n' +
                        'Export an image of your system diagram or bond graph\n' +
                        'Generate a URL that links to your system diagram\n'
                },{
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(3)'),
                    intro: 'This is the edit menu. It allows you to:\n' +
                        'Copy, cut, and paste elements of the system diagram\n' +
                        'Undo and redo changes\n' +
                        'Delete elements from the System Diagram\n'
                },{
                    element: document.querySelector('#iconButtons'),
                    intro: 'You can perform similar features to the edit menu here. By pressing the icons you can save a system diagram, cut, copy, paste, undo, redo, and delete an element or edge from the system diagram.'
                },{
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(4)'),
                    intro: 'This is the help menu. It allows you to:\n' +
                        'Confirm deleting many items at once. Selecting this option will allow you to select multiple items and then delete them all at once.\n' +
                        'Start this tutorial again\n' +
                        'Load example system diagrams\n' +
                        'Report bugs that you find\n' +
                        'Learn about who created BoGL Web System\n'
                }]
            }).start(); 
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}