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
            // Stores the IDs of the respective selected elements.
            public readonly int[] IDs;

            /// <summary>
            /// Creates a new Can
            /// </summary>
            /// <param name="IDs">
            /// The array of IDs denoting the selected 
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
            /// Gets the set of IDs for this CanvasChange.
            /// </summary>
            /// <returns>
            /// <c>this.IDs</c>
            /// </returns>
            public int[] GetIDs() {
                return this.IDs;
            }

            /// <summary>
            /// Stores a change made when the user adds a group of items to the
            /// system diagram.
            /// </summary>
            public class AddSelection : CanvasChange {
                // The JSON object storing the added elements
                private readonly string json;

                /// <summary>
                /// Creates a new AddSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of IDs in the selection.
                /// </param>
                /// <param name="json">The JSON string carrying info about
                /// the added elements.
                /// </param>
                public AddSelection(int[] IDs, string json) : base(IDs) {
                    this.json = json;
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
                /// Gets the stored JSON string from this <c>AddSelection</c>.
                /// </summary>
                /// <returns>The JSON string object</returns>
                public string GetJson() {
                    return this.json;
                }
            }

            /// <summary>
            /// Stores a change made when the user deletes a group of items from the
            /// system diagram.
            /// </summary>
            public class DeleteSelection : CanvasChange {
                // The JSON object storing the added elements
                private readonly string json;

                /// <summary>
                /// Creates a new DeleteSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of IDs in the selection.
                /// </param>
                /// <param name="json">The JSON string carrying info about
                /// the added elements.
                /// </param>
                public DeleteSelection(int[] IDs, string json) : base(IDs) {
                    this.json = json;
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
                public string GetJson() {
                    return this.json;
                }
            }

            /// <summary>
            /// Stores a change made when the selection changes. This 
            /// <c>CanvasChange</c> does not update the backend.
            /// </summary>
            public class ChangeSelection : CanvasChange {
                // Stores the array of IDs for the new selection of elements.
                private readonly int[] newIDS;

                /// <summary>
                /// Creates a new ChangeSelection CanvasChange.
                /// </summary>
                /// <param name="oldIDs">
                /// The set of IDs in the selection.
                /// </param>
                public ChangeSelection(int[] oldIDs, int[] newIDs) : base(oldIDs) {
                    this.newIDS = newIDs;
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
                /// The list of selected IDs.
                /// </returns>
                public int[] GetNewIDs() {
                    return this.newIDS;
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
                /// The set of IDs in the selection.
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
                /// The array of IDs of the elements in the selection.
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
                /// Gets the applicable translation applied to this <c>MoveSelection</c>.
                /// </summary>
                /// <returns>
                /// An array containing <c>this.xOffset</c> and <c>this.yOffset</c>.
                /// </returns>
                public double[] GetTranslation() {
                    return new double[] {this.xOffset, this.yOffset};
                }
            }

            /// <summary>
            /// Stores a change in velocity made to a selection of elements.
            /// </summary>
            public class ChangeVelocity : CanvasChange {
                // Stores the ID of the new velocity.
                private readonly int newID;
                // Stores the IDs of the previous velocities for each
                // target element.
                private readonly int[] oldIDs;

                /// <summary>
                /// Creates a new ChangeSelection CanvasChange.
                /// </summary>
                /// <param name="IDs">
                /// The set of IDs in the selection.
                /// </param>
                public ChangeVelocity(int[] IDs, int newID, int[] oldIDs) : base(IDs) {
                    this.newID = newID;
                    this.oldIDs = oldIDs;
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
                        elements[this.IDs[i]].setVelocity(isUndo ? this.oldIDs[i] : this.newID);
                    }
                }

                /// <summary>
                /// Gets the new velocity ID for this selection of elements.
                /// </summary>
                /// <returns>
                /// <c>this.newID</c>
                /// </returns>
                public int GetNewID() { return this.newID; }

                /// <summary>
                /// Gets the set of old velocity IDs for this selection of
                /// elements.
                /// </summary>
                /// <returns></returns>
                public int[] GetOldIDs() { return this.oldIDs; }
            }
        }
    }
}
