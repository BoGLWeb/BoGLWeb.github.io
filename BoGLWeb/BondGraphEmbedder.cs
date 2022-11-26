using System.Reflection.Metadata.Ecma335;

namespace BoGLWeb {
    public class BondGraphEmbedder {
        private static readonly double cRep = 10000.0;
        private static readonly double cSpring = 15.0;
        private static readonly double kL = 100.0;
        private static readonly int maxIters = 1000;
        private static readonly double epsilon = 0.1;

        private double maxForceChange = double.MaxValue;
        private double iters = 1;
        private bool optimized;
        private BondGraph bondGraph;

        public BondGraphEmbedder(BondGraph bondGraph) {
            this.optimized = false;
            this.bondGraph = bondGraph;
        }

        public void embedBondGraph() {
            Dictionary<BondGraph.Element, Vector> forceMap = new();

            foreach (KeyValuePair<string, BondGraph.Element> element in bondGraph.getElements()) {
                forceMap.Add(element.Value, new Vector(0, 0));
            }

            if (iters > maxIters || maxForceChange < epsilon) {
                optimized = true;
            }

            //TODO These names are really bad. Need to fix.
            foreach (KeyValuePair<string, BondGraph.Element> element in bondGraph.getElements()) {
                BondGraph.Element e = element.Value;
                List<Vector> repList = new();
                List<Vector> springList = new();
                HashSet<BondGraph.Element> adj = new();
                HashSet<BondGraph.Element> notAdj = new();

                //Find Edges adjacent to the element
                //TODO Factor this out, it will improve speed
                foreach (BondGraph.Bond bond in bondGraph.getBonds()) {
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

                foreach (BondGraph.Element adjElement in adj) {
                    //Compute attractive force
                    springList.Add(attractiveForce(e.getX(), e.getY(), adjElement.getX(), adjElement.getY()));
                }

                foreach (BondGraph.Element notAdjEle in notAdj) {
                    //Compute repulsive force
                    repList.Add(repulsiveForce(e.getX(), e.getY(), notAdjEle.getX(), notAdjEle.getY()));
                }

                Vector sumRep = new Vector(0, 0);
                Vector sumSpring = new Vector(0, 0);
                foreach (Vector v in repList) {
                    sumRep = new Vector(sumRep.getXMag() + v.getXMag(), sumRep.getYMag() + v.getYMag());
                }

                foreach (Vector v in springList) {
                    sumSpring = new Vector(sumSpring.getXMag() + v.getXMag(), sumSpring.getYMag() + v.getYMag());
                }

                forceMap[e] = new Vector(sumRep.getXMag() + sumSpring.getXMag(),
                    sumRep.getYMag() + sumSpring.getYMag());
            }

            foreach (KeyValuePair<string, BondGraph.Element> entry in bondGraph.getElements()) {
                BondGraph.Element n = entry.Value;
                Vector f = forceMap[n];

                n.setPosition(n.getX() + f.getXMag(), n.getY() + f.getYMag());
            }

            iters++;
            double max = 0;
            foreach (KeyValuePair<BondGraph.Element, Vector> entry in forceMap) {
                if (Math.Abs(magnitude(entry.Value)) > max) {
                    max = magnitude(entry.Value);
                }
            }

            maxForceChange = max;
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

        public static Vector repulsiveForce(double x1, double y1, double x2, double y2) {
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


        public class Vector {
            private readonly double xMag, yMag;

            public Vector(double xMag, double yMag) {
                this.xMag = xMag;
                this.yMag = yMag;
            }

            public double getXMag() {
                return xMag;
            }

            public double getYMag() {
                return yMag;
            }

            public override string ToString() {
                return "xMag: " + xMag + " yMag: " + yMag;
            }
        }
    }
}