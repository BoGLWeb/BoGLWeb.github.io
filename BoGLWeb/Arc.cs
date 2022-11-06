using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace AVL_Prototype_1
{
    public class Arc
    {
        public GraphElement element1;
        public GraphElement element2;

        public int? velocity;
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
        }

        public Arc(Graph graph, GraphElement element1, GraphElement element2)
        {
            this.element1 = element1;
            this.element2 = element2;
            this.graph = graph;

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
        /// <returns>
        /// The copy
        /// </returns>
        public virtual Arc Copy() {
            return new(this.graph, this.element1, this.element2) {
                velocity = this.velocity,
                deleted = this.deleted
            };
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

            return sb.ToString();
        }

        public string serialize()
        {
            return serialize(graph.elements);
        }

    }
}
