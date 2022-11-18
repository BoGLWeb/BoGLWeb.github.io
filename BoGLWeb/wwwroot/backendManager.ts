namespace backendManager {
    class BackendManager {
        public test(text: string) {
            console.log(text);
        }

        public displayBondGraph(jsonString: string) {

        }

        public getSystemDiagram() {
            return "sysDiagram";
        }
    }

    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}