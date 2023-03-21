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

        imageBuffer = 15;

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

        public svgToCanvas(oldSVG: SVGSelection, svg: SVGElement, graph: BaseGraphDisplay) {
            let scale = parseFloat(oldSVG.select("g").attr("transform").split(" ")[2].replace("scale(", "").replace(")", ""));
            let bounds = (oldSVG.select("g").node() as HTMLElement).getBoundingClientRect();
            let isBondGraph = graph instanceof BondGraphDisplay;
            let w = bounds.width / scale + (isBondGraph ? this.imageBuffer * 2 : 0);
            let h = bounds.height / scale + (isBondGraph ? this.imageBuffer * 2 : 0);

            let img = new Image(w, h);
            let serializer = new XMLSerializer();
            let svgStr = serializer.serializeToString(svg);

            img.src = "data:image/svg+xml;utf8," + svgStr;

            var canvas = document.createElement("canvas");
            document.body.appendChild(canvas);

            canvas.width = w;
            canvas.height = h;
            img.onload = () => {
                canvas.getContext("2d").drawImage(img, 0, 0, w, h);
                canvas.toBlob(blob => {
                    let pickerOptions = {
                        suggestedName: "systemDiagram.png",
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
                    this.saveAsBlob(blob, pickerOptions, new Blob([svgStr]));
                });
            };
        }

        public applyInlineStyles(oldSVG: SVGSelection, svg: SVGSelection, graph: BaseGraphDisplay) {
            svg.selectAll(".link")
                .style("fill", "none")
                .style("stroke", "black")
                .style("stroke-width", "4px");
            svg.selectAll(".boglElem")
                .style("fill", "transparent");
            svg.selectAll(".outline")
                .style("stroke", "black");
            svg.selectAll("text")
                .style("fill", "black")
                .style("font-size", "30px")
                .style("dominant-baseline", "middle")
                .style("font-family", "Arial");
            svg.style("background-color", "white");
            svg.select("circle")
                .style("display", "none");
            svg.selectAll(".bondGraphText")
                .style("font-size", "14px");
            svg.append("style").text("@font-face { font-family: 'Segoe UI'; src: url(data: application / font - woff; base64, d09GRgABAAAAAE3AABIAAAAAg4wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABGRlRNAABNpAAAABwAAAAcRKTk3kdERUYAAEWAAAAAIwAAACYAKQDtR1BPUwAARiQAAAd + AAAOxBNvfaBHU1VCAABFpAAAAH0AAADKeF9YL09TLzIAAAIIAAAAYAAAAGCQx1xVY21hcAAABFwAAAEqAAABcn4N0CNjdnQgAAAMrAAAAJkAAAEs0pcs3mZwZ20AAAWIAAAGYgAADW1NJI58Z2FzcAAARXgAAAAIAAAACAAAABBnbHlmAAAO2AAAMNAAAFAMwZp1xGhlYWQAAAGUAAAANAAAADbWiq4SaGhlYQAAAcgAAAAfAAAAJA8 / BkpobXR4AAACaAAAAfMAAAMcOUhRtWxvY2EAAA1IAAABkAAAAZCxzsdcbWF4cAAAAegAAAAgAAAAIAM6Ag9uYW1lAAA / qAAABFUAAAoGlgWVTnBvc3QAAEQAAAABdgAAAf3RjPSAcHJlcAAAC + wAAAC9AAAA1YGEyCB4AWNgZGAA4dfrtJfG89t8ZZDnYACB9V9t1oPoAyfU7v63 + ifHnsCeDeRyMDCBRAFiSwyEeAFjYGRg4Fj09x8DA / uS / 1b / H7AnAEVQwXEArfcHmQAAAQAAAMcAaAAFADgABAACAIgAmgCLAAABVADSAAIAAQADBDUBkAAFAAgFmgUzAAABGwWaBTMAAAPRAGYCEggFAgsFAgQCBAICA + AAIv / AACBbAAAACQAAAABNUyAgAEAAIALcBdP + UQKkCKICAiAAAd8gCAAABAAFmgAAACAADngBbZExaBNRGMd / 9967uyKlSBCRKkooUopDDKWLg5RSnERCkCIhhAyKpRSRLHJICaFDF8EMpZNkKOEoIYiUUorc6JBOIiIODoKUUqRIBYcihfh9oYFYOvz4v3vvfffg / zOHzAKYlpAV7rFlx1h170m7HOvBK3LBU154X1m1c9z3Iw68hHE7SV3yub3e3ZN7c8KGUBaWhRGhKTw7PXsslPS + MO5H3kX9j6ZdYzls0vYnuOzWSNwfSv4byWESe0DiL8n3WxKT5rb5xiXXlP0PJGFGzk5IgiFK7uVp7srcNJHLMuFXabljUmHEqNsRFkm5OmmzQMcOdfclp / yIfZsHc5O8S1FwW8R2k7LbFjKUTY2p3nqF2Dui7h1179i / uiYO9oh1372jrHN6z3Rk / gnzZptbcla3XxjxO1yxbUbtd0ZsQ9 + n6P2kLTkj73 / qdy797AgLrqJdMat3tBubYym8wbo4yUsnRe1Nu + /t5cE6FnXP/GJeqJkZVlyD2LZ42Ou7yaT5SNVWeCDzhXCaalgRGtTk / 6 + 19 / MIfnBBXaiHQcTD1Z6LdPdE + C2urvU9nEX8Dmuqi0HUhTpzBWLt / TyCDQo9F5n / EQeH0v8jyc / Croso9z2cRXuRzKqLQdSFOlOP5piiuUvebDKmPfV7lHXmHyRqv8wAeAFjYGBghmIZBkYGEMgB8hjBfBaGACAtAIQgeXWGOoZ / jIZMx5huMd35 / x8oosCgxbAAIfL / 8f + H / w / 87 / v76 + /zv8+AZmAARjYghrGZgAQTugKgU1hY2dg5OLkYGLh5ePn4BQSFhEVExcQlJKWkZWTl5BUUlZRVVNXUNTS1tHV09fQNDI2MTUzNzC0sraxtbO3sHRydnF1c3dw9PL28fXz9/AMCg4JDQsPCIyKjomNi4 + IZWlrbOydOm7NwwaIli5cuX7li1eq1a9at37h505ZtW3ft3L2HoSApOZ2hbH5eJkNJBkPbDIZCBobUUrDrsqoYlu2oT8wBsbOrGRIamqfCHL4d4YdyIG7qauzu6O3r75k8hWHSrNkz9x84lM / AcLACKAUAbFJbOgAAeAGtVvdz28gVXrCpF6tdckiUxa2oKMKCp1xsHX3H08mAQMZmCtUSwI4TQKSUXuw0p / di / jUPVDLj / JY / Ld / DDhlJkZQZz3ksvG / ffm9fxYIktCRxHIWxlO1XYna / TZXDxxHdtWkjTs5k / ziiQjX917gYF92uOrEdh0RMIlB7A2GJIPE9sjTJ5MyjgpY9Sf / uUGn98WDDmgrCbkiVMHKoWI0PnkSOcux + JKnTgWo3tiXVGdXjWGaGnfZoA6rMrCRt8f4WmDg1koimn0qa7EQJNJL3JhltM9pO7CSOYxvR0mTQJXEQkWgz2cHabtMqo9V2 + mpedJnxqixO4riXxmS5caxIdKLTOPaoqCU8l6opcikHnYjKyqeK8pE5qIlHJa2Qiexl5RNf8g7naJuY + UljSdil4qaDzUD2ZR8Osq1yFWXZj5KOnR7EkYqdWNLuYYQ9m4th / MN9WdNY4A5EwdS2gqXylSSh / JQKJ2dkdREFlTc9GtOSQ51GLiVxIvkE2k1ipiR7eajjejA2LYLQ33RG3ZrQl7s3aU6xXAU98k5k2FcpOmkqLGzuAkmbdkdVQj9VumdcTN1gTmuwErC6zmha5wkNpiaLGA9bOfGm49GMzgqFkHrpnkezGkQpaSZ4xOYAyo9pllcHWM1i5dGcljSfl0SiAl34pbkgkf1E0hyK5tG8bh9FWam3F6 / RzKl64dEd3d6P2odGaTvQL + b6BZ2J + eA4yubnA7JSn + ZcnnJMk5 / N8GMWD7JWlEQenSjj4iFbv9 + X7HZ201EwG2Lb7MMEf7kmRiYtxN + C9nKrbmhgJsSiQrUCEjsDy7LyXi1qkYlCeBTRvPJlSNPKpymFgfNlAvf / XFiwxJzw / X6SLVRceunab6FMS8ht0fVoWWcWyxXUmeUbOiuy / JjOSiw / rrMyyzd1VmFp62yM5Sd0Ns7ykzqbYPkZrYZ1p0qCCitZI + spvyAebV7YXBltPjOb7oXN9dHmc7O5qgXNuK + R36eQ3yriksiPpYP8WL6F / Fgq5MdyDfmxrCI / luvIj + WnkR / LDeTHUmvZyMfU03C7kMhAYRlwKyFJ86zWNHkueXgL39ZStuQNXVRpXfEdeivD5uy3hq3NpmdCnjR6ezMrW8thhPuPs / ysKc + tnHe0vJdH / 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);} ");
            oldSVG.select(".dragline").remove();
            if ((oldSVG.select("#bondGroup").node() as HTMLElement).children.length == 0) {
                oldSVG.select("#bondGroup").remove();
            }
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
/*            callback();*/
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
            let fileHandle;
            [fileHandle] = await window.showOpenFilePicker();
            const file = await fileHandle.getFile();
            const contents = await file.text();
            return contents;
        }

        public getTabNum(): number {
            return parseInt(window.tabNum);
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
            const fileHandle = await window.showSaveFilePicker(pickerOptions);
            window.filePath = fileHandle;
            const writableFileStream = await fileHandle.createWritable();
            await writableFileStream.write(fileHandle.name.includes(".svg") || fileHandle.name.includes(".svgz") ? svgBlob : blob);
            await writableFileStream.close();
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
                window.filePath = await window.showSaveFilePicker(pickerOptions);
            }

            const writableFileStream = await window.filePath.createWritable();
            await writableFileStream.write(blob);
            await writableFileStream.close();
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