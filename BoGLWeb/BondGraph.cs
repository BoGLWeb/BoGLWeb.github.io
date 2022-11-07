using GraphSynth.Representation;
using Newtonsoft.Json;

namespace BoGLWeb {
    public class BondGraph {
        [JsonProperty]
        protected List<Element> elements;
        [JsonProperty]
        protected List<Bond> bonds;

        public BondGraph() {
            elements = new List<Element>();
            bonds = new List<Bond>();
        }

        public void addElement(Element e) {
            elements.Add(e);
        }

        public void addBond(Bond bond) {
            bonds.Add(bond);
        }

        //Generate BondGraph from GraphSynth designGraph
        //TODO Check if we can ensure a designGraph is a Bond Graph and produce error if it is not
        public static BondGraph generateBondGraphFromGraphSynth(designGraph graph) {
            return null;
        }

        public class Element {
            [JsonProperty]
            protected readonly string label;
            [JsonProperty]
            protected readonly double value;

            //For graph visualization
            //TODO Create a way to modify these values

            private Element(string label, double value) {
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

            //True means the causal stroke is at the source
            private readonly bool causalStroke;

            //The arrow will always point at the sink
            private Bond(Element source, Element sink, string label, bool causalStroke, double flow, double effort) {
                this.source = source;
                this.sink = sink;
                this.label = label;
                this.causalStroke = causalStroke;
                this.flow = flow;
                this.effort = effort;
            }
        }
    }
}
