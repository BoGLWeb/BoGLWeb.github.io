using GraphSynth.Representation;

namespace BoGLWeb {
    public class BondGraph {
        public List<Element> elements;
        public List<Bond> bonds;

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
            private readonly string label;

            //For graph visualization
            //TODO Create a way to modify these values

            public Element(string label) {
                this.label = label;
            }
        }

        public class Bond {
            private Element source, sink;
            private readonly string label;

            //True means that the arrow is pointing towards the sink
            private readonly bool direction;

            //True means the causal stroke is at the source
            private readonly bool causalStroke;

            private Bond(Element source, Element sink, string label, bool direction, bool causalStroke) {
                this.source = source;
                this.sink = sink;
                this.label = label;
                this.direction = direction;
                this.causalStroke = causalStroke;
            }
        }
    }
}
