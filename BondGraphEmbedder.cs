using System.Reflection.Metadata.Ecma335;

namespace BoGLWeb {
    public class BondGraphEmbedder {
        private const double cRep = 10000.0;
        private const double cSpring = 15.0;
        private const double kL = 100.0;
        private const int maxIters = 500;
        private const double epsilon = 10.0;

        private double maxForceChange = double.MaxValue;
        private double iters = 1;
        private bool optimized;
        private readonly BondGraph bondGraph;

        /// <summary>
        /// Take in a bond graph that will be embedded into a canvas
        /// </summary>
        /// <param name="bondGraph">A  ond graph</param>
        public BondGraphEmbedder(BondGraph bondGraph) {
            this.optimized = false;
            this.bondGraph = bondGraph;
        }

        /// <summary>
        /// Creates an embedding of the bond graph
        /// </summary>
        public void embedBondGraph() {
            Dictionary<BondGraph.Element, Vector> forceMap = this.bondGraph.getElements().ToDictionary(element => element.Value, element => new Vector(0, 0));

            //Check if we have reached a solution which is approximately optimal of the maximum iterations
            if (this.iters > maxIters || this.maxForceChange < epsilon) {
                this.optimized = true;
            } 
            
            //Calculate the forces for each element
            foreach (KeyValuePair<string, BondGraph.Element> element in this.bondGraph.getElements()) {
                BondGraph.Element e = element.Value;
                HashSet<BondGraph.Element> adj = new();
                HashSet<BondGraph.Element> notAdj = new();
                
                //Find Edges adjacent to the element
                //TODO Factor this out, it will improve speed
                foreach (BondGraph.Bond bond in this.bondGraph.getBonds()) {
                    if (bond.getSource().Equals(e)) {
                        adj.Add(bond.getSink());
                        if (notAdj.Contains(e)) {
                            notAdj.Remove(e);
                        }
                    } else if (bond.getSink().Equals(e)) {
                        adj.Add(bond.getSource());
                        if (notAdj.Contains(e)) {
                            notAdj.Remove(e);
                        }
                    } else if (!(adj.Contains(bond.getSource()) || adj.Contains(bond.getSink()))) {
                        notAdj.Add(bond.getSource());
                        notAdj.Add(bond.getSink());
                    }
                }

                //Computes the forces for the current element
                List<Vector> springList = adj.Select(adjElement => attractiveForce(e.getX(), e.getY(), adjElement.getX(), adjElement.getY())).ToList();
                List<Vector> repList = notAdj.Select(notAdjEle => repulsiveForce(e.getX(), e.getY(), notAdjEle.getX(), notAdjEle.getY())).ToList();
                
                //Sum the forces
                Vector sumRep = new Vector(0, 0);
                Vector sumSpring = new Vector(0, 0);
                sumRep = repList.Aggregate(sumRep, (current, v) => new Vector(current.getXMag() + v.getXMag(), current.getYMag() + v.getYMag()));
                sumSpring = springList.Aggregate(sumSpring, (current, v) => new Vector(current.getXMag() + v.getXMag(), current.getYMag() + v.getYMag()));

                //Sums the forces on the current element
                forceMap[e] = new Vector(sumRep.getXMag() + sumSpring.getXMag(),
                    sumRep.getYMag() + sumSpring.getYMag());
            }

            //Updates the positions of all elements
            foreach (KeyValuePair<string, BondGraph.Element> entry in this.bondGraph.getElements()) {
                BondGraph.Element n = entry.Value;
                Vector f = forceMap[n];

                n.setPosition(n.getX() + f.getXMag(), n.getY() + f.getYMag());
            }

            //Finds the maximum force
            this.iters++;
            double max = 0;
            foreach (KeyValuePair<BondGraph.Element, Vector> entry in forceMap.Where(entry => Math.Abs(magnitude(entry.Value)) > max)) {
                max = magnitude(entry.Value);
            }

            this.maxForceChange = max;
        }
        
        /// <summary>
        /// Checks if the graph is optimized
        /// </summary>
        /// <returns>True if the graph is optimized, false otherwise</returns>
        public bool isOptimized() {
            return this.optimized;
        }
        
        //Computes the attractive force between two elements by modeling the edge as a spring
        private static Vector attractiveForce(double x1, double y1, double x2, double y2) {
            double dist = distance(x1, y1, x2, y2);
            double scalar = cSpring * Math.Log(dist / kL);
            Vector unitVector = getUnitVector(x1, y1, x2, y2);
            return new Vector(unitVector.getXMag() * scalar, unitVector.getYMag() * scalar);
        }

        //Finds the unit vector between two points
        private static Vector getUnitVector(double x1, double y1, double x2, double y2) {
            double x = x2 - x1;
            double y = y2 - y1;
            double mag = magnitude(new Vector(x, y));
            return new Vector(x / mag, y / mag);
        }

        //Finds the magnitude of a vector
        private static double magnitude(Vector v) {
            double x = v.getXMag();
            double y = v.getYMag();
            return Math.Sqrt(Math.Abs(x * x) + Math.Abs(y * y));
        }

        //Computes the repulsive force between two elements
        private static Vector repulsiveForce(double x1, double y1, double x2, double y2) {
            Vector unitVector = getUnitVector(x2, y2, x1, y1);
            double dist = distance(x2, y2, x1, y1);
            double scalar = cRep / (dist * dist);
            return new Vector(unitVector.getXMag() * scalar, unitVector.getYMag() * scalar);
        }

        //Finds the distance between two points
        private static double distance(double x1, double y1, double x2, double y2) {
            double xDist = Math.Abs(x2 - x1);
            double yDist = Math.Abs(y2 - y1);
            return Math.Sqrt((xDist * xDist) + (yDist * yDist));
        }

        /// <summary>
        /// Gets the bond graph
        /// </summary>
        /// <returns>A bond graph</returns>
        public BondGraph getBondGraph() {
            return this.bondGraph;
        }

        //Private class used to represent a vector
        private class Vector {
            private readonly double xMag, yMag;

            public Vector(double xMag, double yMag) {
                this.xMag = xMag;
                this.yMag = yMag;
            }

            public double getXMag() {
                return this.xMag;
            }

            public double getYMag() {
                return this.yMag;
            }

            public override string ToString() {
                return "xMag: " + this.xMag + " yMag: " + this.yMag;
            }
        }
    }
}