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
                public AddSelection(int[] IDs, string[] newObjects, string[] prevSelectedEdges) : base(IDs) {
                    this.newObjects = newObjects;
                    this.prevSelectedEdges = prevSelectedEdges;
                    SystemDiagram.Packager packager = new(newObjects);
                    this.newElements = packager.GetElements();
                    this.newEdges = packager.GetSourceEdges();
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
                    base.ExecuteUpdate(diagram, isUndo);
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
                            if (this.newEdges.ContainsKey(edge.GetID())) {
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
                }

                /// <summary>
                /// Gets the JSON array of elements from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.newObjects</returns>
                public string[] GetNewObjects() {
                    return this.newObjects;
                }

                /// <summary>
                /// Gets the previous edge selection from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.prevSelectedEdges</returns>
                public string[] GetPrevSelectedEdges() {
                    return this.prevSelectedEdges;
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
                private readonly List<string> deleted;
                // The output array storing the deleted elements
                private string[]? deletedArray;
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
                public DeleteSelection(int[] IDs, string[] deleted) : base(IDs) {
                    SystemDiagram.Packager packager = new(deleted);
                    this.oldElements = packager.GetElements();
                    this.oldEdgesBySource = packager.GetSourceEdges();
                    this.oldEdgesByTarget = packager.GetTargetEdges();
                    this.deleted = new(deleted);
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
                    base.ExecuteUpdate(diagram, isUndo);
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
                                this.deleted.Add(edge.SerializeToJSON());
                            } else if (this.oldElements.ContainsKey(target)) {
                                edgeIterator.Remove();
                                this.oldEdgesByTarget.Add(target, new List<SystemDiagram.Edge>() { edge });
                                this.deleted.Add(edge.SerializeToJSON());
                            }
                        }
                    }
                }

                /// <summary>
                /// Gets the stored JSON string from this <c>DeleteSelection</c>.
                /// </summary>
                /// <returns>The JSON string object</returns>
                public string[] GetDeletedJSONElements() {
                    if (this.deletedArray == null) {
                        string[] JSONstrings = new string[this.deleted.Count];
                        int index = 0;
                        foreach (string str in this.deleted) {
                            JSONstrings[index++] = str;
                        }
                        this.deletedArray = JSONstrings;
                    }
                    Console.WriteLine(string.Join(", ", this.deleted));
                    return this.deletedArray;
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
                /// Gets the old edge elementIDs
                /// </summary>
                /// <returns><c>this.oldEdgeIDs</c></returns>
                public string[] GetOldEdgeIDs() {
                    return this.oldEdgeIDs;
                }

                /// <summary>
                /// Gets the new edge elementIDs
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
                    base.ExecuteUpdate(diagram, isUndo);
                    Dictionary<int, SystemDiagram.Element> elements = diagram.GetElementsFromIDs(this.IDs);
                    for (int i = 0; i < this.IDs.Length; i++) {
                        SystemDiagram.Element element = elements[this.IDs[i]];
                        element.getModifiers().Remove(this.modID);
                        if ((isUndo & this.current[i]) | ((!isUndo) & toggle)) {
                            element.addModifier(this.modID);
                        }
                    }
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
                    base.ExecuteUpdate(diagram, isUndo);
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
                private readonly string[] edgeIDs;
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
                    base.ExecuteUpdate(diagram, isUndo);
                    Dictionary<int, SystemDiagram.Element> elements = diagram.GetElementsFromIDs(this.IDs);
                    for(int i = 0; i < this.IDs.Length; i++) {
                        elements[this.IDs[i]].setVelocity(isUndo ? this.oldIDs[i] : this.newVelID);
                    }
                }

                /// <summary>
                /// Gets the set of edge IDs 
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

                public override string ToString() {
                    return EditStackHandler.Stringify(this.IDs) +
                        EditStackHandler.Stringify(this.edgeIDs) +
                        " " + this.newVelID + " " +
                        EditStackHandler.Stringify(this.oldIDs);
                }
            }
        }
    }
}
