namespace BoGLWeb {
    /// <summary>
    /// Namespace <c>EditorHelper</c> provides undo/redo functionality.
    /// </summary>
    namespace EditorHelper {
        public class CanvasAlteration {
            public CanvasAlteration() {
            }
        }

        /// <summary>
        /// Determines a type of edit for the BoGL Web canvas.
        /// </summary>
        public enum CanvasAlterationType {
            ADDITION      = 0b_0000_0001,
            DELETION      = 0b_1111_1111,
            CONNECTION    = 0b_0000_0010,
            DISCONNECTION = 0b_1111_1110,
            MOVEMENT      = 0b_0000_0000
        }
    }
}
