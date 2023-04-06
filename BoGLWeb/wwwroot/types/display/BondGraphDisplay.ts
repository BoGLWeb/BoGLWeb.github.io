import { BGBondSelection, GraphElementSelection, SVGSelection } from "../../type_libraries/d3-selection";
import { BondGraphBond } from "../bonds/BondGraphBond";
import { BondGraphElement } from "../elements/BondGraphElement";
import { GraphElement } from "../elements/GraphElement";
import { BondGraph } from "../graphs/BondGraph";
import { BaseGraphDisplay } from "./BaseGraphDisplay";

export class BondGraphDisplay extends BaseGraphDisplay {
    dragging: boolean = false;
    testSVG: SVGSelection;
    defs: SVGSelection;
    id: number;

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
            .attr("id", e => "BG_test_" + e.id + "_" + this.id)
            .classed("bondGraphText", true);
        let tspan1 = text.append("tspan")
            .text((d) => "I:M")// (<BondGraphElement>d).label)
            .classed("bondGraphText", true);
        let tspan2 = text.append("tspan");
        tspan2.attr("text-anchor", "middle")
            .text((d) => "1")// (<BondGraphElement>d).label)
            .style('font-size', '10px')
            .style('baseline-shift', 'sub')
            .classed("bondGraphText", true);
        newElements.each((d: BondGraphElement) => {
            let testText = this.testSVG.append("text");
            testText.attr("text-anchor", "middle")
                .text(() => "I:M1");// d.label);
            let bb = testText.node().getBBox();
            d.labelSize = { width: bb.width, height: bb.height };
        });
    }

    pathExtraRendering(paths: BGBondSelection, pathGroup: BGBondSelection) {
        paths.style('marker-end', (d: BondGraphBond) => {
            if(d.hasDirection){
                return "url('#" + (d.causalStroke && !d.causalStrokeDirection ? "causal_stroke_and_arrow_" : "arrow_") + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + "')";
            }
        })
            .style('marker-start', (d: BondGraphBond) => {
                if(d.hasDirection){
                    return (d.causalStroke && d.causalStrokeDirection ? "url('#causal_stroke_" + this.id + (this.selectedBonds.includes(d) ? "_selected" : "") + "')" : "");
                }
            })
            .style('stroke-width', 2);
        let buffer = 15;

        // Need offset based on angle of line
        pathGroup.append("text")
            .text("long text long text")
            .attr("x", d => {
                let angle = Math.atan2(d.source.y - d.target.y, d.source.x - d.target.x);
                return (d.source.x + d.target.x) / 2 + Math.sin(angle) * buffer;
            })
            .attr("y", d => {
                let angle = Math.atan2(d.source.y - d.target.y, d.source.x - d.target.x);
                return (d.source.y + d.target.y) / 2 + Math.cos(angle) * buffer;
            });
        pathGroup.append("text")
            .text("sneaky boi sneaky boi")
            .attr("x", d => {
                let angle = Math.atan2(d.source.y - d.target.y, d.source.x - d.target.x);
                return (d.source.x + d.target.x) / 2 - Math.sin(angle) * buffer;
            })
            .attr("y", d => {
                let angle = Math.atan2(d.source.y - d.target.y, d.source.x - d.target.x);
                return (d.source.y + d.target.y) / 2 - Math.cos(angle) * buffer;
            })
            .attr("text-anchor", "end");
    }
}