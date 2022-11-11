using GraphSynth.Representation;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace BoGLWeb {
    public class BondGraph {
        [JsonProperty]
        protected Dictionary<string, Element> elements;
        [JsonProperty]
        protected List<Bond> bonds;

        public BondGraph() {
            elements = new Dictionary<string, Element>();
            bonds = new List<Bond>();
        }

        public void addElement(string name, Element e) {
            elements.Add(name, e);
        }

        public void addBond(Bond bond) {
            bonds.Add(bond);
        }

        public Element getElement(string name) {
            return elements[name];
        }

        //Generate BondGraph from GraphSynth designGraph
        //TODO Check if we can ensure a designGraph is a Bond Graph and produce error if it is not
        public static BondGraph generateBondGraphFromGraphSynth(designGraph graph) {
            BondGraph bondGraph = new BondGraph();

            foreach(var node in graph.nodes) {
                string label = "";
                foreach (string l in node.localLabels) {
                    label += l + " ";
                }
                bondGraph.addElement(node.name, new Element(node.name, label.TrimEnd(), 0));
            }

            foreach (var arc in graph.arcs) {
                var from = arc.From;
                var to = arc.To;
                var labels = arc.localLabels;
                //TODO Check if this string is correct
                bool flip = labels.Contains("OPP");
                //TODO Make sure that this is an okay way to check if we should have a causal stroke
                bool useCausalStroke = labels.Contains("OPP") || labels.Contains("SAME");

                if (flip) {
                    bondGraph.addBond(new Bond(bondGraph.getElement(to.name), bondGraph.getElement(from.name), "", useCausalStroke, flip, 0, 0));
                } else {
                    bondGraph.addBond(new Bond(bondGraph.getElement(from.name), bondGraph.getElement(to.name), "", useCausalStroke, flip, 0, 0));
                }

            }

            return bondGraph;
        }

        public class Element {
            [JsonProperty]
            protected readonly string label;
            [JsonProperty]
            protected readonly double value;
            protected readonly string name;

            //For graph visualization
            //TODO Create a way to modify these values

            public Element(string name, string label, double value) {
                this.name = name;
                this.label = label;
                this.value = value;
            }
        }

        public class Bond {
            [JsonProperty]
            protected readonly Element source, sink;
            [JsonProperty]
            protected readonly string label;
            [JsonProperty]
            protected readonly double flow, effort;

            private readonly bool causalStroke;
            //True means the causal stroke is at the source
            private readonly bool causalStrokeDirection;

            //The arrow will always point at the sink
            public Bond(Element source, Element sink, string label, bool causalStroke, bool causalStrokeDirection, double flow, double effort) {
                this.source = source;
                this.sink = sink;
                this.label = label;
                this.causalStroke = causalStroke;
                this.causalStrokeDirection = causalStrokeDirection;
                this.flow = flow;
                this.effort = effort;
            }
        }
    }
}
