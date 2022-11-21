using GraphSynth.Representation;
using Newtonsoft.Json;
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

            //TODO Add positions for graph visualization
            //TODO Create a way to modify these values

            /// <summary>
            /// Creates an element of a bond graph
            /// </summary>
            /// <param name="name">The name of the element</param>
            /// <param name="label">Labels associated with the element</param>
            /// <param name="value">A value associated with the element</param>
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
            [JsonProperty]
            private readonly bool causalStroke;
            //True means the causal stroke is at the source
            [JsonProperty]
            private readonly bool causalStrokeDirection;

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
