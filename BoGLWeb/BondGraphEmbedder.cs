using System.Reflection.Metadata.Ecma335;

namespace BoGLWeb {
    public class BondGraphEmbedder {
        private const double cRep = 10000.0;
        private const double cSpring = 15.0;
        private const double kL = 100.0;
        private const int maxIters = 1000;
        private const double epsilon = 1.0;

        private double maxForceChange = double.MaxValue;
        private double iters = 1;
        private bool optimized;
        private readonly BondGraph bondGraph;

        public BondGraphEmbedder(BondGraph bondGraph) {
            this.optimized = false;
            this.bondGraph = bondGraph;
        }

        public void embedBondGraph() {
            Dictionary<BondGraph.Element, Vector> forceMap = this.bondGraph.getElements().ToDictionary(element => element.Value, element => new Vector(0, 0));

            if (this.iters > maxIters || this.maxForceChange < epsilon) {
                this.optimized = true;
            } 
            
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

                List<Vector> springList = adj.Select(adjElement => attractiveForce(e.getX(), e.getY(), adjElement.getX(), adjElement.getY())).ToList();
                List<Vector> repList = notAdj.Select(notAdjEle => repulsiveForce(e.getX(), e.getY(), notAdjEle.getX(), notAdjEle.getY())).ToList();

                Vector sumRep = new Vector(0, 0);
                Vector sumSpring = new Vector(0, 0);
                sumRep = repList.Aggregate(sumRep, (current, v) => new Vector(current.getXMag() + v.getXMag(), current.getYMag() + v.getYMag()));
                sumSpring = springList.Aggregate(sumSpring, (current, v) => new Vector(current.getXMag() + v.getXMag(), current.getYMag() + v.getYMag()));

                forceMap[e] = new Vector(sumRep.getXMag() + sumSpring.getXMag(),
                    sumRep.getYMag() + sumSpring.getYMag());
            }

            foreach (KeyValuePair<string, BondGraph.Element> entry in this.bondGraph.getElements()) {
                BondGraph.Element n = entry.Value;
                Vector f = forceMap[n];

                n.setPosition(n.getX() + f.getXMag(), n.getY() + f.getYMag());
            }

            this.iters++;
            double max = 0;
            foreach (KeyValuePair<BondGraph.Element, Vector> entry in forceMap.Where(entry => Math.Abs(magnitude(entry.Value)) > max)) {
                max = magnitude(entry.Value);
            }

            this.maxForceChange = max;
        }

        public bool isOptimized() {
            return this.optimized;
        }
        
        private static Vector attractiveForce(double x1, double y1, double x2, double y2) {
            double dist = distance(x1, y1, x2, y2);
            double scalar = cSpring * Math.Log(dist / kL);
            Vector unitVector = getUnitVector(x1, y1, x2, y2);
            return new Vector(unitVector.getXMag() * scalar, unitVector.getYMag() * scalar);
        }

        private static Vector getUnitVector(double x1, double y1, double x2, double y2) {
            double x = x2 - x1;
            double y = y2 - y1;
            double mag = magnitude(new Vector(x, y));
            return new Vector(x / mag, y / mag);
        }

        private static double magnitude(Vector v) {
            double x = v.getXMag();
            double y = v.getYMag();
            return Math.Sqrt(Math.Abs(x * x) + Math.Abs(y * y));
        }

        private static Vector repulsiveForce(double x1, double y1, double x2, double y2) {
            Vector unitVector = getUnitVector(x2, y2, x1, y1);
            double dist = distance(x2, y2, x1, y1);
            double scalar = cRep / (dist * dist);
            return new Vector(unitVector.getXMag() * scalar, unitVector.getYMag() * scalar);
        }

        private static double distance(double x1, double y1, double x2, double y2) {
            double xDist = Math.Abs(x2 - x1);
            double yDist = Math.Abs(y2 - y1);
            return Math.Sqrt((xDist * xDist) + (yDist * yDist));
        }

        public BondGraph getBondGraph() {
            return this.bondGraph;
        }


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