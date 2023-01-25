namespace BoGLWeb {
    namespace EditorHelper {
        public class UndoRedoHandler {
            // Stores 
            public static readonly UndoRedoHandler undoRedoHandler = new UndoRedoHandler();
            // Stores the system diagram edit stack.
            private readonly EditionList<CanvasChange> systemStack;
            // Stores the unsimplified bond graph edit stack.
            private readonly EditionList<CanvasChange> unsimpStack;
            // Stores the simplified bond graph edit stack.
            private readonly EditionList<CanvasChange> simpleStack;
            // Stores the causal bond graph edit stack.
            private readonly EditionList<CanvasChange> causalStack;

            /// <summary>
            /// Creates a new UndoRedoHandler.
            /// </summary>
            public UndoRedoHandler() {
                systemStack = new EditionList<CanvasChange>();
                unsimpStack = new EditionList<CanvasChange>();
                simpleStack = new EditionList<CanvasChange>();
                causalStack = new EditionList<CanvasChange>();
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
            public void AddEdit(CanvasChange edit, CanvasTab tab) {
                EditionList<CanvasChange> list = GetStackFromTab(tab);
                list.Add(edit);
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
            /// Generates all bond graphs from a system diagram and 
            /// clears all graph EditionLists
            /// </summary>
            public void GenerateBondGraph() {
                unsimpStack.Clear();
                simpleStack.Clear();
                causalStack.Clear();
            }
        }

        /// <summary>
        /// <code>CanvasTab</code> stores all graph types in an enum
        /// for stack identification in the undo/redo process
        /// </summary>
        public enum CanvasTab {
            SYSTEM_DIAGRAM              = 0,
            UNSIMPLIFIED_BOND_GRAPH     = 1,
            SIMPLIFIED_BOND_GRAPH       = 2,
            CAUSAL_BOND_GRAPH           = 3
        }
    }
}
