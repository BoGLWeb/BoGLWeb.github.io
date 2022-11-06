using AVL_Prototype_1;

namespace BoGLWeb {
    /// <summary>
    /// Namespace <c>EditorHelper</c> provides undo/redo functionality.
    /// </summary>
    namespace EditorHelper {
        public class CanvasAlteration {
            private readonly BondGraphArc? arc;
            private readonly BondGraphElement? element;

            /// <summary>
            /// Creates a new <code>CanvasAlteration</code> object
            /// </summary>
            /// <param name="arc">
            /// The potential <code>Arc</code> object in this Alteration.
            /// </param>
            /// <param name="element">
            /// The potential <code>GraphElement</code> object added in this
            /// Alteration.
            /// </param>
            CanvasAlteration(BondGraphArc? arc, BondGraphElement? element) {
                this.arc = arc?.Copy();
                this.element = element?.Copy();
            }
        }
    }
}
