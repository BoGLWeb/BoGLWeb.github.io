namespace BoGLWeb {
    namespace EditorHelper {
        public class UndoRedoHandler {
            private readonly EditionList<CanvasAlteration> systemStack;
            private readonly EditionList<CanvasAlteration> unsimpStack;
            private readonly EditionList<CanvasAlteration> simpleStack;
            private readonly EditionList<CanvasAlteration> causalStack;

            /// <summary>
            /// Creates a new UndoRedoHandler.
            /// </summary>
            public UndoRedoHandler() {
                systemStack = new EditionList<CanvasAlteration>();
                unsimpStack = new EditionList<CanvasAlteration>();
                simpleStack = new EditionList<CanvasAlteration>();
                causalStack = new EditionList<CanvasAlteration>();
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
            public void AddEdit(CanvasAlteration edit, CanvasTab tab) {
                EditionList<CanvasAlteration> list = GetStackFromTab(tab);
                list.Add(edit);
            }

            /// <summary>
            /// Finds the target <code>EditionList</code> for a specific
            /// operation.
            /// </summary>
            /// <param name="tab">The enum representing the tab.</param>
            /// <returns>
            /// The <code>EditionList</code> representing the edit history
            /// for the target tab.
            /// </returns>
            /// <exception cref="Exception">
            /// If an invalid CanvasTab object is provided as a parameter.
            /// </exception>
            private EditionList<CanvasAlteration> GetStackFromTab(CanvasTab tab) {
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
