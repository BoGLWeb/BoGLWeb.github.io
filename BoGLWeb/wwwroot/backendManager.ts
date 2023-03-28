import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { GraphBond } from "./types/bonds/GraphBond";
import { GraphBondID } from "./types/bonds/GraphBondID";
import { BaseGraphDisplay } from "./types/display/BaseGraphDisplay";
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

        imageBuffer = 15;

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
                return new BondGraphBond(elements[b.sourceID], elements[b.targetID], b.causalStroke, b.causalStrokeDirection, !b.hasDirection && id != 0, b.velocity);
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

            window.systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram([], []));

            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", Array.from(elements.values()).map(e => JSON.stringify(e)).concat(edges.map(e => JSON.stringify(e))),
                ...window.systemDiagram.listToIDObjects([].concat(window.systemDiagram.selectedElements).concat(window.systemDiagram.selectedBonds)), false);

            let systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram(Array.from(elements.values()), edges));
            systemDiagram.draggingElement = null;
            window.systemDiagram = systemDiagram;
            systemDiagram.updateGraph();
            this.zoomCenterGraph("1");
            let bounds = (systemDiagram.svg.select("g").node() as HTMLElement).getBoundingClientRect();
            systemDiagram.initWidth = bounds.width;
            systemDiagram.initHeight = bounds.height;
        }

        public async exportAsImage() {
            let graph = this.getGraphByIndex(window.tabNum);
            let svg = graph.svg;
            if (this.getTabNum() == 1) {
                await this.convertImages("image.hoverImg");
            }
            let copy = svg.node().cloneNode(true);
            this.applyInlineStyles(svg, d3.select(copy), graph);
            this.svgToCanvas(svg, copy as SVGElement, graph);
        }

        public markerToString(marker: string) {
            return marker.replaceAll('"', "&quot;").replaceAll("#", encodeURIComponent("#")).replace("_selected", "");
        }

        public svgToCanvas(oldSVG: SVGSelection, svg: SVGElement, graph: BaseGraphDisplay) {
            let scale = parseFloat(oldSVG.select("g").attr("transform").split(" ")[2].replace("scale(", "").replace(")", ""));
            let bounds = (oldSVG.select("g").node() as HTMLElement).getBoundingClientRect();
            let isBondGraph = graph instanceof BondGraphDisplay;
            let w = bounds.width / scale + (isBondGraph ? this.imageBuffer * 2 : 0);
            let h = bounds.height / scale + (isBondGraph ? this.imageBuffer * 2 : 0);
            svg.setAttribute("viewbox", "0 0 " + w + " " + h);
            svg.setAttribute("width", w + "px");
            svg.setAttribute("height", h + "px");

            let markers = {};

            // bond graph with directed edges
            // This is necessary because the marker-start and marker-end CSS properties are messing up string serialization,
            // so we put in placeholders then replace them after string serialization
            if (this.getTabNum() > 1) {
                svg.id = "currentSVG";
                document.body.appendChild(svg);
                let paths = d3.selectAll("#currentSVG > g > #bondGroup > .link");
                for (let i = 0; i < paths[0].length; i++) {
                    let path = paths[0][i] as HTMLElement;
                    let hasMarkerEnd = path.style?.markerEnd;
                    let hasMarkerStart = path.style?.markerStart;
                    if (hasMarkerEnd) {
                        markers["~~~" + i] = this.markerToString(path.style.markerEnd);
                    }
                    if (hasMarkerStart) {
                        markers["@@@" + i] = this.markerToString(path.style.markerStart);
                    }
                    path.setAttribute("style", (hasMarkerEnd ? "marker-end: ~~~" + i + "; " : "") + (hasMarkerStart ? "marker-start: @@@" + i + "; " : "") + "stroke-width: 2px; fill: none; stroke: black;");
                }
                svg.id = "";
            }

            let img = new Image(w, h);
            let serializer = new XMLSerializer();
            let svgStr = serializer.serializeToString(svg);
            d3.select("#currentSVG").remove();

            for (const i in markers) {
                svgStr = svgStr.replaceAll(i, markers[i]);
            }

            img.src = "data:image/svg+xml;utf8," + svgStr;

            var canvas = document.createElement("canvas");
            document.body.appendChild(canvas);

            canvas.width = w;
            canvas.height = h;
            img.onload = () => {
                canvas.getContext("2d").drawImage(img, 0, 0, w, h);
                let filenames = ["systemDiagram.png", "unsimpBG.png", "simpBG.png", "causalBG.png"];
                canvas.toBlob(blob => {
                    let pickerOptions = {
                        suggestedName: filenames[this.getTabNum() - 1],
                        types: [
                            {
                                description: 'PNG File',
                                accept: {
                                    'image/png': ['.png'],
                                },
                            },
                            {
                                description: 'SVG File',
                                accept: {
                                    'image/svg+xml': ['.svg'],
                                },
                            },
                            {
                                description: 'JPEG File',
                                accept: {
                                    'image/jpeg': ['.jpeg', '.jpg'],
                                },
                            }
                        ],
                    };

                    this.saveAsBlob(blob, pickerOptions, new Blob([svgStr.replaceAll("%23", "#")]));
                    graph.updateGraph();
                });
            };
        }

        public applyInlineStyles(oldSVG: SVGSelection, svg: SVGSelection, graph: BaseGraphDisplay) {
            svg.selectAll(".link")
                .style("fill", "none")
                .style("stroke", "black")
                .style("stroke-width", "4px");
            svg.selectAll(".boglElem")
                .style("fill-opacity", "0");
            svg.selectAll(".outline")
                .style("stroke", "black");
            svg.selectAll("text")
                .style("fill", "black")
                .style("font-size", "30px")
                .style("font-family", "Arial")
                .style("fill-opacity", "1")
                .attr("dy", "0.25em");
            svg.selectAll(".velocity_5_edge")
                .attr("dx", "0em")
                .attr("dy", "0.5em");
            svg.selectAll(".velocity_4_edge")
                .attr("dy", "0.5em");
            svg.selectAll(".velocity_6_edge, .velocity_7_edge, .velocity_6_element, .velocity_5_element")
                .attr("dx", "-0.75em")
                .attr("dy", "0.5em");
            svg.selectAll(".velocity_7_element, .velocity_8_element, .velocity_1_edge, .velocity_8_edge")
                .attr("dx", "-0.75em");
            svg.selectAll(".velocity_1_element")
                .attr("dx", "-0.25em");
            svg.selectAll(".dragline")
                .style("display", "none");
            svg.style("background-color", "white");
            svg.select("circle")
                .style("display", "none");
            svg.selectAll(".bondGraphText")
                .style("font-size", "14px")
                .style("font-family", "'Segoe UI', 'SegoeUI', sanserif !important");
            oldSVG.select(".dragline").remove();
            if (graph.bonds.length == 0) {
                oldSVG.select("#bondGroup").remove();
            }
            svg.selectAll("edgeHover").remove()
            let bounds = (oldSVG.select("g").node() as HTMLElement).getBoundingClientRect();
            let minX = Infinity;
            let minY = Infinity;
            let maxX = -Infinity;
            let maxY = -Infinity;
            for (let e of graph.elements) {
                if (e.x < minX) minX = e.x;
                if (e.y < minY) minY = e.y;
                if (e.x > maxX) maxX = e.x;
                if (e.y > maxY) maxY = e.y;
            }
            let scale = parseFloat(oldSVG.select("g").attr("transform").split(" ")[2].replace("scale(", "").replace(")", ""));
            let isBondGraph = graph instanceof BondGraphDisplay;
            svg.select("g").attr("transform", "translate(" + ((bounds.width / scale) / 2 + (maxX - minX) / 2 - maxX + (isBondGraph ? this.imageBuffer : 0)) + ", "
                + ((bounds.height / scale) / 2 + (maxY - minY) / 2 - maxY + (isBondGraph ? this.imageBuffer : 0)) + ") scale(1)");
        }

        // this will break if additional image types beyond system diagram elements are added to BoGL Web
        public async convertImages(query) {
            const images = document.querySelectorAll(query);

            for (let i = 0; i < images.length; i++) {
                let image = images.item(i);
                await fetch(image.href.baseVal)
                    .then(res => res.text())
                    .then(data => {
                        const parser = new DOMParser();
                        const svg = parser.parseFromString(data, 'image/svg+xml').querySelector('svg');

                        if (image.id) svg.id = image.id;
                        // @ts-ignore
                        if (image.className) svg.classList = image.classList;
                        svg.setAttribute("height", "50px");
                        svg.setAttribute("width", "50px");
                        svg.setAttribute("x", "-25px");
                        svg.setAttribute("y", "-25px");

                        image.parentNode.replaceChild(svg, image);
                    })
                    .catch(error => console.error(error))
            }
        }

        public zoomCenterGraph(index: string) {
            let graph = this.getGraphByIndex(index);
            let prevDisplay = graph.svgG.node().parentElement.parentElement.parentElement.style.display;
            graph.svgG.node().parentElement.parentElement.parentElement.style.display = "block";
            let svgDim = (graph.svgG.node() as SVGSVGElement).getBBox();
            let windowDim = graph.svgG.node().parentElement.getBoundingClientRect();
            let scale = 1;
            if (svgDim.width / svgDim.height > windowDim.width / windowDim.height) {
                scale = (0.8 * windowDim.width) / svgDim.width;
            } else {
                scale = (0.8 * windowDim.height) / svgDim.height;
            }
            scale = Math.min(Math.max(scale, 0.25), 1.75);
            let xTrans = -svgDim.x * scale + (windowDim.width / 2) - (svgDim.width * scale / 2);
            let yTrans = -svgDim.y * scale + (windowDim.height / 2) - (svgDim.height * scale / 2);
            graph.changeScale(xTrans, yTrans, scale);
            graph.svgG.node().parentElement.parentElement.parentElement.style.display = prevDisplay;
        }

        public async openFile() {
            await this.openFileCompatible();
        }

        public getTabNum(): number {
            return parseInt(window.tabNum);
        }

        public saveFileNoPicker(fileName, blob) {
            const urlToBlob = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.setProperty('display', 'none');
            document.body.appendChild(a);
            a.href = urlToBlob;
            a.download = fileName;
            a.click();
            window.URL.revokeObjectURL(urlToBlob);
            a.remove();
        }

        public async openFileCompatible() {
            let input = document.createElement('input');
            input.type = 'file';
            input.accept = ".bogl";
            input.onchange = async _ => {
                let files = Array.from(input.files);
                let text = await files[0].text();
                let systemDiagramText = await DotNet.invokeMethodAsync("BoGLWeb", "openSystemDiagram", text);
                if (systemDiagramText != null) {
                    this.loadSystemDiagram(systemDiagramText);
                }
            };
            input.click();
        }

        public async saveAsFile(fileName: string, contentStreamReference: any, pickerOptions) {
            const arrayBuffer = await contentStreamReference.arrayBuffer();
            const blob = new Blob([arrayBuffer]);

            pickerOptions = pickerOptions ?? {
                suggestedName: fileName,
                types: [
                    {
                        description: 'A BoGL File',
                        accept: {
                            'text/plain': ['.bogl'],
                        },
                    },
                ],
            };

            await this.saveAsBlob(blob, pickerOptions, null);
        }

        public async saveAsBlob(blob: any, pickerOptions: any, svgBlob: any) {
            if (window.showSaveFilePicker) {
                const fileHandle = await window.showSaveFilePicker(pickerOptions);
                window.filePath = fileHandle;
                const writableFileStream = await fileHandle.createWritable();
                await writableFileStream.write(fileHandle.name.includes(".svg") || fileHandle.name.includes(".svgz") ? svgBlob : blob);
                await writableFileStream.close();
            } else {
                this.saveFileNoPicker(pickerOptions.suggestedName, blob);
            }
        }

        public async saveFile(fileName: string, contentStreamReference: any) {
            const arrayBuffer = await contentStreamReference.arrayBuffer();
            const blob = new Blob([arrayBuffer]);

            const pickerOptions = {
                suggestedName: fileName,
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
                if (window.showSaveFilePicker) {
                    window.filePath = await window.showSaveFilePicker(pickerOptions);
                    const writableFileStream = await window.filePath.createWritable();
                    await writableFileStream.write(blob);
                    await writableFileStream.close();
                } else {
                    this.saveFileNoPicker("systemDiagram.bogl", blob);
                }
            }
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
            window.systemDiagram.updateGraph();
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
            let windowDim = graph.svg.node().parentElement.getBoundingClientRect();

            let xOffset = (graph.prevScale * 100 - i) * (graph.svgX - graph.initXPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);
            let yOffset = (graph.prevScale * 100 - i) * (graph.svgY - graph.initYPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);

            if (graph.prevScale * 100 - i != 0) {
                graph.changeScale(windowDim.width / 2 - (windowDim.width / 2 - graph.svgX) - xOffset, windowDim.height / 2 - (windowDim.height / 2 - graph.svgY) - yOffset, i / 100);
            }
        }

        public setTab(key: string) {
            window.tabNum = key;
            DotNet.invokeMethodAsync("BoGLWeb", "SetScale", this.getGraphByIndex(key).prevScale);
        }

        public setVelocity(velocity: number) {
            let prevVelVals = window.systemDiagram.getSelection().map(e => e.velocity);
            for (const e of window.systemDiagram.getSelection()) {
                if (e instanceof GraphBond || ElementNamespace.elementTypes[e.type].velocityAllowed) {
                    e.velocity = velocity;
                }
            }
            window.systemDiagram.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionVelocity", ...window.systemDiagram.listToIDObjects(window.systemDiagram.getSelection()), velocity, prevVelVals);
        }

        public generateURL() {
            return JSON.stringify({
                elements: window.systemDiagram.elements.map(e => {
                    e.x = Math.round(e.x * 10) / 10;
                    e.y = Math.round(e.y * 10) / 10;
                    return e;
                }),
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
                    intro: '<p><b>Welcome To BoGL Web</b></p><p>' +
                        'This application is used to construct system diagrams and generate bond graphs from those diagrams</p>'
                }, {
                    element: document.querySelector('.card-container'),
                    intro: '<p><b>The Canvas</b></p><p>The highlighted space is the Canvas where you can construct, move, and rearrange your system diagrams.</p>'
                }, {
                    element: document.querySelector('#graphMenu'),
                    intro: '<p><b>The Element Palette</b></p><p>This is the element palette. After expanding the menus, you can select and drag elements onto the canvas to construct system diagrams</p>'
                }, {
                    element: document.querySelector('.card-container'),
                    intro: '<p><b>Constructing a System Diagram</b></p><p>Select and drag an element to add it to the Canvas, and then select near its black border to start creating an edge.  You can then select near a second element to finish making the edge. If you see a green circle, your edge is valid, if you see a red X when you try to make an edge, it means the edge you are trying to make is invalid (the two elements do not make sense to be connected).' +
                        '<br><br><img src="images/tutorial/EdgeCreationGif-Edited.gif" width="100%">' +
                        '</p>'
                },
                {
                    element: document.querySelector('#modifierMenu'),
                    intro: '<p><b>The Modifier Menu</b></p><p>Use this menu to add modifiers to the selected element. Some modifiers require multiple elements to be selected. You can do this by holding down the control key and clicking elements you want to select, or drag the cursor across the canvas with the left mouse button to create a selection region. All elements that are completely or partially inside the region will be selected.</p>'
                }, {
                    element: document.querySelector('#zoomMenu'),
                    intro: '<p><b>The Zoom Menu</b></p><p>This menu allows you to zoom in and out of the canvas. You can use the zoom slider, or your scroll wheel.' +
                        '<br><br><img src="images/tutorial/ZoomGif-Edited.gif" width="100%">' +
                        '</p>'
                }, {
                    element: document.querySelector('#generateButton'),
                    intro: '<p><b>The Generate Button</b></p><p>The generate button allows you to turn your system diagram into a bond graph. While the bond graph is generating you will see a loading bar which signifies that BoGL Web is processing your System Diagram. This can take a few seconds.</p>'
                }, {
                    element: document.querySelector('.ant-tabs-nav-list'),
                    intro: '<p><b>The Tabs</b></p><p>These tabs change what stage of the bond graph generation is being displayed. You can look at the unsimplified bond graph, the simplified bond graph, or the causal bond graph</p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(2)'),
                    intro: '<p><b>The File Menu</b></p>' +
                        '<p>This is the file menu. Selecting it opens a menu which allows you to:' +
                        '<ul>' +
                        '<li>Create a new system diagram</li>' +
                        '<li>Open a previously saved .bogl file from your computer</li>' +
                        '<li>Save a .bogl file representing the System Diagram to your computer</li>' +
                        '<li>Generate a URL that that can be used to chare your System Diagram</li>' +
                        '</ul></p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(3)'),
                    intro: '<p><b>The Edit Menu</b></p>' +
                        '<p>This is the edit menu. Selecting it open a menu which allows you to:' +
                        '<ul>' +
                        '<li>Copy, cut, and paste elements of the system diagram</li>' +
                        '<li>Undo and redo changes</li>' +
                        '<li>Delete elements from the System Diagram</li>' +
                        '</ul></p>'
                }, {
                    element: document.querySelector('#iconButtons'),
                    intro: '<p><b>The Toolbar</b></p><p>You can perform similar features to the edit menu here. By selecting the icons you can save a System Diagram, cut, copy, paste, undo, redo, and delete an element or edge from the System Diagram.</p>'
                }, {
                    element: document.querySelector('.ant-menu-horizontal > :nth-child(4)'),
                    intro: '<p><b>The Help Menu</b></p>' +
                        '<p>This is the help menu. Selecting it opens a menu which allows you to:' +
                        '<ul>' +
                        '<li>Confirm deleting many items at once. Selecting this option will allow you to select multiple items and then delete them all at once.</li>' +
                        '<li>Start this tutorial again</li>' +
                        '<li>Load example System Diagrams</li>' +
                        '<li>Report bugs that you find</li>' +
                        '<li>Learn about who created BoGL Web System</li>' +
                        '</ul></p>'
                }]
            }).onbeforechange(function () {
                window.dispatchEvent(new Event('resize'));
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

        public parseEdgeIDStrings(edgeIDs: string[]): GraphBondID[] {
            let edges: GraphBondID[] = [];
            let i = 0;
            for (const edgeString of edgeIDs) {
                let json = JSON.parse(edgeString);
                edges.push(new GraphBondID(json.source, json.target, i));
                i++;
            }
            return edges;
        }

        checkBondIDs(bondIDs: GraphBondID[], b: GraphBond): GraphBondID {
            let sourceID = b.source.id;
            let targetID = b.target.id;
            return bondIDs.find(e => e.checkEquality(sourceID, targetID));
        }

        async handleUndoRedo(undo: boolean) {
            DotNet.invokeMethodAsync("BoGLWeb", "UndoRedoHandler", parseInt(window.tabNum), undo);
        }

        public urDoAddSelection(newObjects: string[], prevSelElIDs: number[], prevSelectedEdges: string[], highlight: boolean, isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(newObjects);
            if (isUndo) {
                let elIDs = elements.map(e => e.id);
                let elBonds = bonds.map(b => { return new GraphBondID(b.source.id, b.target.id); });
                sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                let prevSelEdgeIDs = this.parseEdgeIDStrings(prevSelectedEdges);
                if (highlight) {
                    sysDiag.setSelection(sysDiag.elements.filter(e => prevSelElIDs.includes(e.id)), sysDiag.bonds.filter(b => this.checkBondIDs(prevSelEdgeIDs, b)));
                } else {
                    sysDiag.setSelection([], []);
                }
            } else {
                sysDiag.elements = sysDiag.elements.concat(elements);
                sysDiag.bonds = sysDiag.bonds.concat(bonds);
                if (highlight) {
                    sysDiag.setSelection(elements, bonds);
                } else {
                    sysDiag.setSelection([], []);
                }
            }
            sysDiag.updateGraph();
            sysDiag.updateMenus();
        }

        public urDoDeleteSelection(deletedObjects: string[], unselectedDeletedEdges: string[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(deletedObjects);
            let [_, unselectedBonds] = this.parseElementAndEdgeStrings(unselectedDeletedEdges);
            if (isUndo) {
                sysDiag.elements = sysDiag.elements.concat(elements);
                unselectedBonds = unselectedBonds.map(b => {
                    b.source = sysDiag.elements.find(e => e.id == b.source.id);
                    b.target = sysDiag.elements.find(e => e.id == b.target.id);
                    return b;
                });
                bonds = bonds.map(b => {
                    b.source = sysDiag.elements.find(e => e.id == b.source.id);
                    b.target = sysDiag.elements.find(e => e.id == b.target.id);
                    return b;
                });
                sysDiag.bonds = sysDiag.bonds.concat(bonds).concat(unselectedBonds);
                sysDiag.setSelection(elements, bonds);
            } else {
                let elIDs = elements.map(e => e.id);
                let elBonds = bonds.concat(unselectedBonds).map(b => { return new GraphBondID(b.source.id, b.target.id); });
                sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                sysDiag.setSelection([], []);
            }
            sysDiag.updateGraph();
            sysDiag.updateMenus();
        }

        public urDoChangeSelection(elIDsToAdd: number[], edgesToAdd: string[], elIDsToRemove: number[], edgesToRemove: string[], isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            let addToSelectionEdges = this.parseEdgeIDStrings(edgesToAdd);
            let removeFromSelectionEdges = this.parseEdgeIDStrings(edgesToRemove);
            let elAddSet = isUndo ? elIDsToRemove : elIDsToAdd;
            let elRemoveSet = isUndo ? elIDsToAdd : elIDsToRemove;
            let edgeAddSet = isUndo ? removeFromSelectionEdges : addToSelectionEdges;
            let edgeRemoveSet = isUndo ? addToSelectionEdges : removeFromSelectionEdges;
            // @ts-ignore // may want to fix this later, but shouldn't be an issue as long as tab index is correctly recorded 
            diagram.selectedElements = diagram.selectedElements.concat(diagram.elements.filter(e => elAddSet.includes(e.id)));
            diagram.selectedBonds = diagram.selectedBonds.concat(diagram.bonds.filter(b => this.checkBondIDs(edgeAddSet, b)));
            diagram.selectedElements = diagram.selectedElements.filter(e => !elRemoveSet.includes(e.id));
            diagram.selectedBonds = diagram.selectedBonds.filter(b => !this.checkBondIDs(edgeRemoveSet, b));
            diagram.updateGraph();
            diagram.updateMenus();
        }

        public urDoMoveSelection(elements: number[], xOffset: number, yOffset: number, isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            diagram.elements.filter(e => elements.includes(e.id)).forEach(e => {
                e.x = e.x + (isUndo ? -1 : 1) * xOffset;
                e.y = e.y + (isUndo ? -1 : 1) * yOffset;
            });
            diagram.updateGraph();
        }

        public urDoChangeSelectionVelocity(elIDs: number[], edgeIDs: string[], velID: number, prevVelVals: number[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let bondIDs = this.parseEdgeIDStrings(edgeIDs);
            sysDiag.elements.filter(e => elIDs.includes(e.id)).forEach(e => e.velocity = isUndo ? prevVelVals[elIDs.findIndex(i => i == e.id)] : velID);
            sysDiag.bonds.filter(b => this.checkBondIDs(bondIDs, b)).forEach(b => b.velocity = isUndo ? prevVelVals[elIDs.length + this.checkBondIDs(bondIDs, b).velID] : velID);
            sysDiag.updateGraph();
            sysDiag.updateVelocityMenu();
        }

        public urDoChangeSelectionModifier(elIDs: number[], modID: number, modVal: boolean, prevModVals: boolean[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;

            elIDs.forEach(function (id, i) {
                let el = sysDiag.elements.find(e => e.id == id);
                if (isUndo) {
                    if (prevModVals[i] && !el.modifiers.includes(modID)) {
                        el.modifiers.push(modID);
                    } else if (!prevModVals[i] && el.modifiers.includes(modID)) {
                        el.modifiers.splice(el.modifiers.indexOf(modID), 1);
                    }
                } else {
                    if (modVal && ElementNamespace.elementTypes[el.type].allowedModifiers.includes(modID) && !el.modifiers.includes(modID)) {
                        el.modifiers.push(modID);
                    } else if (el.modifiers.includes(modID)) {
                        el.modifiers.splice(el.modifiers.indexOf(modID), 1);
                    }
                }
            });

            sysDiag.updateGraph();
            sysDiag.updateModifierMenu();
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