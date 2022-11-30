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
            d3.select('#systemDiagram > svg > g').attr("transform", "translate(" + xTrans + ", " + yTrans + ") scale(" + scale + ")");
            systemDiagram.svg.call(systemDiagram.dragSvg().scale(scale).translate([xTrans, yTrans])).on("dblclick.zoom", null);
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
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}