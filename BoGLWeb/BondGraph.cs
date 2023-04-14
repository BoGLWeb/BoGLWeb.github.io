using BoGLWeb.BaseClasses;
using BoGLWeb.EditorHelper;
using GraphSynth.Representation;
using Newtonsoft.Json;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;

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
            this.elements = new Dictionary<string, Element>();
            this.bonds = new List<Bond>();
        }

        /// <summary>
        /// Copies a separate object into this <c>BondGraph</c>.
        /// </summary>
        /// <param name="model">The model <c>BondGraph</c>.</param>
        public void CopyFromModel(BondGraph model) {
            this.elements = new(model.elements);
            this.bonds = new(model.bonds);
        }

        /// <summary>
        /// Adds an elements to the BondGraph
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <param name="e">The instance of the element</param>
        public void addElement(string name, Element e) {
            this.elements.Add(name, e);
        }

        /// <summary>
        /// Adds a bond between two elements
        /// </summary>
        /// <param name="bond">The bond to add</param>
        public void addBond(Bond bond) {
            this.bonds.Add(bond);
        }

        /// <summary>
        /// Returns an elements with a given name
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <returns>The element with the input name</returns>
        public Element getElement(string name) {
            return this.elements[name];
        }

        /// <summary>
        /// Returns a dictionary with the names of elements as Keys and Elements as values
        /// </summary>
        /// <returns>A Dictionary</returns>
        public Dictionary<string, Element> getElements() {
            return this.elements;
        }

        /// <summary>
        /// Returns a list of the bonds in the bond graph
        /// </summary>
        /// <returns>A List</returns>
        public List<Bond> getBonds() {
            return this.bonds;
        }

        /// <summary>
        /// Converts a Bond Graph to a Json string
        /// </summary>
        /// <returns>A string representation of a bond graph</returns>
        public string convertToJson() {
            return JsonConvert.SerializeObject(new {
                elements = JsonConvert.SerializeObject(this.elements.Values.ToList()),
                bonds = JsonConvert.SerializeObject(this.bonds)
            });
        }

        public static IDictionary<string, string> bondGraphLabels = new Dictionary<string, string>() {
            { "_Mass", "I:m" },
            { "_Spring", "C:K" },
            { "_Stiffness", "C:K" },
            { "_Torque_Input", "Se:τ" },
            { "_Damper", "R:b" },
            { "_Resistor", "R:R" },
            { "_Force", "Se:F" },
            { "_Capacitor", "C:C" },
            { "_Velocity", "Sf:v" },
            { "_Inductor", "I:L" },
            { "_Rack&Pinion TF", "TF:K" },
            { "_Flywheel", "I:J" },
            { "_Voltage", "Se:V" },
            { "_Current", "Sf:i" },
            { "gear", "TF:K" },
            { "_Lever", "TF:K" },
            { "Motor", "GY:K" }
        };

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

            BondGraph bondGraph = new();

            //Construct an Element for each node
            foreach(node node in graph.nodes) {
                StringBuilder sb = new();
                string label = "";
                foreach (string l in node.localLabels) {
                    label += l + " ";
                    string strippedLabel = l.Replace("_Added", "");
                    if (bondGraphLabels.ContainsKey(strippedLabel)) {
                        bondGraphLabels.TryGetValue(strippedLabel, out label);
                        break;
                    } else if (l == "0" || l == "1") {
                        label = l;
                        break;
                    }
                }

                bondGraph.addElement(node.name, new Element(node.name, label, 0));
            }

            //Construct each arc
            foreach (arc arc in graph.arcs) {
                node from = arc.From;
                node to = arc.To;
                List<string> labels = arc.localLabels;
                bool flip = labels.Contains("OPP");
                bool useCausalStroke = labels.Contains("OPP") || labels.Contains("SAME");

                int sourceId = bondGraph.elements.ToList().FindIndex(e => e.Value.getName() == to.name);
                int targetId = bondGraph.elements.ToList().FindIndex(e => e.Value.getName() == from.name);
                if (arc.localLabels.Contains("dir")) {
                    bondGraph.addBond(flip
                    ? new Bond(sourceId, targetId, bondGraph.getElement(to.name), bondGraph.getElement(from.name), "",
                    useCausalStroke, flip, true, 0, 0)
                    : new Bond(targetId, sourceId, bondGraph.getElement(from.name), bondGraph.getElement(to.name), "",
                    useCausalStroke, flip, true, 0, 0));    
                } else {
                    bondGraph.addBond(flip
                    ? new Bond(sourceId, targetId, bondGraph.getElement(to.name), bondGraph.getElement(from.name), "",
                    useCausalStroke, flip, false, 0, 0)
                    : new Bond(targetId, sourceId, bondGraph.getElement(from.name), bondGraph.getElement(to.name), "",
                    useCausalStroke, flip,  false, 0, 0));
                }
            }

            return bondGraph;
        }

        /// <summary>
        /// Gets a list of all Elements that introduce differential expressions into
        /// the state finalDifferentialStateEquations of this Graph.
        /// </summary>
        /// <returns></returns>
        public List<Element> GetDifferentialElements() {
            List<Element> differentials = new();
            foreach (KeyValuePair<string, Element> pair in this.elements) {
                char typeChar = pair.Value.GetTypeChar();
                if (typeChar == 'I' | typeChar == 'C') {
                    differentials.Add(pair.Value);
                }
            }
            return differentials;
        }

        /// <summary>
        /// Gets a list of all <c>Elements</c> in this <c>BondGraph</c>
        /// that have the corresponding IDs
        /// </summary>
        /// <param name="IDs">
        /// The array of IDs.
        /// </param>
        /// <returns>
        /// The Dictionary containing all relevant elements.
        /// </returns>
        public Dictionary<int, Element> GetElementsFromIDs(int[] IDs) {
            Dictionary<int, Element> elements = new(), parsedElements = new();
            foreach (KeyValuePair<string, Element> pair in this.elements) {
                Element element = pair.Value;
                elements.Add(element.GetID(), element);
            }
            foreach (int ID in IDs) {
                Element? element = elements.GetValueOrDefault(ID);
                if (element != null) {
                    parsedElements.Add(ID, element);
                }
            }
            return parsedElements;
        }

        /// <summary>
        /// Gets the JSON string form of this <c>BondGraph</c>.
        /// </summary>
        /// <returns>This <c>BondGraph</c> as a <c>string</c>.</returns>
        public string GetJSON() {
            return JsonConvert.SerializeObject(this);
        }

        public class Element {
            [JsonProperty]
            protected readonly string label;
            [JsonProperty]
            protected readonly double value;
            private readonly string name;

            //For graph visualization
            [JsonProperty]
            protected double x, y;

            /// <summary>
            /// Tracks the undo/redo and general IDs for this <c>Element</c>.
            /// </summary>
            private static int universalID = 0;
            [JsonProperty]
            private int? ID;

            /// <summary>
            /// Returns a string representing the element. The string includes label, value, name, x, and y coordinates of the element
            /// </summary>
            /// <returns> A string</returns>
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
                this.label = label;
                this.value = value;
                AssignID(0, true);

                Random rnd = new();
                this.x = rnd.Next(2000);
                this.y = rnd.Next(2000);
            }

            /// <summary>
            /// Sets the x and y coordinates of the element
            /// </summary>
            /// <param name="x">The x coordinate</param>
            /// <param name="y">The y coordinate</param>
            public void setPosition(double x, double y) {
                this.x = x;
                this.y = y;
            }

            /// <summary>
            /// Gets the x coordinate of the element
            /// </summary>
            /// <returns>The x coordinate</returns>
            public double getX() {
                return this.x;
            }

            /// <summary>
            /// Gets the y coordinate of the element
            /// </summary>
            /// <returns>The y coordinate</returns>
            public double getY() {
                return this.y;
            }

            /// <summary>
            /// Gets the name of the element
            /// </summary>
            /// <returns>The name of the element</returns>
            public string getName() {
                return this.name;
            }

            /// <summary>
            /// Assigns an ID to this <c>Element</c>.
            /// </summary>
            /// <param name="ID">
            /// A candidate ID.
            /// </param>
            /// <param name="isDistinct">
            /// <c>true</c> if the ID of this <c>Element</c> should be unique,
            /// else <c>false</c>.
            /// </param>
            public void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null | isDistinct) {
                    this.ID = universalID++;
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <c>Element</c>.
            /// </summary>
            /// <param name="isDistinct">
            /// <c>true</c> if the copy should have its own ID, else <c>false</c>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Element Copy(bool isDistinct) {
                Element copy = new(this.name, this.label, this.value) {
                    x = this.x,
                    y = this.y
                };
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Gets the ID of this <c>Element</c>.
            /// </summary>
            /// <returns>
            /// <c>this.ID</c>
            /// </returns>
            public int GetID() {
                return (this.ID is int ID) ? ID : 0;
            }

            /// <summary>
            /// Checks if two elements are equal
            /// </summary>
            /// <param name="obj">An element</param>
            /// <returns>True if the elements are equal, false otherwise</returns>
            public override bool Equals(object? obj) {
                return obj is Element element &&
                       this.name.Equals(element.name);
            }

            /// <summary>
            /// Creates a hash code for the element using the element's name
            /// </summary>
            /// <returns>An integer</returns>
            public override int GetHashCode() {
                return HashCode.Combine(this.name);
            }

            /// <summary>
            /// Determines from a character the type for this <c>Element</c>.
            /// </summary>
            /// <returns>A single character from the label that designates the 
            /// type of <c>Element</c>.</returns>
            public char GetTypeChar() {
                return this.label[0] == 'S' ? this.label[1] : this.label[0];
            }

            /// <summary>
            /// Converts this <c>Element</c> to a printable format.
            /// </summary>
            /// <returns>This <c>Element</c> as a string.</returns>
            public override string ToString() {
                return GetTypeChar() + ";" + GetID();
            }

            /// <summary>
            /// Gets the variable name for this <c>Element</c>.
            /// </summary>
            /// <returns></returns>
            // Change this once we get variable name input.
            public string GetVar() {
                return ToString();
            }
        }

        public class Bond {
            [JsonProperty]
            protected readonly int sourceID, targetID;
            protected readonly Element source, sink;
            private readonly string label;
            protected readonly double flow, effort;

            [JsonProperty]
            protected readonly bool causalStroke;
            //True means the causal stroke is at the source
            [JsonProperty]
            protected readonly bool causalStrokeDirection;
            [JsonProperty] 
            protected readonly bool hasDirection;

            /// <summary>
            /// Tracks the undo/redo and general IDs for this <c>Bond</c>.
            /// </summary>
            private static int universalID = 0;
            [JsonProperty]
            private int? ID;

            //The arrow will always point at the sink
            /// <summary>
            /// Creates a Bond between two elements
            /// </summary>
            /// <param name="sourceID">The ID of the source element</param>
            /// <param name="targetID">The ID of the target element</param>
            /// <param name="source">The source element</param>
            /// <param name="sink">The sink element</param>
            /// <param name="label">Labels for the bond</param>
            /// <param name="causalStroke">True if there should be a causal strong, false otherwise</param>
            /// <param name="causalStrokeDirection">The position of the causal stroke. True means the causal stroke is at the source. False means the causal stroke is at the sink.</param>
            /// <param name="flow">The flow value for the bond</param>
            /// <param name="effort">The effor value for the bond</param>

            public Bond(int sourceID, int targetID, Element source, Element sink, string label, bool causalStroke, bool causalStrokeDirection, bool hasDirection, double flow, double effort) {
                this.sourceID = sourceID;
                this.targetID = targetID;
                this.source = source;
                this.sink = sink;
                this.label = label;
                this.causalStroke = causalStroke;
                this.causalStrokeDirection = causalStrokeDirection;
                this.flow = flow;
                this.effort = effort;
                this.hasDirection = hasDirection;
                AssignID(0, true);
            }

            /// <summary>
            /// Checks if an element is the source
            /// </summary>
            /// <param name="e">An element</param>
            /// <returns>True if the element is the source, false otherwise</returns>
            public bool isSource(Element e) {
                return e.Equals(this.source);
            }

            /// <summary>
            /// Checks if an element is the sink
            /// </summary>
            /// <param name="e">An element</param>
            /// <returns>True if the element is the source, false otherwise</returns>
            public bool isSink(Element e) {
                return e.Equals(this.sink);
            }

            /// <summary>
            /// Returns the source
            /// </summary>
            /// <returns>An element</returns>
            public Element getSource() {
                return this.source;
            }

            /// <summary>
            /// Returns the sink
            /// </summary>
            /// <returns>An element</returns>
            public Element getSink() {
                return this.sink;
            }

            /// <summary>
            /// Returns the causal stroke direction of this <c>Bond</c>.
            /// </summary>
            /// <returns><c>true</c> if the causal stroke points from the source to
            /// the target (such that the visual stroke itself is on the source side
            /// of the bond), else <c>false</c>.</returns>
            public bool GetCausalDirection() {
                return this.causalStrokeDirection;
            }

            /// <summary>
            /// Assigns an ID to this <c>Bond</c>.
            /// </summary>
            /// <param name="ID">
            /// A candidate ID.
            /// </param>
            /// <param name="isDistinct">
            /// <c>true</c> if the ID of this <c>Bond</c> should be unique,
            /// else <c>false</c>.
            /// </param>
            public void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null | isDistinct) {
                    this.ID = universalID++;
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <c>Bond</c>.
            /// </summary>
            /// <param name="isDistinct">
            /// <c>true</c> if the copy should have its own ID, else <c>false</c>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Bond Copy(bool isDistinct) {
                Bond copy = new(this.sourceID, this.targetID, this.source, this.sink,
                    this.label, this.causalStroke, this.causalStrokeDirection, this.hasDirection,
                    this.flow, this.effort);
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Gets the ID of this <c>Bond</c>.
            /// </summary>
            /// <returns>
            /// <c>this.ID</c>
            /// </returns>
            public int GetID() {
                return (this.ID is int ID) ? ID : 0;
            }

            /// <summary>
            /// Checks if two bonds are equal
            /// </summary>
            /// <param name="obj">A bond</param>
            /// <returns>True if two bonds are equal, false otherwise</returns>
            public override bool Equals(object? obj) {
                return obj is Bond bond &&
                       this.sourceID.Equals(bond.sourceID) &&
                       this.targetID.Equals(bond.targetID) &&
                       this.causalStroke.Equals(bond.causalStroke) &&
                       this.causalStrokeDirection.Equals(bond.causalStrokeDirection);
            }

            /// <summary>
            /// Creates a hash code of the bond using the source id, target id, causal stroke, and causal stroke direction
            /// </summary>
            /// <returns>An integer</returns>
            public override int GetHashCode() {
                return HashCode.Combine(this.sourceID, this.targetID, this.causalStroke, this.causalStrokeDirection);
            }

            /// <summary>
            /// Converts this <c>Bond</c> to a printable format.
            /// </summary>
            /// <returns>This <c>Bond</c> as a string.</returns>
            public override string ToString() {
                return "(" + this.source + " " + this.sink + ")";
            }
        }

        public class BondGraphWrapper {
            private readonly Dictionary<string, BondGraph.Element> elements;
            private readonly Dictionary<int, List<BondGraph.Bond>> bondsBySource;
            private readonly Dictionary<int, List<BondGraph.Bond>> bondsByTarget;

            /// <summary>
            /// Creates a new <c>BondGraphWrapper</c>. This class rewrites a bond 
            /// graph as a graph object where all bonds incident to a given element
            /// can be accessed in O(1).
            /// </summary>
            /// <param name="graph">The bond graph used to model this object.</param>
            public BondGraphWrapper(BondGraph graph) {
                this.elements = graph.getElements();
                this.bondsBySource = new();
                this.bondsByTarget = new();
                foreach (BondGraph.Bond bond in graph.getBonds()) {
                    int source = bond.getSource().GetID();
                    List<BondGraph.Bond>? sourceBondsByElement = this.bondsBySource.GetValueOrDefault(source);
                    if (sourceBondsByElement == null) {
                        this.bondsBySource.Add(source, new List<BondGraph.Bond> { bond });
                    } else {
                        sourceBondsByElement.Add(bond);
                    }
                    int target = bond.getSink().GetID();
                    List<BondGraph.Bond>? targetBondsByElement = this.bondsByTarget.GetValueOrDefault(target);
                    if (targetBondsByElement == null) {
                        this.bondsByTarget.Add(target, new List<BondGraph.Bond> { bond });
                    } else {
                        targetBondsByElement.Add(bond);
                    }
                }
            }

            /// <summary>
            /// Gets the set of elements in this BondGraph.
            /// </summary>
            /// <returns><c>this.elements</c></returns>
            public Dictionary<string, BondGraph.Element> GetElements() {
                return this.elements;
            }

            /// <summary>
            /// Gets the set of all connections to any given bond such that
            /// the key (element ID) is the source ID for all bonds listed in
            /// the value.
            /// </summary>
            /// <returns>this.bondsBySource</returns>
            public Dictionary<int, List<BondGraph.Bond>> GetBondsBySource() {
                return this.bondsBySource;
            }

            /// <summary>
            /// Gets the set of all connections to any given bond such that
            /// the key (element ID) is the target ID for all bonds listed in
            /// the value.
            /// </summary>
            /// <returns>this.bondsByTarget</returns>
            public Dictionary<int, List<BondGraph.Bond>> GetBondsByTarget() {
                return this.bondsByTarget;
            }
        }
    }
}
