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

    constructor(id: number, svg: SVGSelection, bondGraph: BondGraph) {
        super(svg, bondGraph);

        this.id = id;
        this.testSVG = d3.select("#app").append("svg");
        this.testSVG.style("position", "absolute")
            .style("left", "-10000000px")
            .style("top", "-10000000px");

        // define arrow markers for graph links
        this.defs = svg.append("svg:defs");

        this.makeBaseMarker("causal_stroke_" + id, 1, 5, 10, 10)
            .append("path")
            .attr("d", "M1,10L1,-10");

        this.makeBaseMarker("arrow_" + id, 10, 0, 10, 10)
            .append("path")
            .attr("d", "M10,0L2,5");

        let arrowAndFlat = this.makeBaseMarker("causal_stroke_and_arrow_" + id, 10, 10, 20, 20);
        arrowAndFlat.append("path")
            .attr("d", "M10,10L2,15");
        arrowAndFlat.append("path")
            .attr("d", "M10,5L10,15");
    }

    makeBaseMarker(id: string, refX, refY, w, h) {
        let marker = this.defs.append("svg:marker");
        marker.attr("id", id)
            .attr("refX", refX)
            .attr("refY", refY)
            .attr("markerWidth", w)
            .attr("markerHeight", h)
            .attr("orient", "auto-start-reverse")
            .style("stroke", "#333");
        return marker;
    }

    // draw paths second to get the sizes of labels
    updateGraph() {
        this.fullRenderElements();
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

    drawPath(d: GraphBond) {
        let sourceEnd = this.getEdgePosition(d.source, d.target);
        let targetEnd = this.getEdgePosition(d.target, d.source);
        return "M" + (d.source.x + sourceEnd[0]) + "," + (d.source.y + sourceEnd[1]) + "L" + (d.target.x + targetEnd[0]) + "," + (d.target.y + targetEnd[1]);
    }

    renderElements(newElements: GraphElementSelection) {
        let graph = this;
        newElements.classed("boglElem", true)
            .on("mousedown", function (d) {
                graph.nodeMouseDown.call(graph, d);
            })
            .call(this.drag);

        let text = newElements.append("text");
        text.attr("text-anchor", "middle")
            .text((d) => (<BondGraphElement>d).label)
            .each((d: BondGraphElement) => {
                let testText = this.testSVG.append("text");
                testText.attr("text-anchor", "middle")
                    .text(() => d.label);
                let bb = testText.node().getBBox();
                d.labelSize = { width: bb.width, height: bb.height };
            });
    }

    pathExtraRendering(paths: BGBondSelection) {
        paths.style('marker-end', (d: BondGraphBond) => 'url(#' + (d.causalStroke && !d.causalStrokeDirection ? "causal_stroke_and_arrow_" : "arrow_") + this.id + ')')
            .style('marker-start', (d: BondGraphBond) => (d.causalStroke && d.causalStrokeDirection ? 'url(#causal_stroke_' + this.id + ')' : ""))
            .style('stroke-width', 2);
    }
}