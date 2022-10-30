/// <summary>
/// Namespace <c>EditorHelper</c> provides undo/redo functionality.
/// </summary>
namespace BoGLWeb {
    namespace EditorHelper {
        /// <summary>
        /// Class <c>EditionList</c> stores a save history for a set of objects.
        /// </summary>
        /// <typeparam name="Edit">
        /// The datatype stored in this <c>EditionList</c>.
        /// </typeparam> 
        public class EditionList<Edit> {
            /// Fields storing the head (front), tail (end), and cursor (user location) in this 
            /// <c>EditionList</c>.
            private Node<Edit>? head, cursor;
            private int size, index;

            /// <summary>
            /// Sets all default values for fields in this <c>EditionList</c>.
            /// </summary>
            public EditionList() {
                Clear();
            }

            /// <summary>
            /// Adds a new <c>Node</c> to the end of this <c>EditionList</c>.
            /// </summary>
            /// <param name="Unit">
            /// The <c>Unit</c> object to be added to this <c>EditionList</c>.
            /// </param>
            public void Add(Edit unit) {
                Node<Edit> node = new(unit);
                if (this.head == null || this.cursor == null) {
                    Assign(node, node);
                } else {
                    node.prev = this.cursor;
                    this.cursor.next = node;
                    this.cursor = this.cursor.next;
                }
                this.index++;
                this.size = this.index + 1;
            }

            /// <summary>
            /// Proceeds to the next value in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// <c>true</c> if there exists a next element, else <c>false</c>.
            /// </returns>
            public bool Next() {
                if (this.cursor == null) {
                    return false;
                }
                bool hasNext = this.cursor.next != null;
                if (hasNext) {
                    this.cursor = this.cursor.next;
                    this.index++;
                }
                return hasNext;
            }

            /// <summary>
            /// Returns to the previous value in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// <c>true</c> if there exists a previous element, else <c>false</c>.
            /// </returns>
            public bool Prev() {
                if (this.cursor == null) {
                    return false;
                }
                bool hasPrev = this.cursor.prev != null;
                if (hasPrev) {
                    this.cursor = this.cursor.prev;
                    this.index--;
                }
                return hasPrev;
            }

            /// <summary>
            /// Gets the current value in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// The <c>Unit</c> value stored by this <c>cursor</c>.
            /// </returns>
            public Edit? Get() {
                return (this.cursor == null) ? default : this.cursor.data;
            }

            /// <summary>Clears all data from this <c>EditionList</c>.</summary>
            public void Clear() {
                Assign(null, null);
                this.size = 0;
                this.index = -1;
            }

            /// <summary>
            /// Assigns all fields in this <c>EditionList</c> to specific values.
            /// </summary>
            /// <param name="head">
            /// The new <c>head</c> value.
            /// </param>
            /// <param name="cursor">
            /// The new <c>cursor</c> value.
            /// </param>
            private void Assign(Node<Edit>? head, Node<Edit>? cursor) {
                this.head = head;
                this.cursor = cursor;
            }
            
            /// <summary>
            /// Gets the index of this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// <c>this.index</c>
            /// </returns>
            public int Index() {
                return this.index;
            }

            /// <summary>
            /// Gets the size of this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// <c>this.size</c>
            /// </returns>
            public int Size() {
                return this.size;
            }

            /// <summary>
            ///  Returns an <c>Iterator</c> over the elements in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// The <c>Iterator</c>.
            /// </returns>
            public IEnumerable<Edit>? Iterator() {
                Node<Edit>? iterableCursor = this.head;
                while (iterableCursor != null) {
                    Edit edit = iterableCursor.data;
                    iterableCursor = iterableCursor.next;
                    yield return edit;
                }
            }
        }

        /// <summary>
        /// Class <c>Node</c> carries an element of data in an <c>EditionList</c>.
        /// </summary>
        /// <typeparam name="Data">
        /// The classtype supported in this <c>EditionList</c>.
        /// </typeparam>
        class Node<Data> {
            public Data data;
            public Node<Data>? next, prev;

            /// <summary>
            /// Creates a new <c>Node</c>.
            /// </summary>
            /// <param name="data">
            /// The new <c>Unit</c> element to be contained in this <c>Node</c>.
            /// </param>
            public Node(Data data) {
                this.data = data;
            }
        }
    }
}