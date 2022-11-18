namespace backendManager {
    class BackendManager {
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

        public getSystemDiagram() {
            return "sysDiagram";
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}