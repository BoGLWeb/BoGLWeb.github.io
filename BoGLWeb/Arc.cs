using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace AVL_Prototype_1
{
    public class Arc
    {
        public GraphElement element1;
        public GraphElement element2;

        public int? velocity;
        public static int totalID = 0; // Gives new IDs to any new Arc
        public int? ID;                // Tracks the current ID for this Arc
        public bool canHaveVelocity
        {
            get => velocity != null;
        }

        public bool deleted = false;

        protected Graph graph;

        public bool selected
        {
            get => graph.selectedArcs.Contains(this);
            set
            {
                if (deleted)
                    return;

            }
        }

        protected Arc()
        {
            // Default constructor...
            AssignID(0, true);
        }

        public Arc(Graph graph, GraphElement element1, GraphElement element2)
        {
            this.element1 = element1;
            this.element2 = element2;
            this.graph = graph;

            AssignID(0, true);

            // Add this arc to the elements' list of connections
            element1.connections.Add(this);
            element2.connections.Add(this);

            // Check to see if this arc supports velocity
            if (element1.modifiers.ContainsKey(Graph.ModifierType.VELOCITY) && element2.modifiers.ContainsKey(Graph.ModifierType.VELOCITY))
                velocity = 0;
            else
                velocity = null;

            // Add this arc to the graph's list of arcs
            graph.arcs.Add(this);
        }

        /// <summary>
        /// Creates a copy of this <code>Arc</code>
        /// </summary>
        /// <param name="isDistinct">
        /// <code>true</code> if this Arc should have its own ID, else
        /// <code>false</code>
        /// </param>
        /// <returns>
        /// The copy
        /// </returns>
        public virtual Arc Copy(bool isDistinct) {
            Arc copy = new(this.graph, this.element1, this.element2) {
                velocity = this.velocity,
                deleted = this.deleted
            };
            copy.AssignID(this.ID, isDistinct);
            return copy;
        }

        /// <summary>
        /// Assigns an ID value to this <code>Arc</code>
        /// </summary>
        /// <param name="ID">
        /// The target ID value
        /// </param>
        /// <param name="isDistinct">
        /// <code>true</code> if the ID should be unique (e.g. for clipboard)
        /// or if it should match the ID of the copied object (e.g. for undo/redo)
        /// </param>
        public void AssignID(int? ID, bool isDistinct) {
            if (this.ID == null || isDistinct) {
                this.ID = (totalID++);
            } else {
                this.ID = ID;
            }
        }

        // Deletes all WPF controls and references to this arc
        public virtual void delete()
        {
            element1.connections.Remove(this);
            element2.connections.Remove(this);

            element1 = null;
            element2 = null;

            graph.arcs.Remove(this);

            deleted = true;
        }
        public string serialize(List<GraphElement> relativeList)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");

            // Put in the two element indicies
            sb.AppendLine("\telement1 " + relativeList.IndexOf(element1));
            sb.AppendLine("\telement2 " + relativeList.IndexOf(element2));

            if (canHaveVelocity && velocity > 0)
                sb.AppendLine("\tvelocity " + velocity);

            sb.AppendLine("}");

            return sb.ToString(); // Add serializing
        }

        public string serialize()
        {
            return serialize(graph.elements);
        }

    }
}
