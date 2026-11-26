using BoGLWeb.BaseClasses;
using BoGLWeb.EditorHelper;
using GraphSynth.Representation;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
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
            { "_Inertia", "I:J" },
            { "_Spring", "C:K" },
            { "_Stiffness", "C:K" },
            { "_Torque_Input", "Se:τ" },
            { "_Damper", "R:b" },
            { "_Friction", "R:b" },
            { "_Resistor", "R:R" },
            { "_Force", "Se:F" },
            { "_Capacitor", "C:C" },
            { "_Velocity", "Sf:v" },
            { "_Inductor", "I:L" },
            { "_Rack&Pinion", "TF:R" },
            { "_Flywheel", "I:J" },
            { "_Voltage", "Se:V" },
            { "_Current", "Sf:i" },
            { "_ResistanceMotor", "R:R" },
            { "_InductanceMotor", "I:L" },
            { "_RotaryInertiaMotor", "I:J" },
            { "_GearMesh", "TF:R" },
            { "gear", "TF:R" },
            { "_Lever", "TF:D" },
            { "_Transformer", "TF:T" },
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

            Element.resetUni();
            Bond.resetUni();

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
            public string label;
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
            public string? domain;
            public bool visited = false;

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
                bool dist = false;
                if (name.Contains(":") || label.Contains("1") || label.Contains("0")) {
                    dist = true;
                }
                AssignID(0, dist);

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

            public static void resetUni() {
                universalID = 0;
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
            [JsonProperty]
            public string flowLabel = "f";
            [JsonProperty]
            public string effortLabel = "e";
            [JsonProperty]
            public string flowSubscript = "";
            [JsonProperty]
            public string effortSubscript = "";
            
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
                this.ID = source.GetID();
            }

            public static void resetUni() {
                universalID = 0;
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
            /// Returns true if this bond has a causal stroke.
            /// </summary>
            public bool HasCausalStroke() {
                return this.causalStroke;
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

        /// <summary>
        /// This class rewrites a bond graph as a graph object where all bonds 
        /// incident to a given element can be accessed in <c>O(1)</c>.
        /// </summary>
        public class BondGraphWrapper {
            private readonly Dictionary<string, Element> elements;
            private readonly Dictionary<int, List<Bond>> bondsBySource;
            private readonly Dictionary<int, List<Bond>> bondsByTarget;
            private readonly int EFFORT_INDEX = 0, 
                FLOW_INDEX = 1, 
                R_INDEX = 2, 
                C_INDEX = 3,
                I_INDEX = 4,
                TF_INDEX = 5,
                GY_INDEX = 6,
                P_INDEX = 7,
                Q_INDEX = 8;
            private static List<string> translationLabels = new List<string>() { "F", "v", "b", "K", "m", "D", "r", "p", "x" };
            private static List<string> rotLabels         = new List<string>() { "τ", "ω", "D", "κ", "J", "R", "r", "h", "θ" };
            private static List<string> elecLabels        = new List<string>() { "V", "i", "R", "C", "L", "T", "r", "ϕ", "q" };
            private readonly Dictionary<string, List<string>> domainLabelDict = new Dictionary<string, List<string>>() {
                {"v", translationLabels },
                {"F", translationLabels },
                {"b", translationLabels },
                {"m", translationLabels },
                {"K", translationLabels },
                {"D", translationLabels },
                {"ω", rotLabels },
                {"τ", rotLabels },
                {"J", rotLabels },
                {"κ", rotLabels },
                {"i", elecLabels },
                {"V", elecLabels },
                {"R", elecLabels },
                {"L", elecLabels },
                {"C", elecLabels },
                {"T", elecLabels }
            };
            private readonly Dictionary<string, List<string>> stateLabelDict = new Dictionary<string, List<string>>() {
                {"F", new List<string>() { "p'", "x'" } },
                {"τ", new List<string>() { "h'", "θ'" } },
                {"V", new List<string>() { "ϕ'", "q'" } }
            };

            /// <summary>
            /// Creates a new <c>BondGraphWrapper</c>. 
            /// </summary>
            /// <param name="graph">The bond graph used to model this object.</param>
            public BondGraphWrapper(BondGraph graph) {
                this.elements = graph.getElements();
                this.bondsBySource = new();
                this.bondsByTarget = new();
                foreach (Bond bond in graph.getBonds()) {
                    int source = bond.getSource().GetID();
                    List<Bond>? sourceBondsByElement = this.bondsBySource.GetValueOrDefault(source);
                    if (sourceBondsByElement == null) {
                        this.bondsBySource.Add(source, new List<Bond> { bond });
                    } else {
                        sourceBondsByElement.Add(bond);
                    }
                    int target = bond.getSink().GetID();
                    List<Bond>? targetBondsByElement = this.bondsByTarget.GetValueOrDefault(target);
                    if (targetBondsByElement == null) {
                        this.bondsByTarget.Add(target, new List<Bond> { bond });
                    } else {
                        targetBondsByElement.Add(bond);
                    }
                }
            }

            /// <summary>
            /// Gets the set of elements in this BondGraph.
            /// </summary>
            /// <returns><c>this.elements</c></returns>
            public Dictionary<string, Element> GetElements() {
                return this.elements;
            }

            /// <summary>
            /// Gets the set of all connections to any given bond such that
            /// the key (element ID) is the source ID for all bonds listed in
            /// the value.
            /// </summary>
            /// <returns>this.bondsBySource</returns>
            public Dictionary<int, List<Bond>> GetBondsBySource() {
                return this.bondsBySource;
            }

            /// <summary>
            /// Gets the set of all connections to any given bond such that
            /// the key (element ID) is the target ID for all bonds listed in
            /// the value.
            /// </summary>
            /// <returns>this.bondsByTarget</returns>
            public Dictionary<int, List<Bond>> GetBondsByTarget() {
                return this.bondsByTarget;
            }

            public void AssignBondLabels() {
            /* iterate through all elements in bond graph:
                verify that element is leaf node
                do depth first search that stops at gyrator elements
                flip visited boolean on nodes and set domain on bonds (set domain on 0 and 1 junctions and transformers and add them to list)*/
                Queue<Element> queue = new Queue<Element>();
                List<Element> futureLeaves = this.elements.Values.ToList().FindAll(e => {
                    int targetCount = GetBondsByTarget().ContainsKey(e.GetID()) ? GetBondsByTarget()[e.GetID()].Count : 0;
                    int sourceCount = GetBondsBySource().ContainsKey(e.GetID()) ? GetBondsBySource()[e.GetID()].Count : 0;
                    return (targetCount + sourceCount) == 1;
                });
                while (futureLeaves.Count > 0) {
                    Element startEl = futureLeaves[0];
                    futureLeaves.RemoveAt(0);
                    queue.Enqueue(startEl);
                    // we have a 1 junction being a leaf node here
                    string domainCheck = startEl.label.Last().ToString();
                    List<string> labels = domainLabelDict.ContainsKey(domainCheck) ? domainLabelDict[domainCheck] : new() { "e", "f" };
                    string effortLabel = labels[EFFORT_INDEX];
                    string flowLabel = labels[FLOW_INDEX];

                    while (queue.Count > 0) {
                        Element el = queue.Dequeue();
                        if (el.visited) continue;
                        el.visited = true;
                        el.domain = effortLabel;
                        List<Element> sourceBonds = this.bondsBySource.ContainsKey(el.GetID()) ? this.bondsBySource[el.GetID()].Select(b => b.getSink()).ToList() : new();
                        List<Element> targetBonds = this.bondsByTarget.ContainsKey(el.GetID()) ? this.bondsByTarget[el.GetID()].Select(b => b.getSource()).ToList() : new();
                        List<Element> neighbors = sourceBonds.Concat(targetBonds).ToList();
                        if (el.label[0] != 'G' && el.label[0] != 'T') {
                            neighbors.ForEach(o => queue.Enqueue(o));
                            if (this.bondsBySource.ContainsKey(el.GetID())) {
                                this.bondsBySource[el.GetID()].ForEach(b => {
                                    b.effortLabel = effortLabel;
                                    b.flowLabel = flowLabel;
                                });
                            }
                            if (this.bondsByTarget.ContainsKey(el.GetID())) {
                                this.bondsByTarget[el.GetID()].ForEach(b => {
                                    b.effortLabel = effortLabel;
                                    b.flowLabel = flowLabel;
                                });
                            }
                            if (el.label[0] == '0') {
                                el.label += " " + effortLabel;
                            } else if (el.label[0] == '1') {
                                el.label += " " + flowLabel;
                            }
                        }
                    }

                    futureLeaves = futureLeaves.FindAll(e => !e.visited);
                }

                // Update TF display label to TF:1/<sym> when power flows from translational (v) to rotational (ω)
                foreach (Element el in this.elements.Values) {
                    if (el.label.StartsWith("TF:", StringComparison.Ordinal) && !el.label.StartsWith("TF:1/", StringComparison.Ordinal)) {
                        var inB = this.bondsByTarget.TryGetValue(el.GetID(), out var tList) && tList.Count > 0 ? tList[0] : null;
                        var outB = this.bondsBySource.TryGetValue(el.GetID(), out var sList) && sList.Count > 0 ? sList[0] : null;
                        if (inB != null && outB != null) {
                            string inDom = GetElementDomain(inB.getSource());
                            string outDom = GetElementDomain(outB.getSink());
                            if (inDom == "F" && outDom == "τ") {
                                el.label = "TF:1/" + el.label[3..];
                            }
                        }
                    }
                }

                // Assign consistent effort/flow labels and subscripts on every bond:
                // - Around a 1-junction (1 f_j), all bonds share flow f_j (except integral C, which uses x'_j / θ'_j / q'_j).
                // - Around a 0-junction (0 e_j), all bonds share effort e_j.
                // - Integral I elements use p'_id / h'_id / ϕ'_id; derivative I elements use F_id / τ_id / V_id.
                foreach (Bond b in this.bondsBySource.Values.SelectMany(x => x).Distinct()) {
                    Element src = b.getSource();
                    Element dst = b.getSink();
                    char srcType = char.ToUpper(src.GetTypeChar());
                    char dstType = char.ToUpper(dst.GetTypeChar());

                    Element? j1 = srcType == '1' ? src : (dstType == '1' ? dst : null);
                    Element? j0 = srcType == '0' ? src : (dstType == '0' ? dst : null);
                    Element? onePort = IsOnePortChar(srcType) ? src : (IsOnePortChar(dstType) ? dst : null);
                    Element? twoPort = (srcType == 'T' || srcType == 'G') ? src : ((dstType == 'T' || dstType == 'G') ? dst : null);

                    // 1) Flow label & subscript
                    if (onePort != null && char.ToUpper(onePort.GetTypeChar()) == 'C' && !IsElementReceivingEffort(b, onePort)) {
                        // Integral C: state derivative x'_j / θ'_j / q'_j (using junction ID when attached to a junction)
                        Element? otherJunc = ReferenceEquals(onePort, src) ? dst : src;
                        int subId = (otherJunc.GetTypeChar() is '0' or '1') ? otherJunc.GetID() : onePort.GetID();
                        b.flowLabel = GetStateVarPrefix(onePort) + "'";
                        b.flowSubscript = subId.ToString();
                    } else if (j1 != null) {
                        b.flowLabel = GetFlowPrefix(GetElementDomain(j1));
                        b.flowSubscript = j1.GetID().ToString();
                    } else if (j0 != null && onePort != null) {
                        b.flowLabel = GetFlowPrefix(GetElementDomain(onePort));
                        b.flowSubscript = onePort.GetID().ToString();
                    } else if (twoPort != null) {
                        Element jEnd = ReferenceEquals(twoPort, src) ? dst : src;
                        b.flowLabel = GetFlowPrefix(GetElementDomain(jEnd));
                        b.flowSubscript = twoPort.GetID().ToString();
                    } else {
                        b.flowLabel = GetFlowPrefix(GetElementDomain(src));
                        b.flowSubscript = $"{src.GetID()},{dst.GetID()}";
                    }

                    // 2) Effort label & subscript
                    if (onePort != null && char.ToUpper(onePort.GetTypeChar()) == 'I' && IsElementReceivingEffort(b, onePort)) {
                        // Integral I: state derivative p'_id / h'_id / ϕ'_id
                        b.effortLabel = GetStateVarPrefix(onePort) + "'";
                        b.effortSubscript = onePort.GetID().ToString();
                    } else if (j0 != null) {
                        b.effortLabel = GetEffortPrefix(GetElementDomain(j0));
                        b.effortSubscript = j0.GetID().ToString();
                    } else if (onePort != null) {
                        char opType = char.ToUpper(onePort.GetTypeChar());
                        b.effortLabel = (opType == 'E')
                            ? GetElementParamSymbol(onePort)
                            : GetEffortPrefix(GetElementDomain(onePort));
                        b.effortSubscript = onePort.GetID().ToString();
                    } else if (twoPort != null) {
                        Element jEnd = ReferenceEquals(twoPort, src) ? dst : src;
                        b.effortLabel = GetEffortPrefix(GetElementDomain(jEnd));
                        b.effortSubscript = twoPort.GetID().ToString();
                    } else {
                        b.effortLabel = GetEffortPrefix(GetElementDomain(src));
                        b.effortSubscript = $"{src.GetID()},{dst.GetID()}";
                    }
                }
            }

            private static bool IsOnePortChar(char t) => t is 'I' or 'C' or 'R' or 'E' or 'F';

            private static bool IsElementReceivingEffort(Bond bond, Element el) {
                return bond.isSource(el) ? bond.GetCausalDirection() : !bond.GetCausalDirection();
            }

            /// <summary>
            /// Infers domain tags on elements without mutating element labels.
            /// </summary>
            public void InferDomainsIfNeeded() {
                if (this.elements.Values.Any(e => !string.IsNullOrEmpty(e.domain))) {
                    return;
                }
                List<Element> leaves = this.elements.Values.ToList().FindAll(e => {
                    int targetCount = this.bondsByTarget.TryGetValue(e.GetID(), out var tList) ? tList.Count : 0;
                    int sourceCount = this.bondsBySource.TryGetValue(e.GetID(), out var sList) ? sList.Count : 0;
                    return (targetCount + sourceCount) == 1;
                });
                HashSet<int> visitedIds = new();
                foreach (Element startEl in leaves) {
                    if (visitedIds.Contains(startEl.GetID())) {
                        continue;
                    }
                    string domainCheck = startEl.label.Length > 0 ? startEl.label[^1].ToString() : "e";
                    List<string> labels = domainLabelDict.ContainsKey(domainCheck) ? domainLabelDict[domainCheck] : new() { "e", "f" };
                    string effortLabel = labels[EFFORT_INDEX];

                    Queue<Element> queue = new();
                    queue.Enqueue(startEl);
                    while (queue.Count > 0) {
                        Element el = queue.Dequeue();
                        if (!visitedIds.Add(el.GetID())) {
                            continue;
                        }
                        el.domain = effortLabel;
                        if (el.label.Length > 0 && el.label[0] != 'G' && el.label[0] != 'T') {
                            if (this.bondsBySource.TryGetValue(el.GetID(), out var sBonds)) {
                                foreach (Bond b in sBonds) {
                                    queue.Enqueue(b.getSink());
                                }
                            }
                            if (this.bondsByTarget.TryGetValue(el.GetID(), out var tBonds)) {
                                foreach (Bond b in tBonds) {
                                    queue.Enqueue(b.getSource());
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Extracts the parameter/source symbol from an element label (e.g. "TF:R" -> "R", "TF:1/R" -> "R", "I:J" -> "J").
            /// </summary>
            public static string GetElementParamSymbol(Element el) {
                string label = el.label ?? "";
                int colon = label.IndexOf(':');
                if (colon >= 0 && colon + 1 < label.Length) {
                    string sym = label[(colon + 1)..].Trim();
                    if (sym.StartsWith("1/", StringComparison.Ordinal)) {
                        sym = sym[2..];
                    }
                    int space = sym.IndexOf(' ');
                    return space >= 0 ? sym[..space] : sym;
                }
                return el.GetTypeChar().ToString();
            }

            /// <summary>
            /// Gets the effort domain key ("F", "τ", "V", or "e") for an element.
            /// </summary>
            public string GetElementDomain(Element el) {
                string sym = GetElementParamSymbol(el);
                if (sym is "J" or "τ" or "ω" or "κ") {
                    return "τ";
                }
                if (sym is "C" or "V" or "i" or "q" or "ϕ" or "L") {
                    return "V";
                }
                if (sym is "m" or "F" or "v" or "p" or "x") {
                    return "F";
                }
                if (!string.IsNullOrEmpty(el.domain) && el.domain != "e") {
                    return el.domain;
                }
                return "F";
            }

            /// <summary>Gets the effort variable prefix ("F", "τ", "V", or "e") for a domain key.</summary>
            public static string GetEffortPrefix(string? domain) => domain switch {
                "τ" => "τ",
                "V" => "V",
                "F" => "F",
                _ => "e"
            };

            /// <summary>Gets the flow variable prefix ("v", "ω", "i", or "f") for a domain key.</summary>
            public static string GetFlowPrefix(string? domain) => domain switch {
                "τ" => "ω",
                "V" => "i",
                "F" => "v",
                _ => "f"
            };

            /// <summary>Gets the generalized momentum/displacement state prefix for an I or C element.</summary>
            public string GetStateVarPrefix(Element el) {
                char type = char.ToUpper(el.GetTypeChar());
                string domain = GetElementDomain(el);
                if (type == 'I') {
                    return domain switch {
                        "τ" => "h",
                        "V" => "ϕ",
                        _ => "p"
                    };
                }
                return domain switch {
                    "τ" => "θ",
                    "V" => "q",
                    _ => "x"
                };
            }

            /// <summary>
            /// Gets the domain-specific replacement variable for a particular element.
            /// </summary>
            /// <param name="label">The element label.</param>
            /// <param name="c">The character denoting the desired coefficient.</param>
            /// <returns>The character belonging at the beginning of the replacement
            /// variable.</returns>
            public char GetDomainVar(string label, char c) {
                int index = 0;
                while (index < label.Length && label[index] != ' ' && label[index] != ':') {
                    index++;
                }
                index++;
                if (index < label.Length && domainLabelDict.TryGetValue("" + label[index], out var list)) {
                    return list[GetReplacementIndex(c)][0];
                }
                return c;
            }

            /// <summary>
            /// Gets the replacement index.
            /// </summary>
            /// <param name="c">The character representing the index in the 
            /// variable list.</param>
            /// <returns>The replacement index.</returns>
            public int GetReplacementIndex(char c) {
                return char.ToUpper(c) switch {
                    'E' => EFFORT_INDEX,
                    'F' => FLOW_INDEX,
                    'R' => R_INDEX,
                    'C' => C_INDEX,
                    'I' => I_INDEX,
                    'T' => TF_INDEX,
                    'G' => GY_INDEX,
                    'P' => P_INDEX,
                    'Q' => Q_INDEX,
                    _ => throw new Exception("Invalid substitution index character " + c + ".")
                };
            }
        }
    }
}
