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
using System.Text;

namespace BoGLWeb {

    public class BondGraph {
        //Class variables
        [JsonProperty]
        protected Dictionary<string, Element> elements;
        [JsonProperty]
        protected List<Bond> bonds;

        /// <summary>
        /// Creates an instance of BondGraph
        /// </summary>
        public BondGraph() {
            elements = new Dictionary<string, Element>();
            bonds = new List<Bond>();
        }

        /// <summary>
        /// Adds an elements to the BondGraph
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <param name="e">The instance of the element</param>
        public void addElement(string name, Element e) {
            elements.Add(name, e);
        }

        /// <summary>
        /// Adds a bond between two elements
        /// </summary>
        /// <param name="bond">The bond to add</param>
        public void addBond(Bond bond) {
            bonds.Add(bond);
        }

        /// <summary>
        /// Returns an elements with a given name
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <returns>The element with the input name</returns>
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

        /// <summary>
        /// Returns the Bond Graph that can be constructed from a designGraph
        /// </summary>
        /// <param name="graph">The designGraph to construct the BondGraph from</param>
        /// <returns>A BondGraph representation of the designGraph</returns>
        /// <exception cref="ArgumentException">The input designGraph was null</exception>
        //TODO Check if we can ensure a designGraph is a Bond Graph and produce error if it is not
        public static BondGraph generateBondGraphFromGraphSynth(designGraph graph) {
            if (graph is null) {
                throw new ArgumentException("Graph was null");
            }

            BondGraph bondGraph = new BondGraph();

            //Construct an Element for each node
            foreach(var node in graph.nodes) {
                StringBuilder sb = new();
                foreach (string l in node.localLabels) {
                    sb.Append(l);
                    sb.Append(" ");
                }
                bondGraph.addElement(node.name, new Element(node.name, sb.ToString().TrimEnd(), 0));
            }

            //Construct each arc
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

            public string getString() {
                return this.label + " " + this.value + " " + this.name + " " + this.x + " " + this.y;
            }

            /// <summary>
            /// Creates an element of a bond graph
            /// </summary>
            /// <param name="name">The name of the element</param>
            /// <param name="label">Labels associated with the element</param>
            /// <param name="value">A value associated with the element</param>

            public Element(string name, string label, double value) {
                this.name = name;
                this.label = label + " " + name;
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

            public override bool Equals(object? obj) {
                return obj is Element element &&
                       this.name.Equals(element.name);
            }

            public override int GetHashCode() {
                return HashCode.Combine(this.name);
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
            /// <summary>
            /// Creates a Bond between two elements
            /// </summary>
            /// <param name="source">The source element</param>
            /// <param name="sink">The sink element</param>
            /// <param name="label">Labels for the bond</param>
            /// <param name="causalStroke">True if there should be a causal strong, false otherwise</param>
            /// <param name="causalStrokeDirection">The position of the causal stroke. True means the causal stroke is at the source. False means the causal stroke is at the sink.</param>
            /// <param name="flow">The flow value for the bond</param>
            /// <param name="effort">The effor value for the bond</param>
            
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

            public override bool Equals(object? obj) {
                return obj is Bond bond &&
                       this.sourceID.Equals(bond.sourceID) &&
                       this.targetID.Equals(bond.targetID) &&
                       this.causalStroke.Equals(bond.causalStroke) &&
                       this.causalStrokeDirection.Equals(bond.causalStrokeDirection);
            }

            public override int GetHashCode() {
                return HashCode.Combine(this.sourceID, this.targetID, this.causalStroke, this.causalStrokeDirection);
            }
        }
    }
}
