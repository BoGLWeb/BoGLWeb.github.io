using System.Collections;
using System.Text;

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
        public class EditionList<Edit> : IEnumerable<Edit?> {
            /// Fields storing the head (front), tail (end), and pointer (user location) in this 
            /// <c>EditionList</c>.
            private Node<Edit?> head, pointer;
            private int size, index;

            /// <summary>
            /// Sets all default values for fields in this <c>EditionList</c>.
            /// </summary>
            public EditionList() {
                Node<Edit?> node = new(default);
                this.head = node;
                this.pointer = node;
                this.size = 1;
                this.index = 0;
            }

            /// <summary>
            /// Adds a new <c>Node</c> to the end of this <c>EditionList</c>.
            /// </summary>
            /// <param name="Unit">
            /// The <c>Unit</c> object to be added to this <c>EditionList</c>.
            /// </param>
            public void Add(Edit? unit) {
                Node<Edit?> node = new(unit) {
                    prev = this.pointer
                };
                this.pointer.next = node;
                this.pointer = this.pointer.next;
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
                bool hasNext = this.pointer.next != null;
                if (this.pointer.next != null) {
                    this.pointer = this.pointer.next;
                    this.index++;
                }
                return hasNext;
            }

            /// <summary>
            /// Determines whether this <c>EditionList</c> has a next
            /// element.
            /// </summary>
            /// <returns><c>true</c> if there is a next element,
            /// else <c>false</c></returns>
            public bool HasNext() {
                return this.pointer.next != null;
            }

            /// <summary>
            /// Returns to the previous value in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// <c>true</c> if there exists a previous element, else <c>false</c>.
            /// </returns>
            public bool Prev() {
                bool hasPrev = this.pointer.prev != null;
                if (this.pointer.prev != null) {
                    this.pointer = this.pointer.prev;
                    this.index--;
                }
                return hasPrev;
            }

            /// <summary>
            /// Determines whether this <c>EditionList</c> has a next
            /// element.
            /// </summary>
            /// <returns><c>true</c> if there is a next element,
            /// else <c>false</c></returns>
            public bool HasPrev() {
                return this.pointer.data != null;
            }

            /// <summary>
            /// Gets the current value in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// The <c>Unit</c> value stored by this <c>pointer</c>.
            /// </returns>
            public Edit? Get() {
                return (this.pointer == null) ? default : this.pointer.data;
            }

            /// <summary>Clears all data from this <c>EditionList</c>.</summary>
            public void Clear() {
                Node<Edit?> node = new(default);
                Assign(node, node);
                this.size = 0;
                this.index = -1;
            }

            /// <summary>
            /// Assigns all fields in this <c>EditionList</c> to specific values.
            /// </summary>
            /// <param name="head">
            /// The new <c>head</c> value.
            /// </param>
            /// <param name="pointer">
            /// The new <c>pointer</c> value.
            /// </param>
            private void Assign(Node<Edit?> head, Node<Edit?> pointer) {
                this.head = head;
                this.pointer = pointer;
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
            ///  Returns an <c>Enumerator</c> over the elements in this <c>EditionList</c>.
            /// </summary>
            /// <returns>
            /// The <c>Enumerator</c>.
            /// </returns>
            public IEnumerator<Edit?> GetEnumerator() {
                Node<Edit?>? iterablePointer = this.head;
                while (iterablePointer != null) {
                    Edit? edit = iterablePointer.data;
                    iterablePointer = iterablePointer.next;
                    yield return edit;
                }
            }

            /// <summary>
            /// Returns an <c>Enumerator</c> over the elements in this
            /// <c>HashList</c>.
            /// </summary>
            /// <returns>
            /// Each <code>Data</code> element, one at a time.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator() {
                Node<Edit?>? iterablePointer = this.head;
                while (iterablePointer != null) {
                    Edit? edit = iterablePointer.data;
                    iterablePointer = iterablePointer.next;
                    yield return edit;
                }
            }

            /// <summary>
            /// Converts this <c>EditionList</c> to a printable
            /// format.
            /// </summary>
            /// <returns>
            /// This <c>EditionList</c> as a String
            /// </returns>
            public override String ToString() {
                StringBuilder builder = new();
                int index = 0;
                foreach (Edit? edit in this) {
                    char delimiter = (index == this.index) ? '*' : ' ';
                    builder.Append(delimiter).Append(edit).Append(delimiter);
                    index++;
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Class <c>Node</c> carries an element of data in an <c>EditionList</c>.
        /// </summary>
        /// <typeparam name="Data">
        /// The classtype supported in this <c>EditionList</c>.
        /// </typeparam>
        class Node<Data> {
            public Data? data;
            public Node<Data?>? next, prev;

            /// <summary>
            /// Creates a new <c>Node</c>.
            /// </summary>
            /// <param name="data">
            /// The new <c>Data</c> element to be contained in this <c>Node</c>.
            /// </param>
            public Node(Data? data) {
                this.data = data;
            }
        }
    }
}