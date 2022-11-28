

namespace BoGLWeb {
    /// <summary>
    /// Namespace <c>EditorHelper</c> provides undo/redo functionality.
    /// </summary>
    namespace EditorHelper {
        public class CanvasChange {
            // Stores the before and after copies of an object. 
            private readonly object? copy1, copy2;

            /// <summary>
            /// Creates a new <c>CanvasChange</c> with the before and after
            /// copies of an object. Note that these could appear in either
            /// order.
            /// </summary>
            /// <param name="copy1">The first stored copy.</param>
            /// <param name="copy2">The second stored copy.</param>
            public CanvasChange(object? copy1, object? copy2) {
                this.copy1 = copy1;
                this.copy2 = copy2;
            }

            /// <summary>
            /// Provides the component copies stored by this <c>CanvasChange</c>.
            /// </summary>
            /// <returns>
            /// <c>this.copy1</c> and <c>this.copy2</c>
            /// </returns>
            public object?[] GetCopies() {
                return new object?[] {this.copy1, this.copy2};
            }
        }
    }
}
