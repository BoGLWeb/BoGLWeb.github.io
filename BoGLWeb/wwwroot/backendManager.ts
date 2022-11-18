import { GraphBond } from "./types/bonds/GraphBond";
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
            console.log(jsonString);

            let parsedJson = JSON.parse(jsonString);
            let elements = []
            for (let element of parsedJson.elements) {
                elements.push(element as unknown as SystemDiagramElement);
            }
            let edges = []
            for (let edge of parsedJson.edges) {
                edges.push(new GraphBond(elements[edge.source], elements[edge.target]));
            }
            console.log(new SystemDiagram(elements, edges));
        }

        public getSystemDiagram() {
            return "sysDiagram";
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}