namespace BoGLWeb {
    public class BondGraphEmbedder {

        private static readonly double cRep = 10000.0;
        private static readonly double cSpring = 15.0;
        private static readonly double kL = 50.0;
        private static readonly int maxIters = 10000;
        private static readonly double epsilon = 0.1;

        public static BondGraph embedBondGraph(BondGraph bondGraph) {
            double maxForceChange = double.MaxValue;
            double iters = 1;
            bool optimized = false;

            while (!optimized) {
                Dictionary<BondGraph.Element, Vector> forceMap = new();

                foreach (KeyValuePair<string, BondGraph.Element> element in bondGraph.getElements()) {
                    forceMap.Add(element.Value, new Vector(0, 0));
                }

                if (iters > maxIters || maxForceChange < epsilon) {
                    optimized = true;
                    return bondGraph;
                }

                //TODO These names are really bad. Need to fix.
                foreach (KeyValuePair<string, BondGraph.Element> element in bondGraph.getElements()) {
                    BondGraph.Element e = element.Value;
                    List<Vector> repList = new();
                    List<Vector> springList = new();
                    HashSet<BondGraph.Element> adj = new();
                    HashSet<BondGraph.Element> notAdj = new();

                    //Find Edges adjacent to the element
                    foreach (BondGraph.Bond bond in bondGraph.getBonds()) {
                        if (bond.getSource().Equals(e)) {
                            adj.Add(bond.getSink());
                        } else if (bond.getSink().Equals(e)) {
                            adj.Add(bond.getSource());
                        } else {
                            notAdj.Add(bond.getSource());
                            notAdj.Add(bond.getSink());
                        }
                    }

                    foreach (BondGraph.Element adjElement in adj) {
                        //Compute attractive force
                        springList.Add(attractiveForce(e.getX(), e.getY(), adjElement.getX(), adjElement.getY()));
                    }

                    foreach (BondGraph.Element adjElement in notAdj) {
                        //Compute attractive force
                        springList.Add(repulsiveForce(e.getX(), e.getY(), adjElement.getX(), adjElement.getY()));
                    }

                    Vector sumRep = new Vector(0, 0);
                    Vector sumSpring = new Vector(0, 0);
                    foreach (Vector v in repList) {
                        sumRep = new Vector(sumRep.getXMag() + v.getXMag(), sumRep.getYMag() + v.getYMag());
                    }

                    foreach (Vector v in springList) {
                        sumSpring = new Vector(sumSpring.getXMag() + v.getXMag(), sumSpring.getYMag() + v.getYMag());
                    }

                    forceMap.Add(e, new Vector(sumRep.getXMag() + sumSpring.getXMag(), sumRep.getXMag() + sumSpring.getXMag()));
                }

                foreach (KeyValuePair<string, BondGraph.Element> entry in bondGraph.getElements()) {
                    BondGraph.Element n = entry.Value;
                    Vector f = forceMap[n];

                    forceMap[n] = f;
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

            //TODO Show that we have not created an optimal layout
            return bondGraph;
        }

        private static Vector attractiveForce(double x1, double y1, double x2, double y2) {
            double dist= distance(x1, y1, x2, y2);
            double scalar = cSpring * Math.Log(dist/ kL);
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
        }
    }
}
