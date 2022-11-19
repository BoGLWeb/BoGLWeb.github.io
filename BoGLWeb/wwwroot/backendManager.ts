import { populateMenu } from "./main";
import { GraphBond } from "./types/bonds/GraphBond";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { SystemDiagramElement } from "./types/elements/SystemDiagramElement";
import { SystemDiagram } from "./types/graphs/SystemDiagram";

export namespace backendManager {
    export class BackendManager {
        public test(text: string) {
            console.log(text);
        }

        public displayUnsimplifiedBondGraph(jsonString: string) {
            console.log(jsonString);
        }

        public displaySimplifiedBondGraph(jsonString: string) {
            console.log(jsonString);
        }

        public displayCausalBondGraphOptions(jsonStrings: Array<string>) {
            jsonStrings.forEach(function (value) {
                console.log(value);
            });
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
                edges.push(new GraphBond(elements[edge.source], elements[edge.target]));
            }

            (<any>window).systemDiagramSVG.selectAll('*').remove();

            var systemDiagram = new SystemDiagramDisplay((<any> window).systemDiagramSVG, new SystemDiagram(elements, edges));
            systemDiagram.draggingElement = null;

            document.addEventListener("mouseup", function () {
                document.body.style.cursor = "auto";
                systemDiagram.draggingElement = null;
            });

            populateMenu(systemDiagram);
            systemDiagram.updateGraph();

            let svgDim = d3.select('#systemDiagram > svg > g').node().getBBox();
            let windowDim = document.getElementById("systemDiagram").getBoundingClientRect();
            let scale = 1;
            if (svgDim.width / svgDim.height > windowDim.width / windowDim.height) {
                scale = (0.8 * windowDim.width) / svgDim.width;
            } else {
                scale = (0.8 * windowDim.height) / svgDim.height;
            }
            d3.select('#systemDiagram > svg > g').style("transform", "translate(" + ((-svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2))) + "px, "
                + ((-svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2))) + "px) scale(" + scale + ")");
        }

        public getSystemDiagram() {
            return "sysDiagram";
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}