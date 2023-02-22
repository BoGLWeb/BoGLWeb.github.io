using AntDesign.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.util;

namespace BoGLWeb {
    /// <summary>
    /// Namespace <c>EditorHelper</c> provides undo/redo functionality.
    /// </summary>
    namespace EditorHelper {
        /// <summary>
        /// Keeps track of changes made to the canvas.
        /// </summary>
        public class CanvasChange {
            // Stores the IDs of the respective selected elements.
            private readonly int[] IDs;

            /// <summary>
            /// Creates a new Can
            /// </summary>
            /// <param name="IDs">
            /// The array of elementIDs denoting the selected 
            /// </param>
            public CanvasChange(int[] IDs) {
                this.IDs = IDs;
            }

            /// <summary>
            /// The parent function of the 'executeUpdate' methods in the
            /// CanvasChange subclasses.
            /// </summary>
            /// <param name="diagram">
            /// The input graph.
            /// </param>
            /// <param name="isUndo">
            /// <c>true</c> if the action executing this update was an 'undo' call,
            /// else <c>false</c> if the action was a 'redo' call.
            /// </param>
            public virtual void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                //Console.WriteLine(diagram);
            }

            /// <summary>
            /// The parent function of the 'executeUpdate' methods in the
            /// CanvasChange subclasses.
            /// </summary>
            /// <param name="graph">
            /// The input graph.
            /// </param>
            /// <param name="isUndo">
            /// <c>true</c> if the action executing this update was an 'undo' call,
            /// else <c>false</c> if the action was a 'redo' call.
            /// </param>
            public virtual void ExecuteUpdate(BondGraph graph, bool isUndo) {
            }

            /// <summary>
            /// Gets the set of elementIDs for this CanvasChange.
            /// </summary>
            /// <returns>
            /// <c>this.elementIDs</c>
            /// </returns>
            public int[] GetIDs() {
                return this.IDs;
            }

            /// <summary>
            /// Stores a change made when the user adds a group of items to the
            /// system graph.
            /// </summary>
            public class AddSelection : CanvasChange {
                // Stores the JSON form of the added elements
                private readonly string[] newObjects;
                // Stores the previous group of selected edgesBySource
                private readonly string[] prevSelectedEdges;
                // Stores the Element form of the added elements
                private readonly Dictionary<int, SystemDiagram.Element> newElements;
                // Stores the Edge form of the added edgesBySource by source ID
                private readonly Dictionary<int, List<SystemDiagram.Edge>> newEdges;
                // Stores whether the added selection should be highlighted
                private bool highlight;

                // TODO: fix comment
                /// <summary> 
                /// Creates a new AddSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of elementIDs in the selection.
                /// </param>
                /// <param name="newObjects">The JSON string carrying info about
                /// the added elements.
                /// </param>
                /// <param name="prevSelectedEdges">The JSON string carrying info about
                /// the added elements.
                /// </param>
                public AddSelection(int[] IDs, string[] newObjects, string[] prevSelectedEdges, bool highlight) : base(IDs) {
                    this.newObjects = newObjects;
                    this.prevSelectedEdges = prevSelectedEdges;
                    SystemDiagram.Wrapper packager = new(newObjects);
                    this.newElements = packager.GetElements();
                    this.newEdges = packager.GetSourceEdges();
                    this.highlight = highlight;
                }

                /// <summary>
                /// Executes the update made to the system graph during a 
                /// <c>AddSelection</c> action.
                /// </summary>
                /// <param name="diagram">The system graph.</param>
                /// <param name="isUndo"><c>true</c> if this method was called during
                /// the 'undo' action, else <c>false</c> if it was called during the
                /// 'redo' action.</param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    List<SystemDiagram.Element> elements = diagram.getElements();
                    List<SystemDiagram.Edge> edges = diagram.getEdges();
                    if (isUndo) {
                        ListIterator<SystemDiagram.Element> elementIterator = new(elements);
                        while (elementIterator.HasNext()) {
                            if (this.newElements.ContainsKey(elementIterator.Next().GetID())) {
                                elementIterator.Remove();
                            }
                        }
                        ListIterator<SystemDiagram.Edge> edgeIterator = new(edges);
                        while (edgeIterator.HasNext()) {
                            SystemDiagram.Edge edge = edgeIterator.Next();
                            if (this.newEdges.ContainsKey(edge.getSource())) {
                                edgeIterator.Remove();
                            }
                        }
                    } else {
                        foreach (KeyValuePair<int, SystemDiagram.Element> pair in this.newElements) {
                            elements.Add(pair.Value);
                        }
                        foreach (KeyValuePair<int, List<SystemDiagram.Edge>> pair in this.newEdges) {
                            edges.AddRange(pair.Value);
                        }
                    }
                    base.ExecuteUpdate(diagram, isUndo);
                }

                /// <summary>
                /// Gets the JSON array of elements from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.newObjects</returns>
                public string[] GetNewObjects() {
                    return this.newObjects;
                }

                /// <summary>
                /// Gets the previous packager selection from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.prevSelectedEdges</returns>
                public string[] GetPrevSelectedEdges() {
                    return this.prevSelectedEdges;
                }

                /// <summary>
                /// Gets the highlight boolean for the selection</c>.
                /// </summary>
                /// <returns>this.highlight</returns>
                public bool GetHighlight() {
                    return this.highlight;
                }

                /// <summary>
                /// Converts this <c>AddSelection</c> to a printable format.
                /// </summary>
                /// <returns>
                /// This <c>AddSelection</c> as a <c>string</c>.
                /// </returns>
                public override string ToString() {
                    return "This is an AddSelection.";
                }
            }

            /// <summary>
            /// Stores a change made when the user deletes a group of items from the
            /// system graph.
            /// </summary>
            public class DeleteSelection : CanvasChange {
                // The JSON objects storing the deleted elements
                private readonly string[] deleted;
                // The set of unselected deleted edges
                private readonly string[] unselectedDeletedEdges;
                // Stores the Element form of the added elements
                private readonly Dictionary<int, SystemDiagram.Element> oldElements;
                // Stores the Edge form of the added edgesBySource by source ID
                private readonly Dictionary<int, List<SystemDiagram.Edge>> oldEdgesBySource;
                // Stores the Edge form of the added edgesBySource by target ID
                private readonly Dictionary<int, List<SystemDiagram.Edge>> oldEdgesByTarget;

                /// <summary>
                /// Creates a new DeleteSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of elementIDs in the selection.
                /// </param>
                /// <param name="json">The JSON string carrying info about
                /// the added elements.
                /// </param>
                public DeleteSelection(int[] IDs, string[] deleted, string[] unselectedDeletedEdges) : base(IDs) {
                    SystemDiagram.Wrapper deletedPackager = new(deleted);
                    this.oldElements = deletedPackager.GetElements();
                    this.oldEdgesBySource = deletedPackager.GetSourceEdges();
                    this.oldEdgesByTarget = deletedPackager.GetTargetEdges();
                    this.deleted = deleted;
                    this.unselectedDeletedEdges = unselectedDeletedEdges;
                    SystemDiagram.Wrapper unselectedPackager = new(unselectedDeletedEdges);
                    foreach (KeyValuePair<int, List<SystemDiagram.Edge>> pair in unselectedPackager.GetSourceEdges()) {
                        this.oldEdgesBySource.GetValueOrDefault(pair.Key)?.AddRange(pair.Value);
                    }
                    foreach (KeyValuePair<int, List<SystemDiagram.Edge>> pair in unselectedPackager.GetTargetEdges()) {
                        this.oldEdgesByTarget.GetValueOrDefault(pair.Key)?.AddRange(pair.Value);
                    }
                }

                /// <summary>
                /// Executes the update made to the system graph during a 
                /// <c>DeleteSelection</c> action.
                /// </summary>
                /// <param name="diagram">The system graph.</param>
                /// <param name="isUndo"><c>true</c> if this method was called during
                /// the 'undo' action, else <c>false</c> if it was called during the
                /// 'redo' action.</param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    List<SystemDiagram.Element> elements = diagram.getElements();
                    List<SystemDiagram.Edge> edges = diagram.getEdges();
                    if (isUndo) {
                        foreach (KeyValuePair<int, SystemDiagram.Element> pair in this.oldElements) {
                            elements.Add(pair.Value);
                        }
                        HashSet<int> usedSourceIDs = new();
                        foreach (KeyValuePair<int, List<SystemDiagram.Edge>> pair in this.oldEdgesBySource) {
                            edges.AddRange(pair.Value);
                            usedSourceIDs.Add(pair.Key);
                        }
                        foreach (KeyValuePair<int, List<SystemDiagram.Edge>> pair in this.oldEdgesByTarget) {
                            foreach (SystemDiagram.Edge edge in pair.Value) {
                                if (!usedSourceIDs.Contains(edge.getSource())) {
                                    edges.Add(edge);
                                }
                            }
                        }
                    } else {
                        ListIterator<SystemDiagram.Element> elementIterator = new(elements);
                        while (elementIterator.HasNext()) {
                            if (this.oldElements.ContainsKey(elementIterator.Next().GetID())) {
                                elementIterator.Remove();
                            }
                        }
                        ListIterator<SystemDiagram.Edge> edgeIterator = new(edges);
                        while (edgeIterator.HasNext()) {
                            SystemDiagram.Edge edge = edgeIterator.Next();
                            int source = edge.getSource(), target = edge.getTarget();
                            if (this.oldEdgesBySource.ContainsKey(source) | this.oldEdgesByTarget.ContainsKey(target)) {
                                edgeIterator.Remove();
                            } else if (this.oldElements.ContainsKey(source)) {
                                edgeIterator.Remove();
                                this.oldEdgesBySource.Add(source, new List<SystemDiagram.Edge>() { edge });
                            } else if (this.oldElements.ContainsKey(target)) {
                                edgeIterator.Remove();
                                this.oldEdgesByTarget.Add(target, new List<SystemDiagram.Edge>() { edge });
                            }
                        }
                    }
                    base.ExecuteUpdate(diagram, isUndo);
                }

                /// <summary>
                /// Gets the stored JSON string from this <c>DeleteSelection</c>.
                /// </summary>
                /// <returns>The JSON string object</returns>
                public string[] GetDeletedJSONElements() {
                    return this.deleted;
                }

                /// <summary>
                /// Gets the set of unselected deleted edges in this 
                /// <c>DeleteSelection</c>.
                /// </summary>
                /// <returns><c>this.unselectedDeletedEdges</c></returns>
                public string[] GetUnselectedEdges() {
                    return this.unselectedDeletedEdges;
                }
            }

            /// <summary>
            /// Stores a change made when the selection changes. This 
            /// <c>CanvasChange</c> does not update the backend.
            /// </summary>
            public class ChangeSelection : CanvasChange {
                // Stores the array of elementIDs for the new selection of elements.
                private readonly int[] newElementIDS;
                //Stores the array of elementIDs for the old selected edgesBySource.
                private readonly string[] oldEdgeIDs;
                //Stores the array of elementIDs for the new selected edgesBySource.
                private readonly string[] newEdgeIDs;

                /// <summary>
                /// Creates a new ChangeSelection CanvasChange.
                /// </summary>
                /// <param name="oldElementIDs">
                /// The set of elementIDs in the selection.
                /// </param>
                public ChangeSelection(int[] oldElementIDs, int[] newElementIDs, string[] oldEdgeIDs, string[] newEdgeIDs) : base(oldElementIDs) {
                    this.newElementIDS = newElementIDs;
                    this.oldEdgeIDs = oldEdgeIDs;
                    this.newEdgeIDs = newEdgeIDs;
                }

                /// <summary>
                /// Gets the new selection of elements in the bond graph.
                /// </summary>
                /// <param name="isUndo">
                /// <c>true</c> if the 'undo' function has been called to
                /// trigger this method, else <c>false</c> if the 'redo' 
                /// function was called.
                /// </param>
                /// <returns>
                /// The list of selected elementIDs.
                /// </returns>
                public int[] GetNewElementIDs() {
                    return this.newElementIDS;
                }

                /// <summary>
                /// Gets the old packager elementIDs
                /// </summary>
                /// <returns><c>this.oldEdgeIDs</c></returns>
                public string[] GetOldEdgeIDs() {
                    return this.oldEdgeIDs;
                }

                /// <summary>
                /// Gets the new packager elementIDs
                /// </summary>
                /// <returns><c>this.newEdgeIDs</c></returns>
                public string[] GetNewEdgeIDs() {
                    return this.newEdgeIDs;
                }
            }

            /// <summary>
            /// Stores a change in modifier for a selection of elements.
            /// </summary>
            public class ChangeModifier : CanvasChange {
                // Stores the ID of the (un)selected modifier.
                private readonly int modID;
                // Stores the boolean value denoting whether or not the
                // modifier has been toggled on ('true') or off ('false').
                private readonly bool toggle;
                // Stores the previously stored values for this modifier.
                private readonly bool[] current;

                /// <summary>
                /// Creates a new <c>ChangeSelection</c> <c>CanvasChange</c>.
                /// </summary>
                /// <param name="IDs">
                /// The set of elementIDs in the selection.
                /// </param>
                public ChangeModifier(int[] IDs, int modID, bool toggle, bool[] current) : base(IDs) {
                    this.modID = modID;
                    this.toggle = toggle;
                    this.current = current;
                }

                /// <summary>
                /// Updates the system graph to include changes made via a
                /// <c>ChangeModifier</c>.
                /// </summary>
                /// <param name="diagram">
                /// The <c>SystemDiagram</c> to which changes are made.
                /// </param>
                /// <param name="isUndo">
                /// <c>true</c> if the action executing this update was an 'undo' 
                /// call, else <c>false</c> if the action was a 'redo' call.
                /// </param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    Dictionary<int, SystemDiagram.Element> elements = diagram.GetElementsFromIDs(this.IDs);
                    for (int i = 0; i < this.IDs.Length; i++) {
                        SystemDiagram.Element element = elements[this.IDs[i]];
                        element.getModifiers().Remove(this.modID);
                        if ((isUndo & this.current[i]) | ((!isUndo) & toggle)) {
                            element.addModifier(this.modID);
                        }
                    }
                    base.ExecuteUpdate(diagram, isUndo);
                }

                /// <summary>
                /// Gets the ID of the altered modifier.
                /// </summary>
                /// <returns>
                /// <c>this.modID</c>
                /// </returns>
                public int GetModID() { return this.modID; }

                /// <summary>
                /// Gets the new toggled state of the altered modifier.
                /// </summary>
                /// <returns>
                /// <c>this.toggle</c>
                /// </returns>
                public bool GetToggle() { return this.toggle; }

                /// <summary>
                /// Gets the original state of the altered modifiers in each object.
                /// </summary>
                /// <returns>
                /// <c>this.current</c>
                /// </returns>
                public bool[] GetCurrent() { return this.current; }
            }

            /// <summary>
            /// Stores a change made when a selection of items is moved.
            /// </summary>
            public class MoveSelection : CanvasChange {
                // Stores the x- and y-offsets for this CanvasChange.
                private readonly double xOffset, yOffset;

                /// <summary>
                /// Creates a new <c>MoveSelection</c> <c>CanvasChange</c>.
                /// </summary>
                /// <param name="IDs">
                /// The array of elementIDs of the elements in the selection.
                /// </param>
                /// <param name="xOffset">
                /// The offset in the x-direction.
                /// </param>
                /// <param name="yOffset">
                /// The offset in the y-direction.
                /// </param>
                public MoveSelection(int[] IDs, double xOffset, double yOffset) : base(IDs) {
                    this.xOffset = xOffset;
                    this.yOffset = yOffset;
                }

                /// <summary>
                /// Updates the system graph to include changes made via a
                /// <c>MoveSelection</c>.
                /// </summary>
                /// <param name="diagram">
                /// The <c>SystemDiagram</c> to which changes are made.
                /// </param>
                /// <param name="isUndo">
                /// <c>true</c> if the action executing this update was an 'undo' 
                /// call, else <c>false</c> if the action was a 'redo' call.
                /// </param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    double x = this.xOffset, y = this.yOffset;
                    if (isUndo) {
                        x = -x;
                        y = -y;
                    }
                    foreach(KeyValuePair<int, SystemDiagram.Element> pair in diagram.GetElementsFromIDs(this.IDs)) {
                        SystemDiagram.Element element = pair.Value;
                        element.SetX(element.getX() + x);
                        element.SetY(element.getY() + y);
                    }
                    base.ExecuteUpdate(diagram, isUndo);
                }

                /// <summary>
                /// Updates the bond graph to include changes made via a
                /// <c>MoveSelection</c>.
                /// </summary>
                /// <param name="graph">
                /// The <c>BondGraph</c> to which changes are made.
                /// </param>
                /// <param name="isUndo">
                /// <c>true</c> if the action executing this update was an 'undo' 
                /// call, else <c>false</c> if the action was a 'redo' call.
                /// </param>
                public override void ExecuteUpdate(BondGraph graph, bool isUndo) {
                    double x = this.xOffset, y = this.yOffset;
                    if (isUndo) {
                        x = -x;
                        y = -y;
                    }
                    foreach (KeyValuePair<int, BondGraph.Element> pair in graph.GetElementsFromIDs(this.IDs)) {
                        BondGraph.Element element = pair.Value;
                        element.setPosition(element.getX() + x, element.getY() + y);
                    }
                }

                /// <summary>
                /// Gets the xOffset for this <c>MoveSelection</c>.
                /// </summary>
                /// <returns>
                /// <c>this.xOffset</c>
                /// </returns>
                public double GetXOffset() {
                    return this.xOffset;
                }

                /// <summary>
                /// Gets the yOffset for this <c>MoveSelection</c>.
                /// </summary>
                /// <returns>
                /// <c>this.yOffset</c>
                /// </returns>
                public double GetYOffset() {
                    return this.yOffset;
                }

                /// <summary>
                /// Converts this <c>AddSelection</c> to a printable format.
                /// </summary>
                /// <returns>
                /// This <c>AddSelection</c> as a <c>string</c>.
                /// </returns>
                public override string ToString() {
                    string print = "Moved:";
                    foreach (int ID in this.IDs) {
                        print += " " + ID;
                    }
                    return print + "; " + this.xOffset + "; " + this.yOffset;
                }
            }

            /// <summary>
            /// Stores a change in velocity made to a selection of elements.
            /// </summary>
            public class ChangeVelocity : CanvasChange {
                // Stores partial JSON strings for all edges in the selection.
                private readonly string[] edgeIDs;
                // Stores EdgeIDPackagers for all edges in the selection.
                private readonly Dictionary<int, List<EdgeIDPackager>> edges;
                // Stores the ID of the new velocity.
                private readonly int newVelID;
                // Stores the elementIDs of the previous velocities for each target element.
                private readonly int[] oldIDs;

                /// <summary>
                /// Creates a new ChangeSelection CanvasChange.
                /// </summary>
                /// <param name="elementIDs">
                /// The set of elementIDs in the selection.
                /// </param>
                public ChangeVelocity(int[] elementIDs, string[] edgeIDs, int newVelID, int[] oldVelIDs) : base(elementIDs) {
                    this.edgeIDs = edgeIDs;
                    this.newVelID = newVelID;
                    this.oldIDs = oldVelIDs;
                    this.edges = new();
                    for (int i = 0; i < edgeIDs.Length; i++) {
                        EdgeIDPackager packager = new(edgeIDs[i], oldVelIDs[i + elementIDs.Length]);
                        int source = packager.Source();
                        List<EdgeIDPackager>? list = this.edges.GetValueOrDefault(source);
                        if (list == null) {
                            this.edges.Add(source, new List<EdgeIDPackager> { packager });
                        } else {
                            list.Add(packager);
                        }
                    }
                }

                /// <summary>
                /// Updates the system graph to include changes made via a
                /// <c>ChangeVelocity</c>.
                /// </summary>
                /// <param name="diagram">
                /// The <c>SystemDiagram</c> to which changes are made.
                /// </param>
                /// <param name="isUndo">
                /// <c>true</c> if the action executing this update was an 'undo' 
                /// call, else <c>false</c> if the action was a 'redo' call.
                /// </param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    Dictionary<int, SystemDiagram.Element> elements = diagram.GetElementsFromIDs(this.IDs);
                    for(int i = 0; i < this.IDs.Length; i++) {
                        elements[this.IDs[i]].setVelocity(isUndo ? this.oldIDs[i] : this.newVelID);
                    }
                    foreach (SystemDiagram.Edge edge in diagram.getEdges()) {
                        List<EdgeIDPackager>? edgeList = this.edges.GetValueOrDefault(edge.getSource());
                        if (edgeList != null) {
                            int target = edge.getTarget();
                            foreach (EdgeIDPackager packager in edgeList) {
                                if (packager.Target() == target) {
                                    edge.SetVelocity(isUndo ? packager.Velocity() : this.newVelID);
                                }
                            }
                        }
                    }
                    base.ExecuteUpdate(diagram, isUndo);
                }

                /// <summary>
                /// Gets the set of packager IDs 
                /// </summary>
                /// <returns></returns>
                public string[] GetEdgeIDs() {
                    return this.edgeIDs;
                }

                /// <summary>
                /// Gets the new velocity ID for this selection of elements.
                /// </summary>
                /// <returns>
                /// <c>this.newVelID</c>
                /// </returns>
                public int GetNewVelID() { return this.newVelID; }

                /// <summary>
                /// Gets the set of old velocity elementIDs for this selection of
                /// elements.
                /// </summary>
                /// <returns></returns>
                public int[] GetOldIDs() { return this.oldIDs; }

                /// <summary>
                /// Converts this <c>ChangeVelocity</c> to a printable format.
                /// </summary>
                /// <returns>This <c>ChangeVelocity</c> as a <c>string</c>.</returns>
                public override string ToString() {
                    return "[" + string.Join(", ", this.IDs) + "] ["
                        + string.Join(", ", this.edgeIDs)
                        + "] " + this.newVelID + " ["
                        + string.Join(", ", this.oldIDs) + "]";
                }

                /// <summary>
                /// Stores the source and target ID of an packager
                /// in a system diagram that can't be assigned 
                /// to a specified diagram yet.
                /// </summary>
                public class EdgeIDPackager {
                    // Stores the source and target IDs of a specified edge.
                    private readonly int source, target, velocity;
                    // Stores the previous velocity value for the edge 

                    /// <summary>
                    /// Creates a new <c>EdgeIDPackager</c>.
                    /// </summary>
                    /// <param name="JSONstring">The JSON string
                    /// to be parsed into the source and target IDs
                    /// of an packager.</param>
                    /// <param name="velocity">The previous velocity
                    /// for this edge.</param>
                    public EdgeIDPackager(string JSONstring, int velocity) {
                        this.velocity = velocity;
                        JObject obj = JObject.Parse(JSONstring);
                        this.source = obj.Value<int>("source");
                        this.target = obj.Value<int>("target");
                    }

                    /// <summary>
                    /// Gets the source element ID for this packager.
                    /// </summary>
                    /// <returns><c>this.source</c></returns>
                    public int Source() {
                        return this.source;
                    }

                    /// <summary>
                    /// Gets the target element ID for this packager.
                    /// </summary>
                    /// <returns><c>this.target</c></returns>
                    public int Target() {
                        return this.target;
                    }

                    /// <summary>
                    /// Gets the previous velocity assigned to this edge.
                    /// </summary>
                    /// <returns><c>this.velocity</c></returns>
                    public int Velocity() {
                        return this.velocity;
                    }
                }
            }
        }
    }
}
