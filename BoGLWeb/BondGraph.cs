using AntDesign;
using BoGLWeb.BaseClasses;
using GraphSynth.Representation;
using Newtonsoft.Json;
/*using Newtonsoft.Json;
*/using Newtonsoft.Json.Linq;
using System.Collections;
using System.Globalization;
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

        public Dictionary<string, Element> getElements() {
            return elements;
        }

        public List<Bond> getBonds() {
            return bonds;
        }

        public string convertToJson() {
            return JsonConvert.SerializeObject(new {
                elements = JsonConvert.SerializeObject(this.elements.Values.ToList()),
                bonds = JsonConvert.SerializeObject(this.bonds)
            });
        }

        //Generate BondGraph from GraphSynth designGraph
        //TODO Check if we can ensure a designGraph is a Bond Graph and produce error if it is not
        public static BondGraph generateBondGraphFromGraphSynth(designGraph graph) {
            if (graph is null) {
                throw new ArgumentException("Graph was null");
            }

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

                var sourceID = bondGraph.elements.ToList().FindIndex(e => e.Value.getName() == to.name);
                var targetID = bondGraph.elements.ToList().FindIndex(e => e.Value.getName() == from.name);
                if (flip) {
                    bondGraph.addBond(new Bond(sourceID, targetID, bondGraph.getElement(to.name), bondGraph.getElement(from.name), "", useCausalStroke, flip, 0, 0));
                } else {
                    bondGraph.addBond(new Bond(targetID, sourceID, bondGraph.getElement(from.name), bondGraph.getElement(to.name), "", useCausalStroke, flip, 0, 0));
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
            [JsonProperty]
            protected double x, y;
            //TODO Create a way to modify these values

            public string getString() {
                return this.label + " " + this.value + " " + this.name + " " + this.x + " " + this.y;
            }

            public Element(string name, string label, double value) {
                this.name = name;
                this.label = label;
                this.value = value;

                Random rnd = new();
                this.x = rnd.Next(2000);
                this.y = rnd.Next(2000);
            }

            public void setPosition(double x, double y) {
                this.x = x;
                this.y = y;
            }

            public double getX() {
                return x;
            }

            public double getY() {
                return y;
            }

            public string getName() {
                return this.name;
            }
        }

        public class Bond {
            [JsonProperty]
            protected readonly int sourceID, targetID;
            protected readonly Element source, sink;
            protected readonly string label;
            protected readonly double flow, effort;

            [JsonProperty]
            protected readonly bool causalStroke;
            //True means the causal stroke is at the source
            [JsonProperty]
            protected readonly bool causalStrokeDirection;

            //The arrow will always point at the sink
            public Bond(int sourceID, int targetID, Element source, Element sink, string label, bool causalStroke, bool causalStrokeDirection, double flow, double effort) {
                this.sourceID = sourceID;
                this.targetID = targetID;
                this.source = source;
                this.sink = sink;
                this.label = label;
                this.causalStroke = causalStroke;
                this.causalStrokeDirection = causalStrokeDirection;
                this.flow = flow;
                this.effort = effort;
            }

            public bool isSource(Element e) {
                return e.Equals(source);
            }

            public bool isSink(Element e) {
                return e.Equals(sink);
            }

            public Element getSource() {
                return source;
            }

            public Element getSink() {
                return sink;
            }
        }
    }
}
