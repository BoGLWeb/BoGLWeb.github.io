namespace BoGLWeb {
    namespace EditorHelper {
        public class UndoRedoHandler {
            // Stores the UndoRadoHandler used.
            public static readonly UndoRedoHandler undoRedoHandler = new(new(), new(), new(), new());
            // Stores the edit stacks.
            private readonly EditionList<CanvasChange> systemStack, unsimpStack, simpleStack, causalStack;
            // Stores the system diagram.
            private readonly SystemDiagram systemDiagram;
            // Stores the bond graphs.
            private readonly BondGraph unsimpGraph, simpleGraph, causalGraph;

            /// <summary>
            /// Creates a new <c>UndoRedoHandler</c>.
            /// </summary>
            public UndoRedoHandler(SystemDiagram diagram, BondGraph unsimpGraph, BondGraph simpleGraph, BondGraph causalGraph) {
                this.systemStack = new EditionList<CanvasChange>();
                this.unsimpStack = new EditionList<CanvasChange>();
                this.simpleStack = new EditionList<CanvasChange>();
                this.causalStack = new EditionList<CanvasChange>();
                this.systemDiagram = diagram;
                this.unsimpGraph = unsimpGraph;
                this.simpleGraph = simpleGraph;
                this.causalGraph = causalGraph;
            }

            /// <summary>
            /// Adds a new version of the canvas into this Handler.
            /// </summary>
            /// <param name="edit">
            /// The change made to the canvas.
            /// </param>
            /// <param name="tab">
            /// The tab where the edit occurred.
            /// </param>
            public void AddEdit(CanvasChange edit, int tab) {
                GetStackFromTab(GetTab(tab)).Add(edit);
            }

            /// <summary>
            /// Gets the CanvasChange from this 
            /// </summary>
            /// <param name="tab"></param>
            /// <returns></returns>
            public CanvasChange? GetChange(int tab) {
                return GetStackFromTab(GetTab(tab)).Get();
            }

            /// <summary>
            /// Determines whether a specific undo or redo action is
            /// possible on a target <c>EditionList</c>.
            /// </summary>
            /// <param name="tab">An int denoting which tab is 
            /// being queried.</param>
            /// <param name="isUndo"><c>true</c> if the 'undo'
            /// action would be attempted, else <c>false</c> if
            /// the 'redo' action would be called.</param>
            /// <returns><c>true</c> if the requested action is
            /// possible because there is more room in the stack,
            /// else <c>false</c> because the requested action is 
            /// impossible.</returns>
            public bool CanDo(int tab, bool isUndo) {
                return CanDo(GetStackFromTab(GetTab(tab)), isUndo);
            }

            /// <summary>
            /// Determines whether a specific undo or redo action is
            /// possible on a target <c>EditionList</c>.
            /// </summary>
            /// <param name="list">The target <c>EditionList</c>.</param>
            /// <param name="isUndo"><c>true</c> if the 'undo'
            /// action would be attempted, else <c>false</c> if
            /// the 'redo' action would be called.</param>
            /// <returns><c>true</c> if the requested action is
            /// possible because there is more room in the stack,
            /// else <c>false</c> because the requested action is 
            /// impossible.</returns>
            private static bool CanDo(EditionList<CanvasChange> list, bool isUndo) {
                return (isUndo & list.HasPrev()) | (!isUndo & list.HasNext());
            }

            /// <summary>
            /// Performs the undo or redo action on a graph or diagram in
            /// the system.
            /// </summary>
            /// <param name="tab">the int corresponding to the requested 
            /// tab</param>
            /// <param name="isUndo"><c>true</c> if the 'undo' action was 
            /// called, else <c>false</c> if the 'redo' action was called
            /// </param>
            public void Do(int tab, bool isUndo) {
                CanvasTab canvasTab = GetTab(tab);
                EditionList<CanvasChange> edits = GetStackFromTab(canvasTab);
                if (CanDo(edits, isUndo)) {
                    if (!isUndo) {
                        edits.Next();
                    }
                    if (canvasTab == CanvasTab.SYSTEM_DIAGRAM) {
                        edits.Get()?.ExecuteUpdate(this.systemDiagram, isUndo);
                    } else {
                        edits.Get()?.ExecuteUpdate(GetBondGraph(canvasTab), isUndo);
                    }
                    if (isUndo) {
                        edits.Prev();
                    }
                }
            }

            /// <summary>
            /// Finds the target <c>EditionList</c> for a specific
            /// operation.
            /// </summary>
            /// <param name="tab">The enum representing the tab.</param>
            /// <returns>
            /// The <c>EditionList</c> representing the edit history
            /// for the target tab.
            /// </returns>
            /// <exception cref="Exception">
            /// If an invalid CanvasTab object is provided as a parameter.
            /// </exception>
            private EditionList<CanvasChange> GetStackFromTab(CanvasTab tab) {
                return tab switch {
                    CanvasTab.SYSTEM_DIAGRAM => systemStack,
                    CanvasTab.UNSIMPLIFIED_BOND_GRAPH => unsimpStack,
                    CanvasTab.SIMPLIFIED_BOND_GRAPH => simpleStack,
                    CanvasTab.CAUSAL_BOND_GRAPH => causalStack,
                    _ => throw new Exception("Improper CanvasTab object."),
                };
            }

            /// <summary>
            /// Clears the system diagram stack.
            /// </summary>
            public void ClearSystemDiagramEditHistory() {
                this.systemStack.Clear();
            }

            /// <summary>
            /// Generates all bond graphs from a system diagram and 
            /// clears all graph EditionLists
            /// </summary>
            public void ClearBondGraphEditHistories() {
                unsimpStack.Clear();
                simpleStack.Clear();
                causalStack.Clear();
            }

            /// <summary>
            /// Gets the system diagram from this <c>UndoRedoHandler</c>.
            /// </summary>
            /// <returns>this.systemDiagram</returns>
            public SystemDiagram GetSystemDiagram() {
                return this.systemDiagram;
            }

            /// <summary>
            /// Gets the appropriate bond graph from this 
            /// <c>UndoRedoHandler</c>.
            /// </summary>
            /// <param name="tab">The int corresponding to a 
            /// specific tab.</param>
            /// <returns>The bond graph</returns>
            public BondGraph GetBondGraph(int tab) {
                return GetBondGraph(GetTab(tab));
            }

            /// <summary>
            /// Gets the appropriate bond graph from this 
            /// <c>UndoRedoHandler</c>.
            /// </summary>
            /// <param name="tab">The desired CanvasTab.</param>
            /// <returns>The bond graph</returns>
            private BondGraph GetBondGraph(CanvasTab tab) {
                return tab switch {
                    CanvasTab.UNSIMPLIFIED_BOND_GRAPH => unsimpGraph,
                    CanvasTab.SIMPLIFIED_BOND_GRAPH => simpleGraph,
                    CanvasTab.CAUSAL_BOND_GRAPH => causalGraph,
                    _ => throw new Exception("Not a bond graph option")
                };
            }

            /// <summary>
            /// Gets the <c>CanvasTab</c> corresponding to a
            /// specified int.
            /// </summary>
            /// <param name="tab">the int</param>
            /// <returns>the correct <c>CanvasTab</c> according to the
            /// enum settings</returns>
            private static CanvasTab GetTab(int tab) {
                return tab switch {
                    (int) CanvasTab.SYSTEM_DIAGRAM => CanvasTab.SYSTEM_DIAGRAM,
                    (int) CanvasTab.UNSIMPLIFIED_BOND_GRAPH => CanvasTab.UNSIMPLIFIED_BOND_GRAPH,
                    (int) CanvasTab.SIMPLIFIED_BOND_GRAPH => CanvasTab.SIMPLIFIED_BOND_GRAPH,
                    (int) CanvasTab.CAUSAL_BOND_GRAPH => CanvasTab.CAUSAL_BOND_GRAPH,
                    _ => throw new Exception("Improper CanvasTab object.")
                };
            }
        }

        /// <summary>
        /// <code>CanvasTab</code> stores all graph types in an enum
        /// for stack identification in the undo/redo process
        /// </summary>
        public enum CanvasTab {
            SYSTEM_DIAGRAM = 1,
            UNSIMPLIFIED_BOND_GRAPH = 2,
            SIMPLIFIED_BOND_GRAPH = 3,
            CAUSAL_BOND_GRAPH = 4
        }
    }
}
