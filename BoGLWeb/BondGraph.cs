using BoGLWeb.BaseClasses;
using BoGLWeb.EditorHelper;
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

            // Assigns a unique ID to each Element
            private static int universalID = 0;
            private int? ID;

            //For graph visualization
            //TODO Create a way to modify these values

            public Element(string name, string label, double value) {
                this.name = name;
                this.label = label;
                this.value = value;
            }

            /// <summary>
            /// Assigns an ID to this <code>Element</code>.
            /// </summary>
            /// <param name="ID">
            /// A reference ID for this <code>Element</code>.
            /// </param>
            /// <param name="isDistinct">
            /// <code>true</code> if this <code>Element</code> should not be tied 
            /// to any other object in the canvas, else <code>false</code>.
            /// </param>
            public void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null || isDistinct) {
                    this.ID = (universalID++);
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <code>Element</code>.
            /// </summary>
            /// <param name="isDistinct">
            /// <code>true</code> if this <code>Element</code> should not be tied 
            /// to any other object in the canvas, else <code>false</code>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Element Copy(bool isDistinct) {
                Element copy = new(name, label, value);
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Finds the hashing code for this <code>Element</code>
            /// </summary>
            /// <returns>
            /// <code>this.ID</code>
            /// </returns>
            public override int GetHashCode() {
                return this.ID is int ID ? ID : 0;
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

            // Assigns a unique ID to each Element
            private static int universalID = 0;
            private int? ID;

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

            /// <summary>
            /// Assigns an ID to this <code>Bond</code>.
            /// </summary>
            /// <param name="ID">
            /// A reference ID for this <code>Bond</code>.
            /// </param>
            /// <param name="isDistinct">
            /// <code>true</code> if this <code>Bond</code> should not be tied 
            /// to any other object in the canvas, else <code>false</code>.
            /// </param>
            public void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null || isDistinct) {
                    this.ID = (universalID++);
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <code>Bond</code>.
            /// </summary>
            /// <param name="isDistinct">
            /// <code>true</code> if this <code>Bond</code> should not be tied 
            /// to any other object in the canvas, else <code>false</code>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Bond Copy(bool isDistinct) {
                Bond copy = new(this.source, this.sink, this.label, 
                    this.causalStroke, this.causalStrokeDirection, 
                    this.flow, this.effort);
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Finds the hashing code for this <code>Element</code>
            /// </summary>
            /// <returns>
            /// <code>this.ID</code>
            /// </returns>
            public override int GetHashCode() {
                return this.ID is int ID ? ID : 0;
            }
        }
    }
}
