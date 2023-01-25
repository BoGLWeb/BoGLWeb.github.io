using AntDesign.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace BoGLWeb {
    /// <summary>
    /// Namespace <c>EditorHelper</c> provides undo/redo functionality.
    /// </summary>
    namespace EditorHelper {
        /// <summary>
        /// Keeps track of changes made to the canvas.
        /// </summary>
        public class CanvasChange {
            // Stores the elementIDs of the respective selected elements.

            public new CanvasChange GetType() {
                return this.GetType();
            }

            // Stores the IDs of the respective selected elements.
            public readonly int[] IDs;

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
            /// The input diagram.
            /// </param>
            /// <param name="isUndo">
            /// <c>true</c> if the action executing this update was an 'undo' call,
            /// else <c>false</c> if the action was a 'redo' call.
            /// </param>
            public virtual void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
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
            /// system diagram.
            /// </summary>
            public class AddSelection : CanvasChange {
                // Stores the previous group of selected elements
                private readonly int[] prevSelectedElements;
                // Stores the previous group of selected edges
                private readonly string[] prevSelectedEdges;

                // TODO: fix comment
                /// <summary> 
                /// Creates a new AddSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of elementIDs in the selection.
                /// </param>
                /// <param name="prevSelectedElements">The JSON string carrying info about
                /// the added elements.
                /// </param>
                /// <param name="prevSelectedEdges">The JSON string carrying info about
                /// the added elements.
                /// </param>
                public AddSelection(int[] IDs, int[] prevSelectedElements, string[] prevSelectedEdges) : base(IDs) {
                    this.prevSelectedElements = prevSelectedElements;
                    this.prevSelectedEdges = prevSelectedEdges;
                }

                /// <summary>
                /// Executes the update made to the system diagram during a 
                /// <c>AddSelection</c> action.
                /// </summary>
                /// <param name="diagram">The system diagram.</param>
                /// <param name="isUndo"><c>true</c> if this method was called during
                /// the 'undo' action, else <c>false</c> if it was called during the
                /// 'redo' action.</param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    //TODO: leave empty for now
                }

                /// <summary>
                /// Gets the previous element selection from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.prevSelectedElements</returns>
                public int[] GetPrevSelectedElements() {
                    return this.prevSelectedElements;
                }

                /// <summary>
                /// Gets the previous edge selection from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>this.prevSelectedEdges</returns>
                public string[] GetPrevSelectedEdges() {
                    return this.prevSelectedEdges;
                }
            }

            /// <summary>
            /// Stores a change made when the user deletes a group of items from the
            /// system diagram.
            /// </summary>
            public class DeleteSelection : CanvasChange {
                // The JSON objects storing the deleted elements
                private readonly string[] deleted;

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
                    this.deleted = deleted;
                }

                /// <summary>
                /// Executes the update made to the system diagram during a 
                /// <c>DeleteSelection</c> action.
                /// </summary>
                /// <param name="diagram">The system diagram.</param>
                /// <param name="isUndo"><c>true</c> if this method was called during
                /// the 'undo' action, else <c>false</c> if it was called during the
                /// 'redo' action.</param>
                public override void ExecuteUpdate(SystemDiagram diagram, bool isUndo) {
                    //TODO: leave empty for now
                }

                /// <summary>
                /// Gets the stored JSON string from this <c>DeleteSelection</c>.
                /// </summary>
                /// <returns>The JSON string object</returns>
                public string[] GetDeletedJSONElements() {
                    return this.deleted;
                }
            }

            /// <summary>
            /// Stores a change made when the selection changes. This 
            /// <c>CanvasChange</c> does not update the backend.
            /// </summary>
            public class ChangeSelection : CanvasChange {
                // Stores the array of elementIDs for the new selection of elements.
                private readonly int[] newElementIDS;
                //Stores the array of elementIDs for the old selected edges.
                private readonly string[] oldEdgeIDs;
                //Stores the array of elementIDs for the new selected edges.
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
                /// Updates the system diagram to include changes made via a
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
                /// Updates the system diagram to include changes made via a
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
                /// Updates the system diagram to include changes made via a
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
            }
        }
    }
}
