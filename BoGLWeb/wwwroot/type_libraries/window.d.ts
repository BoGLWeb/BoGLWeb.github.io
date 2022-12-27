import { SVGSelection } from "./d3-selection";
import { backendManager } from "./../backendManager";
import { SystemDiagramDisplay } from "../types/display/SystemDiagramDisplay";
import { BondGraphDisplay } from "../types/display/BondGraphDisplay";

// Adds the elements we're storing on the window to the window type
declare global {
    interface Window {
        systemDiagramSVG: SVGSelection;
        backendManager: typeof backendManager;
        systemDiagram: SystemDiagramDisplay;
        tabNum: string;
        unsimpBGSVG: SVGSelection;
        simpBGSVG: SVGSelection;
        causalBGSVG: SVGSelection;
        unsimpBG: BondGraphDisplay;
        simpBG: BondGraphDisplay;
        causalBG: BondGraphDisplay;
        showSaveFilePicker(options?: any): Promise<FileSystemFileHandle>;
        showOpenFilePicker(options?: any): Promise<FileSystemFileHandle[]>;
        filePath: FileSystemFileHandle;
    }

    interface FileSystemFileHandle extends FileSystemHandle {
        readonly kind: "file";
        getFile(): Promise<File>;
        createWritable(options?: any): Promise<any>;
    }
}

export { }