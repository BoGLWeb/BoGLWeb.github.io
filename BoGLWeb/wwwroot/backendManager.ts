 import { BondGraphBond } from "./types/bonds/BondGraphBond";
import { GraphBond } from "./types/bonds/GraphBond";
import { GraphBondID } from "./types/bonds/GraphBondID";
import { BaseGraphDisplay } from "./types/display/BaseGraphDisplay";
import { BondGraphDisplay } from "./types/display/BondGraphDisplay";
import { SystemDiagramDisplay } from "./types/display/SystemDiagramDisplay";
import { BondGraphElement } from "./types/elements/BondGraphElement";
import { ElementNamespace } from "./types/elements/ElementNamespace";
import { GraphElement } from "./types/elements/GraphElement";
import { SystemDiagramElement } from "./types/elements/SystemDiagramElement";
import { BondGraph } from "./types/graphs/BondGraph";
import { SystemDiagram } from "./types/graphs/SystemDiagram";
import { SVGSelection } from "./type_libraries/d3-selection";

export namespace backendManager {
    export class BackendManager {

        imageBuffer = 15;

        // takes in a list of JSON elements and centers them around (0, 0) while turning them into GraphElements
        centerElements(jsonElements: any[], bondGraph: boolean) {
            let [minX, minY, maxX, maxY] = [Infinity, Infinity, -Infinity ,-Infinity];

            let elements = new Map<number, GraphElement>();
            let i = 0;
            // find min and max x and y while making GraphElement objects
            for (let e of jsonElements) {
                if (e.x < minX) minX = e.x;
                if (e.y < minY) minY = e.y;
                if (e.x > maxX) maxX = e.x;
                if (e.y > maxY) maxY = e.y;

                let id = e.id ?? i++;
                elements.set(id, bondGraph ? new BondGraphElement(i, e.ID, e.label, e.x, e.y)
                    : new SystemDiagramElement(id, e.type, e.x, e.y, e.velocity, e.modifiers))
            }

            // transform elements to center around (0, 0)
            elements.forEach(e => {
                e.x += (maxX - minX) / 2 - maxX;
                e.y += (maxY - minY) / 2 - maxY;
            });

            return elements;
        }

        // parses a bond graph string into a bond graph display
        parseAndDisplayBondGraph(id: number, jsonString: string, svg: SVGSelection) {
            let bg = JSON.parse(jsonString);
            let elements = Array.from(this.centerElements(JSON.parse(bg.elements), true).values()) as BondGraphElement[];

            // makes BondGraphDisplay
            let bonds = JSON.parse(bg.bonds).map(b => {
                return new BondGraphBond(b.ID, elements[b.sourceID], elements[b.targetID], b.causalStroke, b.causalStrokeDirection, !b.hasDirection && id != 0, b.effortLabel, b.flowLabel);
            }) as BondGraphBond[];
            let bondGraph = new BondGraphDisplay(id, svg, new BondGraph(elements, bonds));

            // assigns BondGraphDisplay to appropriate window attribute
            if (id == 0) {
                window.unsimpBG = bondGraph;
            } else if (id == 1) {
                window.simpBG = bondGraph;
            } else {
                window.causalBG = bondGraph;
            }

            // updates and zooms new bond graph display
            bondGraph.updateGraph();
            this.zoomCenterGraph(JSON.stringify(id + 2));
        }

        // parses a list of JSON strings representing elements and bonds into SystemDiagramElement and GraphBond lists
        parseElementAndEdgeStrings(objects: string[]): [SystemDiagramElement[], GraphBond[]] {
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

        // transofmrs edge IDs into GraphBondID objects
        parseEdgeIDStrings(edgeIDs: string[]): GraphBondID[] {
            let edges: GraphBondID[] = [];
            let i = 0;
            for (const edgeString of edgeIDs) {
                let json = JSON.parse(edgeString);
                edges.push(new GraphBondID(json.source, json.target, i));
                i++;
            }
            return edges;
        }

        // determines whether two GraphBondID objects are equal
        checkBondIDs(bondIDs: GraphBondID[], b: GraphBond): GraphBondID {
            let sourceID = b.source.id;
            let targetID = b.target.id;
            return bondIDs.find(e => e.checkEquality(sourceID, targetID));
        }

        // encodes a marker string safely so that it can be added to a URI
        markerToString(marker: string) {
            return marker.replaceAll('"', "&quot;").replaceAll("#", encodeURIComponent("#")).replace("_selected", "");
        }

        // converts an SVG to an HTML canvas to allow for image export
        svgToCanvas(oldSVG: SVGSelection, svg: SVGElement, graph: BaseGraphDisplay) {
            let scale = parseFloat(oldSVG.select("g").attr("transform").split(" ")[2].replace("scale(", "").replace(")", ""));
            let bounds = (oldSVG.select("g").node() as HTMLElement).getBoundingClientRect();
            let w = bounds.width / scale + this.imageBuffer * 2;
            let h = bounds.height / scale + this.imageBuffer * 2;
            svg.setAttribute("viewbox", "0 0 " + w + " " + h);
            svg.setAttribute("width", w + "px");
            svg.setAttribute("height", h + "px");
            w *= 5;
            h *= 5;

            let markers = {};

            // For any bond graph with directed edges, replace marker strings with placeholders
            // This is necessary because the marker-start and marker-end CSS properties mess up string serialization,
            // so we put in placeholders then replace them after string serialization
            if (this.getTabNum() > 1) {
                svg.id = "currentSVG";
                document.body.appendChild(svg);
                let paths = d3.selectAll("#currentSVG > g > #bondGroup > g > .link");
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

            // serialize the SVG
            let img = new Image(w, h);
            let serializer = new XMLSerializer();
            let svgStr = serializer.serializeToString(svg);
            d3.select("#currentSVG").remove();

            // replace the marker strings with their original values
            for (const i in markers) {
                svgStr = svgStr.replaceAll(i, markers[i]);
            }

            // encode SVG string into image
            img.src = "data:image/svg+xml;utf8," + svgStr;

            var canvas = document.createElement("canvas");
            document.body.appendChild(canvas);

            // convert image to canvas and open file save popup on image load
            canvas.width = w;
            canvas.height = h;
            img.onerror = () => alert("Error");
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

                    // save the image blob when the user gives it a name then update the graph
                    this.saveAsBlob(blob, pickerOptions, new Blob([svgStr.replaceAll("%23", "#")]));
                    graph.updateGraph();
                });
            };
        }

        // applies styles to the SVG before it is exported as an image
        applyInlineStyles(oldSVG: SVGSelection, svg: SVGSelection, graph: BaseGraphDisplay) {
            // adds the Symbola font to the SVG for the eight arrow symbols used for velocity (only 8 symbols of the font encoded here)
            svg.append("style").text(`
                @font-face {
                    font-family: Symbola;
                    src: url(data:application/octet-stream;base64,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);
                }
            `);
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
            svg.selectAll(".velocityArrow")
                .attr("style", "fill: black; font-size: 30px; font-family: Symbola !important; fill-opacity: 1;");
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
            let [minX, minY, maxX, maxY] = [Infinity, Infinity, -Infinity, -Infinity];

            for (let e of graph.elements) {
                if (e.x < minX) minX = e.x;
                if (e.y < minY) minY = e.y;
                if (e.x > maxX) maxX = e.x;
                if (e.y > maxY) maxY = e.y;
            }
            let scale = parseFloat(oldSVG.select("g").attr("transform").split(" ")[2].replace("scale(", "").replace(")", ""));
            svg.select("g").attr("transform", "translate(" + ((bounds.width / scale) / 2 + (maxX - minX) / 2 - maxX + this.imageBuffer) + ", "
                + ((bounds.height / scale) / 2 + (maxY - minY) / 2 - maxY + this.imageBuffer) + ") scale(1)");
        }

        // gets the current tab numbers, starting with 1 as the system diagram tab
        getTabNum(): number {
            return parseInt(window.tabNum);
        }

        // alternative saving method that attempts to download an image automatically, workaround for Firefox
        saveFileNoPicker(fileName, blob) {
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

        // hides a menu with a particular menu id
        hideMenu(menuId: string) {
            let el = document.getElementById(menuId);
            if (document.getElementById(menuId)) {
                el = el.parentElement.parentElement;
                if (el.getAttribute("hidden-menu") != "true") {
                    el.setAttribute("hidden-menu", "true");
                }
            }
        }

        // gets the system diagram display object
        getSystemDiagramDisplay() {
            return this.getGraphByIndex("1") as SystemDiagramDisplay;
        }

        // converts all SVG images in the system diagram display into inline form
        // as a note, this will break if additional image types besides SVGs are used for system diagram elements
        async convertImages(query) {
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

        // saves a blob to a file
        async saveAsBlob(blob: any, pickerOptions: any, svgBlob: any) {
            if (window.showSaveFilePicker) {
                const fileHandle = await window.showSaveFilePicker(pickerOptions);
                window.filePath = fileHandle;
                const writableFileStream = await fileHandle.createWritable();
                // if the user chooses to save an image as an SVG, use the SVG blob
                await writableFileStream.write(fileHandle.name.includes(".svg") || fileHandle.name.includes(".svgz") ? svgBlob : blob);
                await writableFileStream.close();
            } else {
                this.saveFileNoPicker(pickerOptions.suggestedName, blob);
            }
        }

        // parse and display unsimplified bond graph
        public displayUnsimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(0, jsonString, window.unsimpBGSVG);
        }

        // parse and display simplified bond graph
        public displaySimplifiedBondGraph(jsonString: string) {
            this.parseAndDisplayBondGraph(1, jsonString, window.simpBGSVG);
        }

        // parse and display causal bond graph
        public displayCausalBondGraphOption(jsonStrings: Array<string>, index: number) {
            this.parseAndDisplayBondGraph(2, jsonStrings[index], window.causalBGSVG);
        }

        // load a system diagram from a JSON string and display it
        public loadSystemDiagram(jsonString: string) {
            let edges = [];
            let parsedJson = JSON.parse(jsonString);
            let elements = this.centerElements(parsedJson.elements, false);

            for (let edge of parsedJson.edges) {
                let bond = new GraphBond(elements.get(edge.source), elements.get(edge.target));
                bond.velocity = edge.velocity ?? 0;
                edges.push(bond);
            }

            window.systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram([], []));

            DotNet.invokeMethodAsync("BoGLWeb", "URAddSelection", Array.from(elements.values()).map(e => JSON.stringify(e)).concat(edges.map(e => JSON.stringify(e))),
                ...window.systemDiagram.listToIDObjects([].concat(window.systemDiagram.selectedElements).concat(window.systemDiagram.selectedBonds)), false);

            let systemDiagram = new SystemDiagramDisplay(window.systemDiagramSVG, new SystemDiagram((Array.from(elements.values()) as SystemDiagramElement[]), edges));
            systemDiagram.draggingElement = null;
            window.systemDiagram = systemDiagram;
            systemDiagram.updateGraph();
            this.zoomCenterGraph("1");
            let bounds = (systemDiagram.svg.select("g").node() as HTMLElement).getBoundingClientRect();
            systemDiagram.initWidth = bounds.width;
            systemDiagram.initHeight = bounds.height;
        }

        // get the current system diagram as a JSON string
        public getSystemDiagram() {
            return JSON.stringify({
                elements: window.systemDiagram.elements,
                bonds: window.systemDiagram.bonds
            });
        }

        // zooms and centers a particular graph by finding the center of the current display and centering the graph there
        // scales the graph to 80% of the screen height or width, whichever is smaller, so that the graph has a margin of empty space around it
        public zoomCenterGraph(index: string) {
            let graph = this.getGraphByIndex(index);
            let prevDisplay = graph.svgG.node().parentElement.parentElement.parentElement.style.display;
            graph.svgG.node().parentElement.parentElement.parentElement.style.display = "block";
            let svgDim = (graph.svgG.node() as SVGSVGElement).getBBox();
            let windowDim = graph.svgG.node().parentElement.getBoundingClientRect();
            let scale = 1;
            // choose which dimension to scale to 80% in
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

        // converts a contentStreamReference to a blob and saves the blob to a file
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

        // converts a contentStreamReference to a blob and saves the blob to a file, using a preselected filename if available
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

        // exports the current tab as an image, copying the SVG before turning it into an image object
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

        // TODO: rename function to be more accurate
        // stringifies the current system diagram and rounds its element x and y positions
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

        // runs the tutorial using Intro.js
        public runTutorial() {
            this.closeMenu("Help");
            window.introJs().setOptions({
                showStepNumbers: false,
                scrollToElement: false,
                steps: [{
                    intro: '<p><b>Welcome To BoGL Web</b></p><p>' +
                        'This application is used to construct system diagrams and generate bond graphs from those diagrams</p>'
                }, {
                    element: document.querySelector('.graphSVG'),
                    intro: '<p><b>The Canvas</b></p><p>The highlighted space is the Canvas where you can construct, move, and rearrange your system diagrams.</p>'
                }, {
                    element: document.querySelector('#graphMenu'),
                    intro: '<p><b>The Element Palette</b></p><p>This is the element palette. After expanding the menus, you can select and drag elements onto the canvas to construct system diagrams</p>'
                }, {
                    intro: '<p><b>Constructing a System Diagram</b></p><div style="display: flex; align-items: center"><p style="margin-right: 10px; margin-bottom: 0px; text-align: right">Select and drag an element to add it to the Canvas, and then select near its black border to start creating an edge. You can then select near a second element to finish making the edge. If you see a green circle, your edge is valid, if you see a red X when you try to make an edge, it means the edge you are trying to make is invalid (the two elements do not make sense to be connected). All elements should have a connection to another element except for the gravity element. </p><img src="images/tutorial/EdgeCreationGif-Edited.gif" width="60%"></div>',
                    tooltipClass: 'wideTooltip'
                },
                {
                    element: document.querySelector('#modifierMenu'),
                    intro: '<p><b>The Modifier Menu</b></p><p>Use this menu to add modifiers to the selected element. Some modifiers require multiple elements to be selected. You can do this by holding down the control key and clicking elements you want to select, or drag the cursor across the canvas with the left mouse button to create a selection region. All elements that are completely or partially inside the region will be selected.</p>',
                    position: 'left'
                }, {
                    element: document.querySelector('#zoomMenu'),
                    intro: '<p><b>The Zoom Menu</b></p><p>This menu allows you to zoom in and out of the canvas. You can use the zoom slider, or your scroll wheel. You can also pan around the canvas by holding right click.' +
                        '<br><br><img src="images/tutorial/ZoomGif-Edited.gif" width="100%">' +
                        '</p>',
                    position: 'top'
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
                        '<li>Generate a URL that that can be used to share your System Diagram</li>' +
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

        // handles undo/redo using the backend
        public async handleUndoRedo(undo: boolean) {
            DotNet.invokeMethodAsync("BoGLWeb", "UndoRedoHandler", parseInt(window.tabNum), undo);
        }

        // undo/redo for adding a selection, where highlight indicates whether the current selection should be modified 
        public urDoAddSelection(newObjects: string[], prevSelElIDs: number[], prevSelectedEdges: string[], highlight: boolean, isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(newObjects);
            if (isUndo) {
                // if undo, get identifiers for all elements/edges and remove those elements/edges from the display object,
                // then restore the selection from before elements/edges were added if highlight is true
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
                // if redo, add the recorded elements/edges and select them if highlight is true
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

        // undo/redo for deleting a selection, recording bonds in unselectedDeletedEdges that are deleted automatically because one of their
        // end elements got deleted, but not re-highlighting these bonds on undo
        public urDoDeleteSelection(deletedObjects: string[], unselectedDeletedEdges: string[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let [elements, bonds] = this.parseElementAndEdgeStrings(deletedObjects);
            let [_, unselectedBonds] = this.parseElementAndEdgeStrings(unselectedDeletedEdges);
            if (isUndo) {
                // if undo, add back the deleted elements/edges and restore highlight for all but the unselected bonds
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
                // if redo, remove the deleted elements from the display object
                let elIDs = elements.map(e => e.id);
                let elBonds = bonds.concat(unselectedBonds).map(b => { return new GraphBondID(b.source.id, b.target.id); });
                sysDiag.elements = sysDiag.elements.filter(e => !elIDs.includes(e.id));
                sysDiag.bonds = sysDiag.bonds.filter(b => !this.checkBondIDs(elBonds, b));
                sysDiag.setSelection([], []);
            }
            sysDiag.updateGraph();
            sysDiag.updateMenus();
        }

        // undo/redo for changing a selection
        public urDoChangeSelection(elIDsToAdd: number[], edgesToAdd: string[], elIDsToRemove: number[], edgesToRemove: string[], isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            let addToSelectionEdges = this.parseEdgeIDStrings(edgesToAdd);
            let removeFromSelectionEdges = this.parseEdgeIDStrings(edgesToRemove);
            // toggles whether the elements/edges are added or removed based on whether we're undoing or redoing
            let elAddSet = isUndo ? elIDsToRemove : elIDsToAdd;
            let elRemoveSet = isUndo ? elIDsToAdd : elIDsToRemove;
            let edgeAddSet = isUndo ? removeFromSelectionEdges : addToSelectionEdges;
            let edgeRemoveSet = isUndo ? addToSelectionEdges : removeFromSelectionEdges;
            diagram.selectedElements = (diagram.selectedElements as GraphElement[]).concat(diagram.elements.filter(e => elAddSet.includes(e.id)));
            diagram.selectedBonds = diagram.selectedBonds.concat(diagram.bonds.filter(b => this.checkBondIDs(edgeAddSet, b)));
            diagram.selectedElements = diagram.selectedElements.filter(e => !elRemoveSet.includes(e.id));
            diagram.selectedBonds = diagram.selectedBonds.filter(b => !this.checkBondIDs(edgeRemoveSet, b));
            diagram.updateGraph();
            diagram.updateMenus();
        }

        // undo/redo for moving a selection
        public urDoMoveSelection(elements: number[], xOffset: number, yOffset: number, isUndo: boolean) {
            let diagram = this.getGraphByIndex(window.tabNum);
            // adds or substracts the recorded offset based on whether we're undoing or redoing
            diagram.elements.filter(e => elements.includes(e.id)).forEach(e => {
                e.x = e.x + (isUndo ? -1 : 1) * xOffset;
                e.y = e.y + (isUndo ? -1 : 1) * yOffset;
            });
            diagram.updateGraph();
        }

        // undo/redo for changing a selection's velocity
        public urDoChangeSelectionVelocity(elIDs: number[], edgeIDs: string[], velID: number, prevVelVals: number[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let bondIDs = this.parseEdgeIDStrings(edgeIDs);
            // set the velocity of edges/elements to either the new velocity or their previous velocity depending on whether we're undoing or redoing
            sysDiag.elements.filter(e => elIDs.includes(e.id)).forEach(e => e.velocity = isUndo ? prevVelVals[elIDs.findIndex(i => i == e.id)] : velID);
            sysDiag.bonds.filter(b => this.checkBondIDs(bondIDs, b)).forEach(b => b.velocity = isUndo ? prevVelVals[elIDs.length
                + this.checkBondIDs(bondIDs, b).velID] : velID);
            sysDiag.updateGraph();
            sysDiag.updateVelocityMenu();
        }

        // undo/redo for changing a selection's modifier
        public urDoChangeSelectionModifier(elIDs: number[], modID: number, modVal: boolean, prevModVals: boolean[], isUndo: boolean) {
            let sysDiag = window.systemDiagram;
            let backend = this;

            elIDs.forEach(function (id, i) {
                let el = sysDiag.elements.find(e => e.id == id);
                if (isUndo) {
                    // if undo, reverse modifier addition or removal in selected elements
                    backend.setModifierNoUR(el, modID, prevModVals[i]);
                } else {
                    // if redo, enact modifier addition or removal in selected elements
                    backend.setModifierNoUR(el, modID, modVal);
                }
            });

            sysDiag.updateGraph();
            sysDiag.updateModifierMenu();
        }

        // copy then delete the current selection
        public cut() {
            this.getSystemDiagramDisplay().copySelection();
            this.getSystemDiagramDisplay().deleteSelection();
        }

        // copy the current selection
        public copy() {
            this.getSystemDiagramDisplay().copySelection();
        }

        // paste the current selection
        public paste() {
            this.getSystemDiagramDisplay().paste();
        }

        // delete the current selection, bringing up the confirmation modal if needsConfirmation is true and multiple
        // elements/edges are being deleted
        public delete(needsConfirmation = true) {
            this.getSystemDiagramDisplay().deleteSelection(needsConfirmation);
        }

        // selects all elements/edges in the canvas and deletes them without confirmation
        public clear() {
            this.getSystemDiagramDisplay().selectAll();
            this.getSystemDiagramDisplay().deleteSelection(false);
        }

        // set a modifier to a value for a given element without doing any undo/redo
        setModifierNoUR(el: SystemDiagramElement, i: number, value: boolean) {
            if (value) { // adding modifier
                if (ElementNamespace.elementTypes[el.type].allowedModifiers.includes(i) && !el.modifiers.includes(i)) {
                    el.modifiers.push(i);
                }
            } else { // removing modifiers
                if (el.modifiers.includes(i)) {
                    el.modifiers.splice(el.modifiers.indexOf(i), 1);
                }
            }
        }

        // sets the modifier for the current selection
        public setModifier(i: number, value: boolean) {
            // save current modifier values
            let prevModVals = window.systemDiagram.selectedElements.map(e => e.modifiers.includes(i));

            // set the modifier for each selected element
            for (const el of window.systemDiagram.selectedElements) {
                this.setModifierNoUR(el, i, value);
            }

            // update the graph and save action for undo/redo
            window.systemDiagram.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionModifier", window.systemDiagram.selectedElements.map(e => e.id), i, value, prevModVals);
            window.systemDiagram.updateModifierMenu();
        }

        // set the velocity of all edges/elements to a given velocity ID
        public setVelocity(velocity: number) {
            let prevVelVals = window.systemDiagram.getSelection().map(e => e.velocity);
            for (const e of window.systemDiagram.getSelection()) {
                // set the velocity if the object is an edge or it is an element that is allowed to have velocity
                if (e instanceof GraphBond || ElementNamespace.elementTypes[e.type].velocityAllowed) {
                    e.velocity = velocity;
                }
            }
            window.systemDiagram.updateGraph();
            DotNet.invokeMethodAsync("BoGLWeb", "URChangeSelectionVelocity", ...window.systemDiagram.listToIDObjects(window.systemDiagram.getSelection()), velocity, prevVelVals);
        }

        // set the zoom of the graph to i (0 to 100), used by the zoom slider
        public setZoom(i: number) {
            let graph = this.getGraphByIndex(window.tabNum);
            let windowDim = graph.svg.node().parentElement.getBoundingClientRect();

            // calculate the x and y offsets needed to keep the window centered on its current center while zooming
            let xOffset = (graph.prevScale * 100 - i) * (graph.svgX - graph.initXPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);
            let yOffset = (graph.prevScale * 100 - i) * (graph.svgY - graph.initYPos) / ((graph.prevScale + (i > graph.prevScale ? 0.01 : -0.01)) * 100);

            // only translate and scale if the zoom value has changed
            if (graph.prevScale * 100 - i != 0) {
                graph.changeScale(windowDim.width / 2 - (windowDim.width / 2 - graph.svgX) - xOffset, windowDim.height / 2 - (windowDim.height / 2 - graph.svgY) - yOffset, i / 100);
            }
        }

        // change the recorded tab number and set the zoom slider to the new graph's zoom value
        public setTab(key: string) {
            window.tabNum = key;
            DotNet.invokeMethodAsync("BoGLWeb", "SetScale", this.getGraphByIndex(key).prevScale);
        }

        // get the graph display object for a given tab ID (1 to 4)
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

        // render equation strings in DOM elements with particular IDs (match string to ID by list index)
        public renderEquations(ids: string[], eqStrings: string[]) {
            for (let i = 0; i < ids.length; i++) {
                let html = katex.renderToString(eqStrings[i], {
                    throwOnError: false
                });
                const parser = new DOMParser();
                let parent = document.getElementById(ids[i]);
                parent.innerHTML = "";
                parent.appendChild(parser.parseFromString(html, "application/xml").children[0].children[0]);
            }
        }

        // copies text to the clipboard
        public textToClipboard(text: string) {
            navigator.clipboard.writeText(text);
        }

        // closes a given menu and all its submenus
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
    }

    // returns a backend manager object
    export function getBackendManager(): BackendManager {
        return new BackendManager();
    }
}
