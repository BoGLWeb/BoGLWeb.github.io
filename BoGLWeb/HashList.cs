﻿using System.Collections;
using System.Text;

namespace BoGLWeb {
    namespace EditorHelper {
        /// <summary>
        /// Creates an accessible <code>HashSet</code>.
        /// </summary>
        /// <typeparam name="Data">
        /// The datatype of all objects in this <c>HashList</c>.
        /// </typeparam>
        public class HashList<Data> : IEnumerable<Data> where Data : notnull {
            private readonly Dictionary<Data, Cell<Data>> data;
            private Cell<Data>? head, tail;

            /// <summary>
            /// Creates a new, empty <c>HashList</c>.
            /// </summary>
            public HashList() {
                this.data = new();
            }

            /// <summary>
            /// Creates a new <c>HashList</c>
            /// </summary>
            /// <param name="data">
            /// The enumerable list of <code>Data</code> objects
            /// </param>
            public HashList(IEnumerable<Data> data) {
                this.data = new();
                foreach (Data item in data) {
                    Add(item);
                }
            }

            /// <summary>
            /// Adds a <code>Data</code> element to this <c>HashList</c>.
            /// </summary>
            /// <param name="item">
            /// The new <code>Data</code> object.
            /// </param>
            public void Add(Data item) {
                Cell<Data> cell = new(item);
                if (this.data.ContainsKey(item)) {
                    this.data[item].data = item;
                } else {
                    if (this.tail is Cell<Data> tail) {
                        cell.prev = tail;
                        tail.next = cell;
                        this.tail = cell;
                    } else {
                        this.head = cell;
                        this.tail = cell;
                    }
                    this.data.Add(item, cell);
                }
            }

            /// <summary>
            /// Gets the element of this <c>HashList</c> at a specific index.
            /// </summary>
            /// <param name="index">
            /// The index of the target element.
            /// </param>
            /// <returns>
            /// <code>this[index]</code>
            /// </returns>
            /// <exception cref="IndexOutOfRangeException">
            /// If this <c>HashList</c> does not contain an element at the
            /// specified index. This can occur in the following cases:
            ///     - The index is less than 0, which is too small
            ///     - The index is at least the size of this <c>HashList</c>
            ///     Note that these checks occur at the beginning of the method.
            ///     The second IndexOutOfRangeException will never be thrown.
            /// </exception>
            public Data this[int index] {
                get {
                    if (index < 0 || index >= this.data.Count) {
                        throw new IndexOutOfRangeException();
                    }
                    Cell<Data>? pointer = this.head;
                    int pointerIndex = 0;
                    while (pointerIndex < index) {
                        pointer = pointer?.next;
                        pointerIndex++;
                    }
                    if (pointer == null) {
                        throw new IndexOutOfRangeException();
                    }
                    return pointer.data;
                }
            }


            /// <summary>
            /// Removes an item from this <c>HashList</c>.
            /// </summary>
            /// <param name="item">
            /// The target item to be removed.
            /// </param>
            /// <returns>
            /// <code>true</code> if the item was successfully removed from 
            /// this <c>HashList</c>, else <code>false</code> if there
            /// was no such item originally.
            /// </returns>
            public bool Remove(Data item) {
                Cell<Data> cell = this.data[item];
                if (cell == null) {
                    return false;
                }
                if (cell.prev == null) {
                    this.head = cell.next;
                } else {
                    cell.prev.next = cell.next;
                }
                if (cell.next == null) {
                    this.tail = cell.prev;
                } else {
                    cell.next.prev = cell.prev;
                }
                cell.next = null;
                cell.prev = null;
                this.data.Remove(item);
                return true;
            }

            /// <summary>
            /// Clears all elements from this <c>HashList</c>.
            /// </summary>
            public void Clear() {
                this.head = null;
                this.tail = null;
                this.data.Clear();
            }

            /// <summary>
            /// Returns an <c>Enumerator</c> over the elements in this
            /// <c>HashList</c>.
            /// </summary>
            /// <returns>
            /// Each <code>Data</code> element, one at a time.
            /// </returns>
            public IEnumerator<Data> GetEnumerator() {
                Cell<Data>? pointer = this.head;
                while (pointer != null) {
                    yield return pointer.data;
                    pointer = pointer.next;
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
                Cell<Data>? pointer = this.head;
                while (pointer != null) {
                    yield return pointer.data;
                    pointer = pointer.next;
                }
            }

            /// <summary>
            /// Pulls any copy of a specified <code>Data</code> object from
            /// this <c>HashList</c>.
            /// </summary>
            /// <param name="item">
            /// The target item.
            /// </param>
            /// <returns>
            /// This copy of the target object.
            /// </returns>
            public Data Get(Data item) {
                return this.data[item].data;
            }

            /// <summary>
            /// Gets the size of this <c>HashList</c>.
            /// </summary>
            /// <returns>
            /// <code>this.data.Count</code>
            /// </returns>
            public int Size() {
                return this.data.Count;
            }

            /// <summary>
            /// Converts this <c>HashList</c> to a printable format.
            /// </summary>
            /// <returns>
            /// This <c>HashList</c> as a <code>String</code>.
            /// </returns>
            public override string ToString() {
                StringBuilder builder = new();
                builder.Append('[');
                String delimiter = "";
                foreach (Data data in this) {
                    builder.Append(delimiter).Append(data);
                    delimiter = ", ";
                }
                return builder.Append(']').ToString();
            }

            /// <summary>
            /// A single node of the <c>HashList</c> that 
            /// contains a single piece of information.
            /// </summary>
            /// <typeparam name="CellData">
            /// The information stored in this <code>Cell</code>
            /// </typeparam>
            private class Cell<CellData> {
                // Denotes the previous and next Cells
                public Cell<CellData>? next, prev;
                // Denotes the information held in this Cell
                public CellData data;

                /// <summary>
                /// Creates a new <code>Cell</code>
                /// </summary>
                /// <param name="data">
                /// The object stored in this <code>Cell</code>
                /// </param>
                public Cell(CellData data) {
                    this.data = data;
                }
            }
        }
    }
}
