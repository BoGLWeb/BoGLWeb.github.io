using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AVL_Prototype_1
{
    class BondGraphArc : Arc
    {
        private static double arrowLength = 20;
        private static double arrowAngleOffset = Math.PI / 5;
        private static double causalLength = 20;

        public int arrowDir;
        public int causalDir;

        /// <summary>
        /// Creates a new <code>BondGraphArc</code>
        /// </summary>
        /// <param name="element1">
        /// The source endpoint element
        /// </param>
        /// <param name="element2">
        /// The destination endpoint element
        /// </param>
        /// <param name="arrowDir">
        /// The direction of the bond arrow
        /// </param>
        /// <param name="causalDir">
        /// The direction of causality
        /// </param>
        public BondGraphArc(GraphElement element1, GraphElement element2, int arrowDir, int causalDir)
        {
            this.element1 = element1;
            this.element2 = element2;

            this.arrowDir = arrowDir;
            this.causalDir = causalDir;

            AssignID(0, true);

            // Add this arc to the elements' list of connections
            element1.connections.Add(this);
            element2.connections.Add(this);

            velocity = null;

            // Add this arc to the graph's list of arcs
            graph.arcs.Add(this);
        }

        /// <summary>
        /// Creates a copy of this <code>BondGraphArc</code>
        /// </summary>
        /// <param name="isDistinct">
        /// <code>true</code> if the copy should have its own ID, else
        /// <code>false</code>
        /// </param>
        /// <returns>
        /// The copy
        /// </returns>
        public override BondGraphArc Copy(bool isDistinct) {
            BondGraphArc arc = new(this.element1, this.element2, this.arrowDir, this.causalDir) {
                deleted = this.deleted,
                velocity = this.velocity,
                graph = this.graph
            };
            arc.AssignID(this.ID, isDistinct);
            return arc;
        }

        /// <summary>
        /// Deletes this <code>BondGraphArc</code> from the graph.
        /// </summary>
        public override void delete()
        {
            element1.connections.Remove(this);
            element2.connections.Remove(this);

            element1 = null;
            element2 = null;

            graph.arcs.Remove(this);

            deleted = true;
        }
    }
}
