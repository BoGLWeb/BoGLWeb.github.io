import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { BondGraphBond } from "../bonds/BondGraphBond";
import { GraphBond } from "../bonds/GraphBond";
import { BondGraphElement } from "../elements/BondGraphElement";
import { GraphElement } from "../elements/GraphElement";
import { BondGraph } from "../graphs/BondGraph";
import { BaseGraphDisplay } from "./BaseGraphDisplay";

export class BondGraphDisplay extends BaseGraphDisplay {
    dragging: boolean = false;
    testSVG: SVGSelection;
    defs: SVGSelection;
    id: number;
    buffer: number = 15;

    constructor(id: number, svg: SVGSelection, bondGraph: BondGraph) {
        super(svg, bondGraph);

        this.id = id;
        this.testSVG = d3.select("#app").append("svg");
        this.testSVG.style("position", "absolute")
            .style("left", "-10000000px")
            .style("top", "-10000000px");

        // define arrow markers for graph links
        this.defs = this.svgG.append("svg:defs");

        this.makeBaseMarker("causal_stroke_" + id, 1, 5, 10, 10, false)
            .append("path")
            .attr("d", "M1,10L1,-10");

        this.makeBaseMarker("causal_stroke_" + id + "_selected", 1, 5, 10, 10, true)
            .append("path")
            .attr("d", "M1,10L1,-10");

        this.makeBaseMarker("arrow_" + id, 10, 0, 10, 10, false)
            .append("path")
            .attr("d", "M10,0L2,5");

        this.makeBaseMarker("arrow_" + id + "_selected", 10, 0, 10, 10, true)
            .append("path")
            .attr("d", "M10,0L2,5");

        let arrowAndFlat = this.makeBaseMarker("causal_stroke_and_arrow_" + id, 10, 10, 20, 20, false);
        arrowAndFlat.append("path")
            .attr("d", "M10,10L2,15");
        arrowAndFlat.append("path")
            .attr("d", "M10,5L10,15");

        arrowAndFlat = this.makeBaseMarker("causal_stroke_and_arrow_" + id + "_selected", 10, 10, 20, 20, true);
        arrowAndFlat.append("path")
            .attr("d", "M10,10L2,15");
        arrowAndFlat.append("path")
            .attr("d", "M10,5L10,15");

        this.bonds.forEach((b: BondGraphBond) => {
            console.log(b.effortLabel, b.flowLabel);
            let testText = this.testSVG.append("text");
            testText.attr("text-anchor", "middle")
                .classed("bondGraphText", true);
            let l1 = testText.append("tspan")
                .text(b.effortLabel);
            let l2 = testText.append("tspan")
                .text(b.id)
                .style('font-size', '10px')
                .style('baseline-shift', 'sub');

            let bb = testText.node().getBBox();
            b.effortLabelAngle = (Math.PI / 2) - Math.acos(this.buffer / Math.sqrt(Math.pow(bb.width, 2) + Math.pow(bb.height, 2)));

            l1.text(b.flowLabel);
            l2.text(b.id);

            bb = testText.node().getBBox();
            b.flowLabelAngle = (Math.PI / 2) - Math.acos(this.buffer / Math.sqrt(Math.pow(bb.width, 2) + Math.pow(bb.height, 2)));
            testText.remove();
        });

        console.log(this.bonds.map((b: BondGraphBond) => b.effortLabelAngle));
        console.log(this.bonds.map((b: BondGraphBond) => b.flowLabelAngle));
    }

    makeBaseMarker(id: string, refX, refY, w, h, isSelected) {
        let marker = this.defs.append("svg:marker");
        marker.attr("id", id)
            .attr("refX", refX)
            .attr("refY", refY)
            .attr("markerWidth", w)
            .attr("markerHeight", h)
            .attr("orient", "auto")
            .style("stroke", isSelected ? "rgb(6, 82, 255)" : "#333");
        return marker;
    }

    // draw paths second to get the sizes of labels
    updateGraph(dragmove: boolean = false) {
        this.fullRenderElements(dragmove);
        this.drawPaths();
    }

    getEdgePosition(source: GraphElement, target: GraphElement) {
        let sourceEl = source as BondGraphElement;
        let targetEl = target as BondGraphElement;
        let margin = 10;
        let width = sourceEl.labelSize.width / 2 + margin;
        let height = sourceEl.labelSize.height / 2 + margin;
        let x = targetEl.x - sourceEl.x;
        let y = targetEl.y - sourceEl.y;
        let theta = (Math.atan2(x, y) + (3 * Math.PI / 2)) % (2 * Math.PI);
        let thetaUR = Math.atan2(height, width);
        let thetaUL = Math.PI - thetaUR;
        let thetaLL = Math.PI + thetaUR;
        let thetaLR = 2 * Math.PI - thetaUR;
        let coords = [];
        // quads 1, 2, 3, and 4
        if ((theta >= 0 && theta < thetaUR) || (theta >= thetaLR && theta < 2 * Math.PI)) {
            coords = [width, -width * Math.tan(theta)]
        } else if (theta >= thetaUR && theta < thetaUL) {
            coords = [height * 1 / Math.tan(theta), -height]
        } else if (theta >= thetaUL && theta < thetaLL) {
            coords = [-width, width * Math.tan(theta)]
        } else {
            coords = [-height * 1 / Math.tan(theta), height]
        }
        return coords;
    }

    renderElements(newElements: GraphElementSelection) {
        let graph = this;
        newElements.classed("boglElem", true)
            .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
            .on("mouseup", function (d) {
                graph.nodeMouseUp.call(graph, d);
            })
            .call(this.drag);

        let text = newElements.append("text");
        text.attr("text-anchor", "middle")
            .classed("bondGraphText", true);
        text.append("tspan")
            .text((d: BondGraphElement) => d.label);
        text.append("tspan")
            .text((d: BondGraphElement) => ["0", "1"].indexOf(d.label) == -1 ? d.backendId : "")
            .style('font-size', '10px')
            .style('baseline-shift', 'sub');
        newElements.each((d: BondGraphElement) => {
            let testText = this.testSVG.append("text");
            testText.attr("text-anchor", "middle")
                .classed("bondGraphText", true);
            testText.append("tspan")
                .text(d.label);
            testText.append("tspan")
                .text(["0", "1"].indexOf(d.label) == -1 ? d.backendId : "")
                .style('font-size', '10px')
                .style('baseline-shift', 'sub');

            let bb = testText.node().getBBox();
            d.labelSize = { width: bb.width, height: bb.height };
        });
    }

    getAngle(d: GraphBond) {
        return Math.atan2(d.source.y - d.target.y, d.source.x - d.target.x);
    }

    getNormAngle(d: GraphBond) {
        return (this.getAngle(d) + (2 * Math.PI)) % (2 * Math.PI);
    }

    isEffortLabel(d, label1) {
        return (this.getNormAngle(d) > (Math.PI / 4) && this.getNormAngle(d) < (5 * Math.PI / 4)) ? label1 : !label1;
    }

    getTextAnchor(d, label1) {
        let absAngle = Math.abs(this.getAngle(d));
        let threshAngle = this.isEffortLabel(d, label1) ? d.effortLabelAngle : d.flowLabelAngle;
        if (absAngle < threshAngle || absAngle > (Math.PI - threshAngle)) {
            return "middle";
        }
        return (label1 && this.getAngle(d) > 0) || (!label1 && this.getAngle(d) < 0) ? "end" : "start";
    }

    pathExtraRendering(paths: BGBondSelection, pathGroup: BGBondSelection) {
        paths.style('marker-end', (d: BondGraphBond) => {
            if(d.hasDirection){
                return "url('#" + (d.causalStroke && !d.causalStrokeDirection ? "causal_stroke_and_arrow_" : "arrow_") + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + "')";
            }
        })
            .style('marker-start', (d: BondGraphBond) => {
                if(d.hasDirection) {
                    return (d.causalStroke && d.causalStrokeDirection ? "url('#causal_stroke_" + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + "')" : "");
                }
            })
            .style('stroke-width', 2);
        pathGroup.selectAll("circle").remove();

        if (this.id == 2) {
            let label1 = pathGroup.append("text")
                .attr("x", d => {
                    return (d.source.x + d.target.x) / 2 - Math.sin(this.getAngle(d)) * this.buffer;
                })
                .attr("y", d => {
                    return (d.source.y + d.target.y) / 2 + Math.cos(this.getAngle(d)) * this.buffer;
                })
                .style("text-anchor", (d: BondGraphBond) => this.getTextAnchor(d, true))
                .style("fill", d => this.selectedBonds.includes(d) ? "rgb(6, 82, 255)" : "#333");
            label1.append("tspan")
                .text((d: BondGraphBond) => this.isEffortLabel(d, true) ? d.effortLabel : d.flowLabel)
                .classed("bondGraphText", true);
            label1.append("tspan")
                .attr("text-anchor", "middle")
                .text((d: BondGraphBond) => d.id)
                .style('font-size', '10px')
                .style('baseline-shift', 'sub');
            let label2 = pathGroup.append("text")
                .attr("x", d => {
                    return (d.source.x + d.target.x) / 2 + Math.sin(this.getAngle(d)) * this.buffer;
                })
                .attr("y", d => {
                    return (d.source.y + d.target.y) / 2 - Math.cos(this.getAngle(d)) * this.buffer;
                })
                .style("text-anchor", (d: BondGraphBond) => this.getTextAnchor(d, false))
                .style("fill", d => this.selectedBonds.includes(d) ? "rgb(6, 82, 255)" : "#333");
            label2.append("tspan")
                .text((d: BondGraphBond) => this.isEffortLabel(d, false) ? d.effortLabel : d.flowLabel)
                .classed("bondGraphText", true);
            label2.append("tspan")
                .attr("text-anchor", "middle")
                .text((d: BondGraphBond) => d.id)
                .style('font-size', '10px')
                .style('baseline-shift', 'sub');
        }
    }
}