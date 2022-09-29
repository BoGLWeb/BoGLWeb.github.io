using System;
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

        public BondGraphArc(GraphElement element1, GraphElement element2, int arrowDir, int causalDir)
        {
            this.element1 = element1;
            this.element2 = element2;

            this.arrowDir = arrowDir;
            this.causalDir = causalDir;

            // Add this arc to the elements' list of connections
            element1.connections.Add(this);
            element2.connections.Add(this);

            velocity = null;

            // Add this arc to the graph's list of arcs
            graph.arcs.Add(this);
        }

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
