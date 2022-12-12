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
        
        public test(text: string) {
            console.log(text);
        }

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
                bond.velocity = edge.velocity;
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

            console.log(graph, svgDim, windowDim);
            graph.changeScale(graph.initXPos + ((xTrans - graph.initXPos) * scaleDiff), graph.initYPos + ((yTrans - graph.initYPos) * scaleDiff), i / 100, true);
        }

        public setTab(key: string) {
            (<any>window).tabNum = key;
            console.log(key);
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}